// C# singleton script for making the GDScript gdlogger more accessible
using Godot;
using System;

public class CSLogger : Node
{
  Node gdlogger;

  public override void _Ready()
  {
    // initialize the logging configuration
    Node gdlogger = GetNode<Node>("/root/GDLogger");
    gdlogger.Call("load_config", "res://Resources/logger.cfg");
  }

  // simple helpers
  public void Verbose(String message)
  {
    gdlogger = GetNode<Node>("/root/GDLogger");
    gdlogger.Call("verbose", message);
  }
  
  public void Debug(String message)
  {
    gdlogger = GetNode<Node>("/root/GDLogger");
    gdlogger.Call("debug", message);
  }
  
  public void Info(String message)
  {
    gdlogger = GetNode<Node>("/root/GDLogger");
    gdlogger.Call("info", message);
  }
  
  public void Warn(String message)
  {
    gdlogger = GetNode<Node>("/root/GDLogger");
    gdlogger.Call("warn", message);
  }
  public void Error(String message)
  {
    gdlogger = GetNode<Node>("/root/GDLogger");
    gdlogger.Call("error", message);
  }


}
