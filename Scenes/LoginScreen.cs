using Godot;
using System;

public class LoginScreen : Control
{
  private Authorization authorization;
  private LineEdit textField;
  Boolean activateAuthDev = false;
  public Serilog.Core.Logger _serilogger;

  [Signal] public delegate void loginSuccess(string userId);

  public override void _Ready()
  {
    var clientConfig = new ConfigFile();
    Godot.Error err = clientConfig.Load("res://Resources/client.cfg");

    if (err == Godot.Error.Ok)
    {
      // enable/disable authentication in dev mode
      activateAuthDev = (Boolean)clientConfig.GetValue("auth", "activate_auth_dev");

      // skip the authentication flow in Debug mode and if we don't want to test it
      if (OS.IsDebugBuild() && activateAuthDev == false)
      {
        this.GetNode<TextureRect>("NoAuthorizedRect").Visible = false;
        this.GetNode<TextureRect>("AuthLoadingRect").Visible = false;

        textField = this.GetNode<LineEdit>("VBoxContainer/HBoxContainer/NameLineEdit");
        textField.GrabFocus();
      }
      else
      {
        authorization = authorization = new Authorization();
        this.AddChild(authorization);

        // listening for the Auth results: fail or success
        authorization.Connect("playerAuthenticated", this, "_on_is_player_authenticated");
      }

    }
  }

  void _on_is_player_authenticated(bool isAuthenticated)
  {
    // if auth fail show Not authenticated screen with a retry button (_on_RetryButton_pressed)
    if (isAuthenticated == false)
    {
      _serilogger.Debug("LoginScreen.cs: User no authenticated,retry");
      this.GetNode<TextureRect>("AuthLoadingRect").Visible = false;

      return;
    }

    //if the player is authenticated, remove this node and notify the Main Game
    QueueFree();
    EmitSignal("loginSuccess", authorization.getUserId());
  }

  private void _on_RetryButton_pressed()
  {
    _serilogger.Debug("LoginScreen.cs: Retry authentication");
    authorization.authorize();
  }

  private void _on_JoinButton_button_up()
  {
    QueueFree();
    EmitSignal("loginSuccess", textField.Text);
  }
}
