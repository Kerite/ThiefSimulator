using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using NewDemo.Sources;

namespace NewDemo.Models;

/// <summary>
/// 用于存储服务器的数据
/// </summary>
public class ServerData
{
    /// <summary>
    /// 地图信息
    /// </summary>
    public Level LevelData = new();

    /// <summary>
    /// 回合是否锁定（结算中）
    /// </summary>
    public bool RoundLocked;

    /// <summary>
    /// 玩家ID => 玩家当前回合的操作
    /// </summary>
    public readonly Dictionary<string, PlayerOperation> PlayerOperation = new();

    /// <summary>
    /// PeerId => （已登录的）玩家 ID，如果 PeerId <![CDATA[<]]> 0，则为 Bot
    /// </summary>
    public readonly Dictionary<long, string> PeerIdToPlayerId = new();

    /// <summary>
    /// 房子ID => 到房子信息
    /// </summary>
    public readonly Dictionary<string, House> HouseDataDictionary = new();

    /// <summary>
    /// 玩家ID => 玩家信息
    /// </summary>
    public readonly Dictionary<string, Inventory> PlayerDataDictionary = new();

    /// <summary>
    /// 客户端ID => 玩家ID
    /// </summary>
    public readonly Dictionary<string, string> ClientIdToPlayerId = new();

    public ServerData(Vector2I startCoord, Vector2I houseSize, int mapWidth, int mapHeight, bool withBots = false)
    {
        long botPeerId = -1;
        LevelData.Round = 1;
        LevelData.CoordIndexToHouseId.Clear();

        // Traverse all tiles and assign a random GUID to each house
        foreach (var gridCoordY in Enumerable.Range(0, Configs.MapSize.Y))
            foreach (var gridCoordX in Enumerable.Range(0, Configs.MapSize.X))
            {
                // Assign a random GUID to a house as its id
                var houseCoord = Utils.HouseGridCoordToCoord(gridCoordX, gridCoordY);
                var houseCoordIndex = Utils.HouseCoordToCoordIndex(houseCoord);
                var houseId = Guid.NewGuid().ToString();
                LevelData.CoordIndexToHouseId[houseCoordIndex] = houseId;
                if (withBots)
                {
                    var playerId = Guid.NewGuid().ToString();

                    // 添加一所房子给新玩家
                    HouseDataDictionary[houseId] = new House
                    {
                        Owner = playerId,
                        HouseMoney = Configs.InitialHouseGolds
                    };

                    PeerIdToPlayerId[botPeerId] = playerId;
                    PlayerDataDictionary[playerId] = new Inventory
                    {
                        Keys = Configs.InitialKeys,
                        Moneys = Configs.InitialGolds,
                        RemainedPeek = Configs.InitialPeekChance,
                        HouseId = houseId,
                        HouseCoordIndex = houseCoordIndex
                    };

                    GD.Print($"[Server] Created house {houseId} at {houseCoord}, Bot${-botPeerId}'s playerId: {playerId}");
                    botPeerId--;
                }
                else
                {
                    GD.Print($"[Server] Created house {houseId} at {houseCoord}");
                }
            }
    }

    public long GetPeerId(string playerId)
    {
        return PeerIdToPlayerId
            .FirstOrDefault(pair => playerId.Equals(pair.Value))
            .Key;
    }

    /// <summary>
    /// 减少玩家金钱
    /// </summary>
    /// <returns>玩家减少的金钱</returns>
    public uint Caught(string playerId)
    {
        var lostMoney = PlayerDataDictionary[playerId].Moneys / 2;
        PlayerDataDictionary[playerId].Moneys -= lostMoney;
        return lostMoney;
    }

    /// <summary>
    /// 找到第一个 Bot 的玩家 ID
    /// </summary>
    /// <returns></returns>
    public Tuple<long, string> GetFirstBot()
    {
        foreach (var (key, value) in PeerIdToPlayerId)
        {
            if (key < 0)
            {
                return Tuple.Create(key, value);
            }
        }
        return Tuple.Create(0L, "");
    }
}