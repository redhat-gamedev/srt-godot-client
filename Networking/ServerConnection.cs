using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using Amqp;
using Amqp.Framing;
using Amqp.Types;
using ProtoBuf;
using redhatgamedev.srt;

// NOTE: This is an autoloaded singleton class
// We use this class to represent a remote connection to the game servers
public class ServerConnection : Node
{
    CSLogger cslogger;

    // AMQ Broker connection details
    String url = "amqp://127.0.0.1:5672";
    bool disableCertValidation = true;
    String commandsQueue = "COMMAND.IN";
    String gameEventsTopic = "GAME.EVENT.OUT";
    ConnectionFactory factory;
    Connection amqpConnection;
    Session amqpSession;
    SenderLink commandsSender;
    ReceiverLink gameEventsReceiver;
    public static readonly string UUID = System.Guid.NewGuid().ToString();
    
    public override void _Ready()
    {
        cslogger = GetNode<CSLogger>("/root/CSLogger");
        
        var clientConfig = new ConfigFile();
        Godot.Error err = clientConfig.Load("res://Resources/client.cfg");
        if (err == Godot.Error.Ok)
        {
            cslogger.Info("Successfully loaded the AMQ config from 'res://Resources/client.cfg'");
            url = (String) clientConfig.GetValue("amqp","server_string", "amqp://127.0.0.1:5672");
            cslogger.Verbose("config file: setting url to " + url);
            disableCertValidation = (bool) clientConfig.GetValue("amqp", "disable_cert_validation", true);
            cslogger.Verbose("config file: setting cert validation to " + disableCertValidation);
        }

        InitializeAMQP();
    }

    public void ConnectToServer()
    {
        cslogger.Debug("connecting to server");
    }

    private void OnConnectSuccess()
    {
        // TODO
        cslogger.Debug("connected to server");
    }
    
    private void OnConnectFailed()
    {
        // TODO
        cslogger.Error("failed to connect server - TBD why");
    }

    public void RemovePlayer(String UUID)
    {
        // TODO do we need this anymore? Delete and remove references to it
    }

    private void GameEventReceived(IReceiverLink receiver, Message message)
    {
        cslogger.Verbose("Game Event received!");
        receiver.Accept(message);
        byte[] binaryBody = (byte[])message.Body;
        MemoryStream st = new MemoryStream(binaryBody, false);
        CommandBuffer commandBuffer;
        commandBuffer = Serializer.Deserialize<CommandBuffer>(st);
        EmitSignal("NewGameEvent", commandBuffer);
    }
    
    public void SendCommand(CommandBuffer CommandBuffer)
    {
        try
        {
            cslogger.Verbose("ServerConnection: Sending command");
            MemoryStream st = new MemoryStream();
            Serializer.Serialize<CommandBuffer>(st, CommandBuffer);
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

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
