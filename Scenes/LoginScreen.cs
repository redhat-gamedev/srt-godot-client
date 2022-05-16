using Godot;
using System;

public class LoginScreen : Control
{   
    CSLogger cslogger;
    Game game;
    
    public override void _Ready()
    {   
        cslogger = GetNode<CSLogger>("/root/CSLogger");
        game = GetNode<Game>("/root/Game");
        // TODO: grab focus from keyboard for menu control ?
    }

    private void _on_JoinButton_button_up()
    {
        LineEdit textField = this.GetNode<LineEdit>("VBoxContainer/HBoxContainer/NameLineEdit");
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
            // TODO: init game?
            GetTree().ChangeScene("res://Scenes/MainScenes/Game.tscn"); // TODO: is this calling _ReayAgain?
        }
    }
}

