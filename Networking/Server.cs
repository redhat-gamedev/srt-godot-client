using Godot;
using System;
using redhatgamedev.srt;

// NOTE: This is an autoloaded singleton class
public class Server : Node
{
    CSLogger cslogger;

    // Declare member variables here. Examples:
    // private int a = 2;
    // private string b = "text";

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        cslogger = GetNode<CSLogger>("/root/CSLogger");
        cslogger.Info("Space Ring Things (SRT) Game Client v???");
        ConnectToServer();
    }

    public void ConnectToServer()
    {
        // TODO
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
        // TODO
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }
}
