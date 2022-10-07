using Godot;
using System;
using System.IO;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using ProtoBuf;
using redhatgamedev.srt;

// We use this class to represent a remote connection to the game servers
public class ServerConnection : Node
{
  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  // AMQ Broker connection details
  private readonly String commandsQueue = "COMMAND.IN";
  private readonly String gameEventsTopic = "GAME.EVENT.OUT";
  String url;
  bool disableCertValidation = true;
  ConnectionFactory factory;
  Connection amqpConnection;
  Session amqpSession;
  SenderLink commandsSender;
  ReceiverLink gameEventsReceiver;
  public static readonly string UUID = System.Guid.NewGuid().ToString();

  // Ugh - seems like we can't seralize protbuf classes via signals so this might not work
  //[Signal]
  //public delegate void CreatePlayerGameEvent();
  //[Signal]
  //public delegate void CreateMissleGameEvent();
  //[Signal]
  //public delegate void UpdatePlayerGameEvent();
  //[Signal]
  //public delegate void UpdateMissleGameEvent();

  public override void _Ready()
  {
    MyGame = GetNode<Game>("/root/Game");
    _serilogger = MyGame._serilogger;

    var clientConfig = new ConfigFile();
    Godot.Error err; 

    _serilogger.Debug("ServerConnection.cs: Attempting to load embedded client config");
    err = clientConfig.Load("res://Resources/client.cfg");
    if (err == Godot.Error.Ok)
    {
      _serilogger.Information("ServerConnection.cs: Successfully loaded the config from 'res://Resources/client.cfg'");
      url = (String)clientConfig.GetValue("amqp", "server_string", "amqp://127.0.0.1:5672");
      _serilogger.Debug("ServerConnection.cs: config file: setting url to " + url);
      disableCertValidation = (bool)clientConfig.GetValue("amqp", "disable_cert_validation", true);
      _serilogger.Debug("ServerConnection.cs: config file: setting cert validation to " + disableCertValidation);
    }

    _serilogger.Debug("ServerConnection.cs: Overriding with client local/user config");
    err = clientConfig.Load("user://client.cfg");
    if (err == Godot.Error.Ok)
    {
      _serilogger.Information("ServerConnection.cs: Successfully loaded the config from 'user://client.cfg'");
      url = (String)clientConfig.GetValue("amqp", "server_string", "amqp://127.0.0.1:5672");
      _serilogger.Debug("ServerConnection.cs: config file: setting url to " + url);
      disableCertValidation = (bool)clientConfig.GetValue("amqp", "disable_cert_validation", true);
      _serilogger.Debug("ServerConnection.cs: config file: setting cert validation to " + disableCertValidation);
    }

    InitializeAMQP();
  }

  public void RemovePlayerSelf()
  {
    // TODO do we need this anymore? Delete and remove references to it
  }

  private void GameEventReceived(IReceiverLink receiver, Message message)
  {
    _serilogger.Verbose("Game Event received!");
    try
    {
      receiver.Accept(message);
      byte[] binaryBody = (byte[])message.Body;
      MemoryStream st = new MemoryStream(binaryBody, false);
      EntityGameEventBuffer egeb;
      egeb = Serializer.Deserialize<EntityGameEventBuffer>(st);

      this.ProcessGameEvent(egeb); // TODO: move this into it's own class to declutter the networking code
    }
    catch (Exception ex)
    {
      _serilogger.Warning("ServerConnection.cs: Issue deserializing game event.");
      _serilogger.Error($"ServerConnection.cs: {ex.Message}");
      // TODO: if this continues - warn player of connection issues
      return;
    }
  }

  public void SendCommand(CommandBuffer commandBuffer)
  {
    try
    {
      _serilogger.Verbose("ServerConnection.cs: Sending command");
      MemoryStream st = new MemoryStream();
      Serializer.Serialize<CommandBuffer>(st, commandBuffer);
      //Serializer.SerializeWithLengthPrefix<CommandBuffer>(st, commandBuffer, PrefixStyle.Base128, 123);
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

  async void InitializeAMQP()
  {
    _serilogger.Debug("ServerConnection.cs: Initializing AMQP connection");
    Connection.DisableServerCertValidation = disableCertValidation;
    try
    {
      //Trace.TraceLevel = TraceLevel.Frame;
      //Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
      factory = new ConnectionFactory();
      _serilogger.Debug("ServerConnection.cs: connecting to " + url);
      Address address = new Address(url);
      amqpConnection = await factory.CreateAsync(address);
      amqpSession = new Session(amqpConnection);
    }
    catch (Exception ex)
    {
      _serilogger.Error("ServerConnection.cs: AMQP connection/session failed for " + url);
      _serilogger.Error($"ServerConnection.cs: {ex.Message}");
      // TODO: let player know
      return;
    }

    var linkid = "srt-game-client-receiver-" + UUID;
    _serilogger.Debug("ServerConnection.cs: Creating AMQ receiver for game events: " + linkid);
    Source eventInSource = new Source
    {
      Address = gameEventsTopic,
      Capabilities = new Symbol[] { new Symbol("topic") }
    };
    gameEventsReceiver = new ReceiverLink(amqpSession, linkid, eventInSource, null);
    gameEventsReceiver.Start(10, GameEventReceived);

    linkid = "srt-game-client-command-sender-" + UUID;
    _serilogger.Debug("ServerConnection.cs: Creating AMQ sender for player commands: " + linkid);
    Target commandOutTarget = new Target
    {
      Address = commandsQueue,
      Capabilities = new Symbol[] { new Symbol("queue") }
    };
    commandsSender = new SenderLink(amqpSession, linkid, commandOutTarget, null);

    _serilogger.Debug("ServerConnection.cs: Finished initializing AMQP connection");
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
  private void ProcessGameEvent(EntityGameEventBuffer egeb)
  {
    try
    {
      switch (egeb.Type)
      {
        case EntityGameEventBuffer.EntityGameEventBufferType.Create:
          _serilogger.Information("ServerConnection.cs: EntityGameEventBuffer [create]");
          switch (egeb.objectType)
          {
            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Player:
              _serilogger.Information("ServerConnection.cs: Got create for a ship");
              PlayerShip newShip = MyGame.CreateShipForUUID(egeb.Uuid);
              newShip.UpdateFromGameEventBuffer(egeb);
              break;

            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Missile:
              _serilogger.Information("ServerConnection.cs: Got create for a missile");
              SpaceMissile newMissile = MyGame.CreateMissileForUUID(egeb);
              newMissile.UpdateFromGameEventBuffer(egeb);
              break;
          }
          break;

        case EntityGameEventBuffer.EntityGameEventBufferType.Destroy:
          _serilogger.Information("ServerConnection.cs: EntityGameEventBuffer [destroy]");

          switch (egeb.objectType)
          {
            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Player:
              _serilogger.Information($"ServerConnection.cs: Got destroy for player {egeb.Uuid}");
              MyGame.DestroyShipWithUUID(egeb.Uuid);
              break;

            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Missile:
              _serilogger.Information($"ServerConnection.cs: Got destroy for missile {egeb.Uuid}");
              MyGame.DestroyMissileWithUUID(egeb.Uuid);
              break;

          }
          break;

        case EntityGameEventBuffer.EntityGameEventBufferType.Retrieve:
          _serilogger.Information("ServerConnection.cs: EntityGameEventBuffer [retrieve]");
          break;

        case EntityGameEventBuffer.EntityGameEventBufferType.Update:
          // find/update the Node2D
          if (egeb.Uuid == null || egeb.Uuid.Length < 1) // TODO: any additional validation goes here
          {
            _serilogger.Warning("ServerConnection.cs: got update event with invalid UUID, IGNORING...");
            return;
          }

          switch (egeb.objectType)
          {
            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Player:
              PlayerShip ship = MyGame.UpdateShipWithUUID(egeb.Uuid);
              ship.UpdateFromGameEventBuffer(egeb);
              break;

            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Missile:
              SpaceMissile missile = MyGame.UpdateMissileWithUUID(egeb);
              missile.UpdateFromGameEventBuffer(egeb);
              break;
          }
          break;

        default:
          _serilogger.Information("ServerConnection.cs: EntityGameEventBuffer type:[?????], IGNORING...");
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
