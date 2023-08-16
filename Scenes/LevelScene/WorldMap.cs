using Godot;
using NewDemo.Scenes.Singletons;
using NewDemo.Sources;
using System;

namespace NewDemo.Scenes.LevelScene;

public partial class WorldMap : TileMap
{
    [Signal]
    public delegate void InteractedEventHandler(bool show, Vector2I coord);

    public override void _Ready()
    {
        var server = GetNode<Server>("/root/Server");
        server.ReceivedLevelData += _On_Server_ReceivedLevelData;
    }

    private void _On_Server_ReceivedLevelData(Level levelData)
    {
        //GD.Print("LevelData Received");
        foreach (var (coordIndex, _) in levelData.CoordIndexToHouseId)
        {
            var houseCoord = Utils.HouseCoordIndexToCoord(coordIndex);

            //GD.Print($"[Client] Placing house {houseCoord}");
            for (int y = 0; y < Constants.HouseAtlas.GetLength(0); y++)
            {
                for (int x = 0; x < Constants.HouseAtlas.GetLength(1); x++)
                {
                    var atlas = Constants.HouseAtlas[y, x];
                    var targetCoord = houseCoord - Constants.DoorPosition + new Vector2I(x, y);
                    //GD.Print($"TargetCoord: {targetCoord}, Atlas: {atlas}");
                    SetCell(1, targetCoord, 0, atlasCoords: atlas);
                }
            }
        }
    }

    public override void _Process(double delta)
    {
        var coord = LocalToMap(ToLocal(GetGlobalMousePosition()));
        var atlasCoord = GetCellAtlasCoords(1, coord);
        if (atlasCoord is { X: 1, Y: 7 })
        {
            Input.SetDefaultCursorShape(Input.CursorShape.PointingHand);
        }
        else
        {
            Input.SetDefaultCursorShape();
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is not InputEventMouseButton eventKey) return;
        if (!eventKey.IsPressed() || eventKey.ButtonIndex != MouseButton.Left) return;

        // 和门交互
        var position = GetGlobalMousePosition();
        var coord = LocalToMap(ToLocal(position));
        var atlasCoord = GetCellAtlasCoords(1, coord);
        GD.Print($"[Client] Tile clicked, position: {position}, coord: {coord}, atlas: {atlasCoord}");
        if (atlasCoord is { X: 1, Y: 7 })
        {
            EmitSignal(SignalName.Interacted, true, coord);
        }
    }
}