using Godot;
using System;

public class LoginScreen : Control
{   
    CSLogger cslogger;
    Game game;
    LineEdit textField;
    
    public override void _Ready()
    {   
        cslogger = GetNode<CSLogger>("/root/CSLogger");
        game = GetNode<Game>("/root/Game");

        textField = this.GetNode<LineEdit>("VBoxContainer/HBoxContainer/NameLineEdit");
        textField.GrabFocus();
    }

    private void _on_JoinButton_button_up()
    {
        cslogger.Info($"LoginScreen: trying to login as {textField.Text}");
        
        //EmitSignal("SetPlayerName", textField.Text);
        bool success = game.JoinGameAsPlayer(textField.Text);
        if (!success)
        {
            cslogger.Info($"LoginScreen: join failed TODO tell player why");
            // TODO: alert errors or something 
        }
        else 
        {
            // remove myself
            QueueFree();
        }
    }
}

