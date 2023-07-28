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
    public static int HouseMoney = 0;

    /// <summary>
    /// 初始的钥匙数量
    /// </summary>
    public static uint InitialKeys = 20;

    /// <summary>
    /// 初始的金钱数量
    /// </summary>
    public static uint InitialGolds = 100000;

    /// <summary>
    /// 初始的房屋金钱
    /// </summary>
    public static uint InitialHouseGolds = 5000;

    /// <summary>
    /// 初始的 Peek 次数
    /// </summary>
    public static uint InitialPeekChance = 3;
    #endregion
}
