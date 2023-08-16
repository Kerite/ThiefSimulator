using Godot;
using NewDemo.Scenes.MainScene;
using NewDemo.Scenes.Singletons;
using System;
using System.Linq;

public partial class GameFinishedScene : Control
{
    private VBoxContainer? _rankList;
    private Server? _server;

    public VBoxContainer RankList => _rankList!;
    public Server Server => _server!;

    public override void _Ready()
    {
        _server = GetNode<Server>("/root/Server");
        _server.OnGameFinished += _On_Server_OnGameFinished;

        _rankList = GetNode<VBoxContainer>("%RankList");
    }

    private void _On_Server_OnGameFinished(GameFinishedMessage message)
    {
        Visible = true;
        var rank = 0;
        foreach (var (element, index) in message.Rank.Select((element, index) => (element, index)))
        {
            rank++;
            var isSelf = element.PlayerId.Equals(Server.ClientData.PlayerId);
            var label = new Label()
            {
                Text = $"#{rank} {element.PlayerId} : {element.Golds + element.GoldsInHouse} ({element.Golds} + {element.GoldsInHouse})",
            };
            if (isSelf)
            {
                label.AddThemeColorOverride("font_color", Color.Color8(0, 255, 0));
            }
            RankList.AddChild(label);
        }
    }

    public void _On_BackToMainMenuButton_Pressed()
    {
        GetTree().ChangeSceneToFile(MainScene.ScenePath);
        Server.Logout();
    }
}
