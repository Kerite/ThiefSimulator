using Godot;

namespace NewDemo.Scenes.LevelScene;

public partial class WorldMap : TileMap
{
    [Signal]
    public delegate void InteractedEventHandler(bool show, Vector2I coord);

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
        if (atlasCoord is { X: 1, Y: 7 })
        {
            EmitSignal(SignalName.Interacted, true, coord);
        }
    }
}