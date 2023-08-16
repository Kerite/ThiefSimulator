using Godot;

namespace NewDemo.Sources;
public static class Configs
{
    public const int MessageDuration = 1500;

    public static int Port = 11451;

    /// <summary>
    /// 左上角的房子的坐标
    /// </summary>
    public static Vector2I StartCoord = new(21, 9);

    /// <summary>
    /// 每个房子的大小
    /// </summary>
    public static Vector2I GridSize = new(4, 4);

    /// <summary>
    /// 定义地图的大小（单位：网格 Grid）
    /// </summary>
    public static Vector2I MapSize = new(4, 4);

    #region Server Configs
    public static readonly ServerConfigModel ServerConfig = new()
    {
        FinishRounds = 10,
        InitialGolds = 100_000,
        InitialHouseGolds = 5_000,
        InitialPeekChance = 3,
        InitialKeys = 10,
        BotsStaysAtHomeThreshold = 120_000
    };
    #endregion
}
