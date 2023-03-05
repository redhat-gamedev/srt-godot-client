using Godot;

public class LoginScreen : Control
{
  Serilog.Core.Logger _serilogger;

  [Signal] public delegate void retryAuthorization();

  private void _on_RetryButton_pressed()
  {
    _serilogger.Information("retry auth");
    EmitSignal("retryAuthorization");
  }
}
