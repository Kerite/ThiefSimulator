using System;
using Godot;
using Microsoft.VisualBasic.CompilerServices;
using NewDemo.Models;

namespace NewDemo.Scenes.ServerScene.ServerPeerListItemScene;

#nullable enable
public partial class ServerPeerListItemScene : HBoxContainer
{
    public const string ScenePath = @"res://Scenes/ServerScene/ServerPeerListItemScene/ServerPeerListItemScene.tscn";

    private string _playerId = "";

    public string PlayerId
    {
        get => _playerId;
        set
        {
            _playerId = value;
            if (_playerIdLabel is not null)
            {
                _playerIdLabel.Text = value;
            }
        }
    }

    private long _peerId;

    public long PeerId
    {
        get => _peerId;
        set
        {
            _peerId = value;
            if (_peerIdLabel is not null)
            {
                _peerIdLabel.Text = value.ToString();
            }
        }
    }

    private EnumPlayerOperation _operation = EnumPlayerOperation.None;

    public EnumPlayerOperation Operation
    {
        get => _operation;
        set
        {
            _operation = value;
            if (_operationLabel is not null)
            {
                _operationLabel.Text = Enum.GetName(_operation);
            }
        }
    }

    private Label? _operationLabel;
    private Label? _peerIdLabel;
    private Label? _playerIdLabel;

    public override void _Ready()
    {
        _peerIdLabel = GetNode<Label>("PeerId");
        _peerIdLabel.Text = _peerId.ToString();
        _playerIdLabel = GetNode<Label>("PlayerId");
        _playerIdLabel.Text = _playerId;
        _operationLabel = GetNode<Label>("Operation");
        _operationLabel.Text = Enum.GetName(_operation);
    }

    public static ServerPeerListItemScene Create(long peerId, string playerId, EnumPlayerOperation operation)
    {
        var scene = GD.Load<PackedScene>(ScenePath).Instantiate();
        var peerItem = (ServerPeerListItemScene)scene;
        peerItem.PeerId = peerId;
        peerItem.PlayerId = playerId;
        peerItem.Operation = operation;
        return peerItem;
    }
}
#nullable restore