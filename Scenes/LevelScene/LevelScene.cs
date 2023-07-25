using System;
using Godot;
using NewDemo.Models;
using NewDemo.Scenes.Singletons;

namespace NewDemo.Scenes.LevelScene;

public partial class LevelScene : Node2D
{
    public const string ScenePath = "res://Scenes/LevelScene/LevelScene.tscn";
    public const int MoveSpeed = 1000;

    private Camera2D? _camera;
    private Node2D? _cameraCenter;
    private WorldMap? _groundTileMap;
    private InteractMenu.InteractMenu? _interactMenu;
    private Server? _server;

    private Vector2I? _houseCoord;

    public override void _Process(double delta)
    {
        // Update Camera Position
        var vec = Input.GetVector("left", "right", "up", "down");
        if (vec is { X: 0, Y: 0 }) return;
        
        var newPosition = _cameraCenter!.GlobalPosition + vec * MoveSpeed * (float)delta;
        newPosition.X = Math.Clamp(newPosition.X, 0, 1000);
        newPosition.Y = Math.Clamp(newPosition.Y, 0, 500);
        _cameraCenter.GlobalPosition = newPosition;
        _interactMenu!.Visible = false;
    }

    public override void _Ready()
    {
        _groundTileMap = GetNode<WorldMap>("GroundTileMap");
        _camera = GetNode<Camera2D>("CameraCenter/Camera2D");
        _cameraCenter = GetNode<Node2D>("CameraCenter/Camera2D");
        _server = GetNode<Server>("/root/Server");
        _server.Multiplayer.ServerDisconnected += _On_Server_Disconnected;
        _server.Multiplayer.ConnectedToServer += _On_Server_Connected;
        _interactMenu = GetNode<InteractMenu.InteractMenu>("InteractMenu");
        _interactMenu.MenuInteracted += _On_InteractMenu_Interacted;
    }

    private void _On_TileMap_Interacted(bool show, Vector2I tileCoord)
    {
        if (show)
        {
            ShowInteractMenu(tileCoord);
            _houseCoord = tileCoord;
        }
        else
        {
            _interactMenu!.Visible = false;
            _houseCoord = null;
        }
    }

    private void _On_InteractMenu_Interacted(InteractMenu.InteractMenu.EnumMenu menu)
    {
        switch (menu)
        {
            case InteractMenu.InteractMenu.EnumMenu.Steel:
            {
                if (_houseCoord.HasValue)
                {
                    _server!.HouseOperation((int)EnumPlayerOperation.Steel, _server.GetHouseId(_houseCoord.Value));
                }

                break;
            }
            case InteractMenu.InteractMenu.EnumMenu.Peek:
            {
                if (_houseCoord.HasValue)
                {
                    _server!.HouseOperation((int)EnumPlayerOperation.Peek, _server.GetHouseId(_houseCoord.Value));
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(menu), menu, null);
        }
    }

    public void ShowInteractMenu(Vector2I tileCoord)
    {
        var position = _groundTileMap!.MapToLocal(tileCoord);
        _interactMenu!.Visible = true;
        _interactMenu!.Position = position;
        // GD.Print($"[Client] Moved interact menu to {position}");
    }

    public void _On_Server_Connected()
    {
        GetWindow().Title = $"[{_server!.ClientData.PlayerId}] @ {_server.ServerIp}:{_server.ServerPort}";
    }

    public void _On_Server_Disconnected()
    {
        GetTree().ChangeSceneToFile(ConnectScene.ConnectScene.ScenePath);
        GetWindow().Title = "No connection";
    }
}