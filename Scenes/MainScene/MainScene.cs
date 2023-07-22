using Godot;

namespace NewDemo.Scenes.MainScene;

public partial class MainScene : Control
{
    public const string ScenePath = "res://Scenes/MainScene/MainScene.tscn";

    public override void _Ready()
    {
        GetWindow().Title = "Main";
    }

    public void _On_StartGameButton_Pressed()
    {
        GetTree().ChangeSceneToFile(ConnectScene.ConnectScene.ScenePath);
    }

    public void _On_StartServerButton_Pressed()
    {
        GetTree().ChangeSceneToFile(ServerScene.ServerScene.ScenePath);
    }

    public void _On_ExitGameButton_Pressed()
    {
        GetTree().Quit();
    }

    public void _On_OptionsButton_Pressed()
    {
    }
}