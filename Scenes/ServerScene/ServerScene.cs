using System;
using System.IO;
using System.Linq;
using Godot;
using NewDemo.Models;
using NewDemo.Scenes.Singletons;

namespace NewDemo.Scenes.ServerScene;

public partial class ServerScene : Node
{
    public const string ScenePath = "res://Scenes/ServerScene/ServerScene.tscn";

    private Server? _server;
    private VBoxContainer? _peerList;
    private Label? _currentRound;
    private GridContainer? _operationHistory;
    private ScrollContainer? _operationHistoryScroll;
    private Button? _exportButton;
    private FileDialog? _fileDialog;

    public override void _Ready()
    {
        _server = GetNode<Singletons.Server>("/root/Server");
        _peerList = GetNode<VBoxContainer>("%PeerList");
        _currentRound = GetNode<Label>("%CurrentRoundLabel");
        _operationHistory = GetNode<GridContainer>("%OperationHistory");
        _operationHistoryScroll = GetNode<ScrollContainer>("%OperationHistoryScroll");
        _fileDialog = GetNode<FileDialog>("FileDialog");
        var scrollBar = _operationHistoryScroll.GetVScrollBar();
        scrollBar.Changed += () => { _operationHistoryScroll.ScrollVertical = (int)scrollBar.MaxValue; };

        _server.CreateServer();
        _server.Multiplayer.PeerConnected += _On_Server_PeerConnected;

        _server.Multiplayer.PeerDisconnected += (peerId) =>
        {
            GD.Print("Removing Peer list item");
            var label = GetNode<ServerPeerListItemScene.ServerPeerListItemScene>(
                $"MarginContainer/LeftPanel/VBoxContainer/PeerListContainer/PeerList/{peerId.ToString()}");
            _peerList.RemoveChild(label);
        };
        _server.OperationLog += AddLog;
        GetWindow().Title = "Server Mode";
    }

    public override void _Process(double delta)
    {
    }

    public void _On_FinishRoundButton_Pressed()
    {
        _server!.FinishRound();
    }

    public void _On_FinishGameButton_Pressed()
    {
        _server!.FinishGame();
    }

    public void AddLog(uint round, long peerId, string playerId, string operation, string detail, Vector2I targetHouseGridCoord)
    {
        _operationHistory!.AddChild(new Label
        {
            Text = round.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        var peerIdLabel = new Label
        {
            Text = peerId.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        peerIdLabel.AddThemeColorOverride("font_color", Color.Color8(255, 0, 0));
        _operationHistory.AddChild(peerIdLabel);

        var playerIdLabel = new Label
        {
            Text = playerId,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        playerIdLabel.AddThemeColorOverride("font_color", Color.Color8(0, 255, 0));
        _operationHistory.AddChild(playerIdLabel);

        _operationHistory.AddChild(new Label
        {
            Text = DateTime.Now.ToString("HH:mm:ss"),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        var operationLabel = new Label
        {
            Text = operation,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TooltipText = detail,
            MouseFilter = Control.MouseFilterEnum.Stop
        };
        operationLabel.AddThemeColorOverride("font_color", Color.Color8(0, 100, 100));
        _operationHistory.AddChild(operationLabel);

        _operationHistory.AddChild(new Label
        {
            Text = targetHouseGridCoord.ToString(),
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });
    }

    public void Save(string path)
    {
        using var fs = new FileStream(path, FileMode.Create);
        {
            var nodes = _operationHistory!.GetChildren();
            foreach (var (index, item) in nodes.Select((value, i) => (i, value)))
            {
                var labelItem = (Label)item;
                if (index % 5 == 1 || index % 5 == 0)
                {
                    // PeerId
                    fs.Write($"{labelItem.Text},".ToAsciiBuffer());
                }
                else
                {
                    fs.Write($"\"{labelItem.Text}\",".ToAsciiBuffer());
                }

                if (index % 5 == 4)
                {
                    fs.Write($"\"{labelItem.TooltipText}\"".ToAsciiBuffer());
                    fs.Write("\n".ToAsciiBuffer());
                }
            }
        }
    }

    public void _On_ExportButton_Pressed()
    {
        _fileDialog!.Show();
    }

    public void _On_Server_PeerConnected(long peerId)
    {
        var peerItem =
            ServerPeerListItemScene.ServerPeerListItemScene.Create(peerId, "Not logged in", EnumPlayerOperation.None);
        peerItem.Name = peerId.ToString();
        _server!.UserLoginOrLogout += (id, playerId, login) =>
        {
            if (id != peerId || !login)
                return;
            peerItem.PlayerId = playerId;
        };
        _server.UserOperation += (_, operation, playerId, _) =>
        {
            if (peerItem.PlayerId.Equals(playerId))
            {
                peerItem.Operation = (EnumPlayerOperation)operation;
            }
        };
        _server.RoundChanged += newRound =>
        {
            peerItem.Operation = EnumPlayerOperation.None;
            _currentRound!.Text = newRound.ToString();
        };
        _peerList!.AddChild(peerItem);
    }
}