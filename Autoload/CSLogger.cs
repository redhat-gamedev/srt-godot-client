// C# singleton script for making the GDScript gdlogger more accessible
using Godot;
using System;

public class CSLogger : Node
{
  Node gdlogger;
  
  public override void _Ready() {
   gdlogger = (Node)GetNode("/root/GDLogger");

  }

  // simple helpers
  public void Verbose(String message)
  {
    gdlogger.Call("verbose", message);
  }
  
  public void Debug(String message)
  {
    gdlogger.Call("debug", message);
  }
  
  public void Info(String message)
  {
    gdlogger.Call("info", message);
  }
  
  public void Warn(String message)
  {
    gdlogger.Call("warn", message);
  }
  public void Error(String message)
  {
    gdlogger.Call("error", message);
  }

}