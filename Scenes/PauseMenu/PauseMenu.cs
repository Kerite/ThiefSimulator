using Godot;
using NewDemo.Scenes.Singletons;

namespace NewDemo.Scenes.PauseMenu;

public partial class PauseMenu : Control
{
    private Server _server;
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event.IsActionPressed("pause"))
        {
            Visible = !Visible;
        }
    }

    public void _On_ResumeButton_Pressed()
    {
        Visible = false;
    }

    public void _On_DisconnectButton_Pressed()
    {
        GD.Print("Returning main menu");
        GetTree().ChangeSceneToFile(MainScene.MainScene.ScenePath);
        _server.Logout();
    }

    public override void _Ready()
    {
        Visible = false;
        _server = GetNode<Server>("/root/Server");
    }
}