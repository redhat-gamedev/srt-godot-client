using Godot;
using System;

public partial class GameOverScreen : CanvasLayer
{
  // Declare member variables here. Examples:
  // private int a = 2;
  // private string b = "text";

  Game MyGame;
  CanvasLayer ourGui;
  Serilog.Core.Logger _serilogger;

  // Called when the node enters the scene tree for the first time.
  public override void _Ready()
  {
	MyGame = GetNode<Game>("/root/Game");
	_serilogger = MyGame._serilogger;

	ourGui = MyGame.GetNode<CanvasLayer>("GUI");
  }

  //  // Called every frame. 'delta' is the elapsed time since the previous frame.
  //  public override void _Process(float delta)
  //  {
  //      
  //  }

  public void _on_TryAgainButton_button_up()
  {
	// send the join message
	bool success = MyGame.JoinGameAsPlayer(MyGame.myUuid);
	if (!success)
	{
	  _serilogger.Information($"GameOverScreen.cs: join failed TODO tell player why");
	  // TODO: alert errors or something 
	}
	else
	{
	  // since we successfully joined the game, we can remove this node
	  // which is the game over screen. removing the screen "displays"
	  // the main game window
	  QueueFree();

	  // re-show the GUI
	  MyGame.initializeGameUI();
	}
  }
}
