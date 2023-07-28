using System;

namespace NewDemo.Models;

public class ClientData
{
    /// <summary>
    /// [Client &amp; Server] 地图信息
    /// </summary>
    public Level LevelData = new();

    public string PlayerId { get; set; } = "";

    /// <summary>
    /// 客户端ID，仅在登录时用到
    /// </summary>
    public string ClientId { get; } = Guid.NewGuid().ToString();
}