using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using ProtoBuf;
using redhatgamedev.srt;

// We use this class to represent a remote connection to the game servers
public class ServerConnection : Node
{
  CSLogger cslogger;
  Game game;

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
    cslogger = GetNode<CSLogger>("/root/CSLogger");
    game = GetNode<Game>("/root/Game");

    var clientConfig = new ConfigFile();
    Godot.Error err = clientConfig.Load("res://Resources/client.cfg");
    if (err == Godot.Error.Ok)
    {
      cslogger.Info("Successfully loaded the AMQ config from 'res://Resources/client.cfg'");
      url = (String)clientConfig.GetValue("amqp", "server_string", "amqp://127.0.0.1:5672");
      cslogger.Verbose("config file: setting url to " + url);
      disableCertValidation = (bool)clientConfig.GetValue("amqp", "disable_cert_validation", true);
      cslogger.Verbose("config file: setting cert validation to " + disableCertValidation);
    }

    InitializeAMQP();
  }

  public void RemovePlayerSelf()
  {
    // TODO do we need this anymore? Delete and remove references to it
  }

  private void GameEventReceived(IReceiverLink receiver, Message message)
  {
    //cslogger.Info("Game Event received!");
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
      cslogger.Warn("ServerConnection: Issue deserializing game event.");
      cslogger.Error(ex.Message);
      // TODO: if this continues - warn player of connection issues
      return;
    }
  }

  public void SendCommand(CommandBuffer commandBuffer)
  {
    try
    {
      cslogger.Verbose("ServerConnection: Sending command");
      MemoryStream st = new MemoryStream();
      Serializer.Serialize<CommandBuffer>(st, commandBuffer);
      //Serializer.SerializeWithLengthPrefix<CommandBuffer>(st, commandBuffer, PrefixStyle.Base128, 123);
      byte[] msgBytes = st.ToArray();
      Message msg = new Message(msgBytes);
      commandsSender.Send(msg, null, null); // don't care about the ack on our message being received
    }
    catch (Exception ex)
    {
      cslogger.Error("ServerConnection: Send Command failed.");
      cslogger.Error(ex.Message);

      // TODO: let player know / return to login screen

      return;
    }
  }

  async void InitializeAMQP()
  {
    cslogger.Debug("Initializing AMQP connection");
    Connection.DisableServerCertValidation = disableCertValidation;
    try
    {
      //Trace.TraceLevel = TraceLevel.Frame;
      //Trace.TraceListener = (l, f, a) => Console.WriteLine(DateTime.Now.ToString("[hh:mm:ss.fff]") + " " + string.Format(f, a));
      factory = new ConnectionFactory();
      cslogger.Debug("connecting to " + url);
      Address address = new Address(url);
      amqpConnection = await factory.CreateAsync(address);
      amqpSession = new Session(amqpConnection);
    }
    catch (Exception ex)
    {
      cslogger.Error("AMQP connection/session failed for " + url);
      cslogger.Error(ex.Message);
      // TODO: let player know
      return;
    }

    var linkid = "srt-game-client-receiver-" + UUID;
    cslogger.Debug("Creating AMQ receiver for game events: " + linkid);
    Source eventInSource = new Source
    {
      Address = gameEventsTopic,
      Capabilities = new Symbol[] { new Symbol("topic") }
    };
    gameEventsReceiver = new ReceiverLink(amqpSession, linkid, eventInSource, null);
    gameEventsReceiver.Start(10, GameEventReceived);

    linkid = "srt-game-client-command-sender-" + UUID;
    cslogger.Debug("Creating AMQ sender for player commands: " + linkid);
    Target commandOutTarget = new Target
    {
      Address = commandsQueue,
      Capabilities = new Symbol[] { new Symbol("queue") }
    };
    commandsSender = new SenderLink(amqpSession, linkid, commandOutTarget, null);

    cslogger.Debug("Finished initializing AMQP connection");
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
      // cslogger will be a ton of log output - only uncomment to debug
      switch (egeb.Type)
      {
        case EntityGameEventBuffer.EntityGameEventBufferType.Create:
          cslogger.Info("EntityGameEventBuffer [create]");
          switch (egeb.objectType)
          {
            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Player:
              cslogger.Info("Got create for a ship");
              PlayerShip newShip = game.CreateShipForUUID(egeb.Uuid);
              newShip.UpdateFromGameEventBuffer(egeb);
              break;

            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Missile:
              cslogger.Info("Got create for a missile");
              SpaceMissile newMissile = game.CreateMissileForUUID(egeb);
              newMissile.UpdateFromGameEventBuffer(egeb);
              break;
          }
          break;

        case EntityGameEventBuffer.EntityGameEventBufferType.Destroy:
          cslogger.Info("EntityGameEventBuffer [destroy]");

          switch (egeb.objectType)
          {
            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Player:
              cslogger.Info("Should be destroying a ship now");
              game.DestroyShipWithUUID(egeb.Uuid);
              break;

            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Missile:
              cslogger.Info($"Destroying SpaceMissile {egeb.Uuid}");
              game.DestroyMissileWithUUID(egeb.Uuid);
              break;

          }
          break;

        case EntityGameEventBuffer.EntityGameEventBufferType.Retrieve:
          cslogger.Info("EntityGameEventBuffer [retrieve]");
          break;

        case EntityGameEventBuffer.EntityGameEventBufferType.Update:
          // find/update the Node2D
          if (egeb.Uuid == null || egeb.Uuid.Length < 1) // TODO: any additional validation goes here
          {
            cslogger.Warn("got update event with invalid UUID, IGNORING...");
            return;
          }

          switch (egeb.objectType)
          {
            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Player:
              PlayerShip ship = game.UpdateShipWithUUID(egeb.Uuid);
              ship.UpdateFromGameEventBuffer(egeb);
              break;

            case EntityGameEventBuffer.EntityGameEventBufferObjectType.Missile:
              SpaceMissile missile = game.UpdateMissileWithUUID(egeb);
              missile.UpdateFromGameEventBuffer(egeb);
              break;
          }
          break;

        default:
          cslogger.Info("EntityGameEventBuffer type:[?????], IGNORING...");
          break;
      }
    }
    catch (Exception ex)
    {
      cslogger.Error("Issue processing game event:");
      cslogger.Error(ex.Message);
      return;
    }
  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //  public override void _Process(float delta)
  //  {
  //      
  //  }
}
