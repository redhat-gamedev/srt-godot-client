using Godot;
using System;

public class LoginScreen : Control
{
  Game MyGame;

  public Serilog.Core.Logger _serilogger;

  LineEdit textField;

  public override void _Ready()
  {
    MyGame = GetNode<Game>("/root/Game");
    _serilogger = MyGame._serilogger;


    textField = this.GetNode<LineEdit>("VBoxContainer/HBoxContainer/NameLineEdit");
    textField.GrabFocus();

    // TODO: need to interrogate server for the initial defaults for things like
    // missile speed
  }

  private void _on_JoinButton_button_up()
  {
    _serilogger.Information($"LoginScreen: trying to login as {textField.Text}");
    MyGame.myUuid = textField.Text;

    //EmitSignal("SetPlayerName", textField.Text);
    bool success = MyGame.JoinGameAsPlayer(textField.Text);
    if (!success)
    {
      _serilogger.Information($"LoginScreen: join failed TODO tell player why");
      // TODO: alert errors or something 
    }
    else
    {
      // since we successfully joined the game, we can remove this node
      // which is the login screen. removing the login screen "displays"
      // the main game window
      QueueFree();
      MyGame.initializeGameUI();
    }
  }
}

