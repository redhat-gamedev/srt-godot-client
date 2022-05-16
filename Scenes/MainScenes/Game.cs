using Godot;
using System;
using System.Collections.Generic;
using redhatgamedev.srt;

// This class is autoloaded
public class Game : Node
{
    private ServerConnection serverConnection;
    private LoginScreen loginScreen;
    CSLogger cslogger;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        cslogger = GetNode<CSLogger>("/root/CSLogger");
        cslogger.Info("Space Ring Things (SRT) Game Client v???");

        serverConnection = new ServerConnection();
        this.AddChild(serverConnection);
        serverConnection.Connect("NewGameEvent", this, nameof(_onNewGameEvent));
        
        this.GetTree().ChangeScene("res://Scenes/LoginScreen.tscn");

        // TODO: check for server connection and do some retries if something is wrong
        // if lots of fails, pop up an error screen (and let player do server config?)
    }

    public override void _Process(float delta)
    {
        var velocity = Vector2.Zero; // The player's movement direction.
        var shoot = Vector2.Zero; // the player's shoot status
        if (Input.IsActionPressed("rotate_right")) velocity.x += 1;
        if (Input.IsActionPressed("rotate_left")) velocity.x -= 1;
        if (Input.IsActionPressed("thrust_forward")) velocity.y += 1;
        if (Input.IsActionPressed("thrust_reverse")) velocity.y -= 1;
        if (Input.IsActionPressed("fire")) shoot.y = 1;        
        if ((velocity.Length() > 0) || (shoot.Length() > 0)) ProcessInputEvent(velocity, shoot);

        // TODO ? maybe handle server telling us of new players
    }

    public bool JoinGameAsPlayer(string playerName)
    {
        // TODO: if not connected, try again to connect to server
        cslogger.Debug($"Game.cs: Sending join with UUID: {ServerConnection.UUID}, named: {playerName}");

        // construct a join message
        SecurityCommandBuffer scb = new SecurityCommandBuffer();
        scb.Uuid = playerName; //ServerConnection.UUID;
        scb.Type = SecurityCommandBuffer.SecurityCommandBufferType.Join;
        CommandBuffer cb = new CommandBuffer();
        cb.Type = CommandBuffer.CommandBufferType.Security;
        cb.securityCommandBuffer = scb;
        serverConnection.SendCommand(cb);
        
        return true; // TODO: this can't always be true
    }

    void ProcessInputEvent(Vector2 velocity, Vector2 shoot)
    {
        // there was some kind of input, so construct a message to send to the server
        CommandBuffer cb = new CommandBuffer();
        cb.Type = CommandBuffer.CommandBufferType.Rawinput;

        RawInputCommandBuffer ricb = new RawInputCommandBuffer();
        ricb.Type = RawInputCommandBuffer.RawInputCommandBufferType.Dualstick;
        ricb.Uuid = ServerConnection.UUID;

        DualStickRawInputCommandBuffer dsricb = new DualStickRawInputCommandBuffer();
        if ((velocity.Length() > 0) || (shoot.Length() > 0))  // what's up with this code? no brackets.

            if (velocity.Length() > 0)
            {
                Box2d.PbVec2 b2dMove = new Box2d.PbVec2();
                b2dMove.X = velocity.x;
                b2dMove.Y = velocity.y;
                dsricb.pbv2Move = b2dMove;
            }

        if (shoot.Length() > 0)
        {
            // TODO: make this actually depend on ship direction
            Box2d.PbVec2 b2dShoot = new Box2d.PbVec2();
            b2dShoot.Y = 1;
            dsricb.pbv2Shoot = b2dShoot;
        }

        ricb.dualStickRawInputCommandBuffer = dsricb;
        cb.rawInputCommandBuffer = ricb;
        serverConnection.SendCommand(cb);
    }
    
    void _onNewGameEvent(CommandBuffer buffer)
    {
        cslogger.Debug($"Game.cs: Got a game event"); // TODO: remove this it'll likely overload the logger
    }

    //  // Called every frame. 'delta' is the elapsed time since the previous frame.
    //  public override void _Process(float delta)
    //  {
    //      
    //  }
}
