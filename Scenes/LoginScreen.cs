using Godot;

public class LoginScreen : Control
{
  Game MyGame;

  private Authorization authorization;

  Serilog.Core.Logger _serilogger;

  [Signal] public delegate void loginSuccess(string userId);

  public override void _Ready()
  {
    authorization = authorization = new Authorization();
    this.AddChild(authorization);

    // listening for the Auth results: fail or success
    authorization.Connect("playerAuthenticated", this, "_on_is_player_authenticated");
  }

  void _on_is_player_authenticated(bool isAuthenticated)
  {
    // if auth fail show Not authenticated screen with a retry button (_on_RetryButton_pressed)
    if (isAuthenticated == false)
    {
      _serilogger.Information("User no authenticated,retry");
      this.GetNode<TextureRect>("AuthLoadingRect").Visible = false;

      return;
    }

    //if the player is authenticated, remove this node and notify the Main Game
    QueueFree();
    EmitSignal("loginSuccess", authorization.getUserId());
  }

  private void _on_RetryButton_pressed()
  {
    _serilogger.Information("Retry authentication");
    authorization.authorize();
  }
}
