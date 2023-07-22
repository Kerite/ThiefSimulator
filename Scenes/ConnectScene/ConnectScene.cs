using Godot;
using NewDemo.Scenes.Singletons;

namespace NewDemo.Scenes.ConnectScene;

public partial class ConnectScene : Control
{
    public const string ScenePath = "res://Scenes/ConnectScene/ConnectScene.tscn";

    private LineEdit _ip;
    private LineEdit _port;
    private Server _server;

    public override void _Ready()
    {
        _ip = GetNode<LineEdit>("CenterContainer/VBoxContainer/GridContainer/ServerIp");
        _port = GetNode<LineEdit>("CenterContainer/VBoxContainer/GridContainer/ServerPort");
        _server = GetNode<Server>("/root/Server");
        _server.ConnectedToServer += (ip, port) =>
        {
            _server.Login(_server.PlayerId, "keep this empty is ok for now");
        };
    }

    public void Connect()
    {
        var ip = _ip.Text;
        var port = int.Parse(_port.Text);
        GetWindow().Title = "Connecting...";
        _server.Connect(ip, port);
        GetTree().ChangeSceneToFile(LevelScene.LevelScene.ScenePath);
    }
}