using Godot;
using System;

public class LoginScreen : Control
{
  Game MyGame;

  Serilog.Core.Logger _serilogger;

  LineEdit textField;

  [Signal] public delegate void retryAuthorization();

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

    MyGame.goToTheGame(textField.Text);
  }

  private void _on_RetryButton_pressed()
  {
    GD.Print("retry auth");
    EmitSignal("retryAuthorization");
  }
}
