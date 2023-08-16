using Godot;

namespace NewDemo.Sources;

public static class Constants
{
    public const string ColorTagEnd = "[/color]";
    public const string ColorTagRoundFinished = "[color=blue]";

    public const string ColorGoldGot = "[color=green]";
    public const string ColorGoldLost = "[color=red]";

    public const string ColorPeekResult = "[color=yellow]";

    public static readonly Vector2I[,] HouseAtlas = new Vector2I[4, 4]{
        { new Vector2I(0,4), new Vector2I(1,4), new Vector2I(1,4), new Vector2I(2,4) },
        { new Vector2I(0,5), new Vector2I(3,5), new Vector2I(1,5), new Vector2I(2,5) },
        { new Vector2I(0,6), new Vector2I(0,7), new Vector2I(1,6), new Vector2I(3,6) },
        { new Vector2I(0,6), new Vector2I(1,6), new Vector2I(1,7), new Vector2I(3,6) }
    };

    /// <summary>
    /// 门在房子的位置
    /// </summary>
    public static readonly Vector2I DoorPosition = new(2, 3);
}