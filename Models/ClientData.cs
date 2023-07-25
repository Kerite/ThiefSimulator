using System;

namespace NewDemo.Models;

public class ClientData
{
    /// <summary>
    /// [Client &amp; Server] 地图信息
    /// </summary>
    public Level LevelData = new();

    public string PlayerId { get; private set; } = Guid.NewGuid().ToString();

    public int HouseMoney = 0;
}