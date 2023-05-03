using Godot;
using System;
using System.IO;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using ProtoBuf;
using redhatgamedev.srt.v1;

// We use this class to represent a remote connection to the game servers
public class ServerConnection : Node
{
  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  // AMQ Broker connection details
  private readonly String commandsQueue = "COMMAND.IN";
  private readonly String securityQueue = "SECURITY.IN";
  private readonly String gameEventsTopic = "GAME.EVENT.OUT";

  private readonly String securityOutTopic = "SECURITY.OUT";

  String url;
  bool disableCertValidation = true;
  ConnectionFactory factory;
  Connection amqpConnection;
  Session amqpSession;
  SenderLink commandsSender;
  SenderLink securitySender;
  ReceiverLink gameEventsReceiver;
  ReceiverLink securityOutReceiver;
  public static readonly string UUID = System.Guid.NewGuid().ToString();

  [Signal] public delegate void isServerConnected(bool connected);

  public override void _Ready()
  {
    MyGame = GetNode<Game>("/root/Game");
    _serilogger = MyGame._serilogger;

    // TODO: move config to its own method
    var clientConfig = new ConfigFile();
    Godot.Error err;

    _serilogger.Debug("ServerConnection.cs: Attempting to load embedded client config");
    err = clientConfig.Load("res://Resources/client.cfg");
    if (err != Godot.Error.Ok)
    {
      err = clientConfig.Load("user://client.cfg");
      _serilogger.Information("ServerConnection.cs: Successfully loaded the config from 'user://client.cfg'");

      // If config file failed to load and the game is in debug mode throw an error
      if (err != Godot.Error.Ok && OS.IsDebugBuild())
      {
        throw new Exception("ServerConnection.cs: Failed to load the client configuration file");
      }
    }

    url = System.Environment.GetEnvironmentVariable("SERVER_STRING") ?? (String)clientConfig.GetValue("amqp", "server_string", "amqp://127.0.0.1:5672");
    _serilogger.Debug("ServerConnection.cs: config file: setting url to " + url);

    var disableCertValidationValue = System.Environment.GetEnvironmentVariable("DISABLE_CERT_VALIDAION") ?? clientConfig.GetValue("amqp", "disable_cert_validation", true);
    disableCertValidationValue = (bool)disableCertValidationValue;
    _serilogger.Debug("ServerConnection.cs: config file: setting cert validation to " + disableCertValidation);

    InitializeAMQP();

  }

  public void RemovePlayerSelf()
  {
    // TODO do we need this anymore? Delete and remove references to it
  }

  private void GameEventReceived(IReceiverLink receiver, Message message)
  {
    _serilogger.Verbose("ServerConnection.cs: Game Event received!");
    try
    {
      receiver.Accept(message);
      byte[] binaryBody = (byte[])message.Body;
      MemoryStream st = new MemoryStream(binaryBody, false);
      GameEvent egeb = Serializer.Deserialize<GameEvent>(st);

      long messageDT;
      if (message.Properties != null)
      {
        // CreationTime is a DateTime, so cast to an offset so that we can use the
        // Unix time easily - there may be a better way to do this
        messageDT = ((DateTimeOffset)message.Properties.CreationTime).ToUnixTimeMilliseconds();
      }
      else
      {
        messageDT = 0;
      }

      long localDT = DateTimeOffset.Now.ToUnixTimeMilliseconds();
      long DTDiff = localDT - messageDT;

      _serilogger.Verbose($"ServerConnection.cs: Msg DT: {messageDT} / Current DT: {localDT} / Diff: {DTDiff}");

      var eventTuple = (egeb, messageDT);

      this.ProcessGameEvent(eventTuple); // TODO: move this into it's own class to declutter the networking code
    }
    catch (Exception ex)
    {
      _serilogger.Warning("ServerConnection.cs: Issue deserializing game event.");
      _serilogger.Error($"ServerConnection.cs: {ex.Message}");
      // TODO: if this continues - warn player of connection issues
      return;
    }
  }

  private void SecurityReceived(IReceiverLink receiver, Message message)
  {
    _serilogger.Debug("ServerConnection.cs: Security Event received!");
    try
    {
      receiver.Accept(message);
      byte[] binaryBody = (byte[])message.Body;
      MemoryStream st = new MemoryStream(binaryBody, false);
      Security security = Serializer.Deserialize<Security>(st);

      this.ProcessSecurity(security); // TODO: move this into it's own class to declutter the networking code
    }
    catch (System.Exception)
    {

      throw;
    }
  }

  public void SendCommand(Command commandBuffer)
  {
    try
    {
      _serilogger.Verbose("ServerConnection.cs: Sending command");
      MemoryStream st = new MemoryStream();
      Serializer.Serialize<Command>(st, commandBuffer);
      byte[] msgBytes = st.ToArray();
      Message msg = new Message(msgBytes);
      commandsSender.Send(msg, null, null); // don't care about the ack on our message being received
    }
    catch (Exception ex)
    {
      _serilogger.Error("ServerConnection.cs: Send Command failed.");
      _serilogger.Error(ex.Message);

      // TODO: let player know / return to login screen

      return;
    }
  }

  public void SendSecurity(Security security)
  {
    try
    {
      _serilogger.Debug("ServerConnection.cs: Sending security");
      MemoryStream st = new MemoryStream();
      Serializer.Serialize<Security>(st, security);
      byte[] msgBytes = st.ToArray();
      Message msg = new Message(msgBytes);
      securitySender.Send(msg, null, null); // don't care about the ack on our message being received


    }
    catch (Exception ex)
    {
      _serilogger.Error("ServerConnection.cs: Send Security failed.");
      _serilogger.Error(ex.Message);

      // TODO: let player know / return to login screen

      return;
    }
  }

  async void InitializeAMQP()
  {
    String linkid;

    _serilogger.Information("ServerConnection.cs: Initializing AMQP connection");
    Connection.DisableServerCertValidation = disableCertValidation;
    try
    {
      //Trace.TraceLevel = TraceLevel.Frame;
      //Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
      factory = new ConnectionFactory();
      _serilogger.Information("ServerConnection.cs: connecting to " + url);
      Address address = new Address(url);

      // TODO: does this need to be async? it causes some issues
      amqpConnection = await factory.CreateAsync(address);
      amqpSession = new Session(amqpConnection);
    }
    catch (Exception ex)
    {
      _serilogger.Error("ServerConnection.cs: AMQP connection/session failed for " + url);
      _serilogger.Error($"ServerConnection.cs: {ex.Message}");

      EmitSignal("isServerConnected", false);
      return;
    }

    // set up senders and receivers ////////////////////////////////////////////
    linkid = "srt-game-client-command-receiver-" + UUID;
    _serilogger.Debug("ServerConnection.cs: Creating AMQ receiver for game events: " + linkid);
    Source eventOutSource = new Source
    {
      Address = gameEventsTopic,
      Capabilities = new Symbol[] { new Symbol("topic") }
    };
    gameEventsReceiver = new ReceiverLink(amqpSession, linkid, eventOutSource, null);
    gameEventsReceiver.Start(10, GameEventReceived);

    linkid = "srt-game-client-security-receiver-" + UUID;
    _serilogger.Debug("ServerConnection.cs: Creating AMQ receiver for security events: " + linkid);
    Source securityOutSource = new Source
    {
      Address = securityOutTopic,
      Capabilities = new Symbol[] { new Symbol("topic") }
    };
    securityOutReceiver = new ReceiverLink(amqpSession, linkid, securityOutSource, null);
    securityOutReceiver.Start(10, SecurityReceived);

    linkid = "srt-game-client-command-sender-" + UUID;
    _serilogger.Debug("ServerConnection.cs: Creating AMQ sender for player commands: " + linkid);
    Target commandInTarget = new Target
    {
      Address = commandsQueue,
      Capabilities = new Symbol[] { new Symbol("queue") }
    };
    commandsSender = new SenderLink(amqpSession, linkid, commandInTarget, null);

    linkid = "srt-game-security-sender-" + UUID;
    _serilogger.Debug("ServerConnection.cs: Creating AMQ sender for security commands: " + linkid);
    Target securityInTarget = new Target
    {
      Address = securityQueue,
      Capabilities = new Symbol[] { new Symbol("queue") }
    };
    securitySender = new SenderLink(amqpSession, linkid, securityInTarget, null);

    _serilogger.Debug("ServerConnection.cs: Finished initializing AMQP connection");

    // send announce message
    _serilogger.Debug($"ServerConnection.cs: Sending announce message for our client: {UUID}");
    Security announceMessage = new Security();
    announceMessage.Uuid = UUID;
    announceMessage.security_type = Security.SecurityType.SecurityTypeAnnounce;
    SendSecurity(announceMessage);

    EmitSignal("isServerConnected", true);
  }

  private void ProcessSecurity(Security security)
  {
    try
    {
      switch (security.security_type)
      {
        case Security.SecurityType.SecurityTypeAnnounce:
          _serilogger.Debug($"ServerConnection.cs: Received announce for {security.Uuid}");

          // check if the received announce matches our ServerConnection UUID
          if (security.Uuid == UUID)
          {
            _serilogger.Debug("ServerConnection.cs: Received announce message matches our UUID, processing");
            MyGame.ProcessAnnounce(security);
          }
          break;
        case Security.SecurityType.SecurityTypeJoin:
          // TODO: do something fancy because a player joined
          break;
        case Security.SecurityType.SecurityTypeLeave:
          // TODO: do something fancy because a player left
          break;
        case Security.SecurityType.SecurityTypeUnspecified:
          _serilogger.Verbose("ServerConnection.cs: Unspecified security message received");
          break;
      }
    }
    catch (Exception ex)
    {
      _serilogger.Error("ServerConnection.cs: Issue processing security event:");
      _serilogger.Error(ex.Message);
      return;
    }
  }

  /// <summary>
  /// Translate Network Protocol Buffer Data into Godot game data.
  /// Create events should add to scene
  /// Update events should find/update
  /// Delete event should remove nodes
  ///
  /// TODO: move this into it's own class to declutter the ServerCode
  ///
  /// </summary>
  /// <param name="egeb"></param>
  private void ProcessGameEvent((GameEvent egeb, long DTDiff) tuple)
  {
    // extract the event from the tuple
    GameEvent egeb = tuple.egeb;
    try
    {
      switch (egeb.game_event_type)
      {
        case GameEvent.GameEventType.GameEventTypeCreate:
          _serilogger.Debug("ServerConnection.cs: EntityGameEventBuffer [create]");
          switch (egeb.game_object_type)
          {
            case GameEvent.GameObjectType.GameObjectTypePlayer:
              _serilogger.Debug($"ServerConnection.cs: Got create for a ship {egeb.Uuid}");
              MyGame.PlayerCreateQueue.Enqueue(egeb);
              break;

            case GameEvent.GameObjectType.GameObjectTypeMissile:
              _serilogger.Debug($"ServerConnection.cs: Got create for a missile {egeb.Uuid} owner {egeb.OwnerUuid}");
              MyGame.MissileCreateQueue.Enqueue(egeb);
              break;
          }
          break;

        case GameEvent.GameEventType.GameEventTypeDestroy:
          _serilogger.Debug("ServerConnection.cs: EntityGameEventBuffer [destroy]");

          switch (egeb.game_object_type)
          {
            case GameEvent.GameObjectType.GameObjectTypePlayer:
              _serilogger.Debug($"ServerConnection.cs: Got destroy for player {egeb.Uuid}");
              MyGame.PlayerDestroyQueue.Enqueue(egeb);
              break;

            case GameEvent.GameObjectType.GameObjectTypeMissile:
              _serilogger.Debug($"ServerConnection.cs: Got destroy for missile {egeb.Uuid} owner {egeb.OwnerUuid}");
              MyGame.MissileDestroyQueue.Enqueue(egeb);
              break;

          }
          break;

        case GameEvent.GameEventType.GameEventTypeRetrieve:
          _serilogger.Debug("ServerConnection.cs: EntityGameEventBuffer [retrieve]");
          break;

        case GameEvent.GameEventType.GameEventTypeUpdate:
          _serilogger.Verbose("ServerConnection.cs: EntityGameEventBuffer [update]");

          // find/update the Node2D
          if (egeb.Uuid == null || egeb.Uuid.Length < 1) // TODO: any additional validation goes here
          {
            _serilogger.Warning("ServerConnection.cs: got update event with invalid UUID, IGNORING...");
            return;
          }

          switch (egeb.game_object_type)
          {
            case GameEvent.GameObjectType.GameObjectTypePlayer:
              _serilogger.Verbose($"ServerConnection.cs: Got update for player {egeb.Uuid}");
              // send the whole tuple instead of the egeb part so that we can calculate the true
              // time later
              MyGame.PlayerUpdateQueue.Enqueue(tuple);
              break;

            case GameEvent.GameObjectType.GameObjectTypeMissile:
              _serilogger.Verbose($"ServerConnection.cs: Got update for missile {egeb.Uuid} owner {egeb.OwnerUuid}");
              MyGame.MissileUpdateQueue.Enqueue(egeb);
              break;
          }
          break;

        default:
          _serilogger.Debug("ServerConnection.cs: EntityGameEventBuffer type:[?????], IGNORING...");
          break;
      }
    }
    catch (Exception ex)
    {
      _serilogger.Error("ServerConnection.cs: Issue processing game event:");
      _serilogger.Error(ex.Message);
      return;
    }
  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //  public override void _Process(float delta)
  //  {
  //
  //  }
}
