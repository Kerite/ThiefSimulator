using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NewDemo.Scripts;

namespace NewDemo.Models;

public class ServerData
{
    public const int InitialKeys = 20;
    public const int InitialMoney = 100000;
    public const int InitialHouseMoney = 5000;
    public const int InitialPeekChance = 3;

    private int _tempHouseIndex;

    /// <summary>
    /// [Client &amp; Server] 地图信息
    /// </summary>
    public Level LevelData = new();

    /// <summary>
    /// [Server] 回合是否锁定（结算中）
    /// </summary>
    public bool RoundLocked;

    /// <summary>
    /// [Server] 玩家ID => 玩家当前回合的操作
    /// </summary>
    public readonly Dictionary<string, PlayerOperation> PlayerOperation = new();

    /// <summary>
    /// [Server] PeerId => （已登录的）玩家ID
    /// </summary>
    public readonly Dictionary<long, string> PeerIdToPlayerId = new();

    /// <summary>
    /// [Server] 房子ID => 到房子信息
    /// </summary>
    public readonly Dictionary<string, House> HouseDataDictionary = new();

    /// <summary>
    /// [Server] 玩家ID => 玩家信息
    /// </summary>
    public readonly Dictionary<string, Inventory> PlayerDataDictionary = new();

    public ServerData(Vector2I startCoord, Vector2I houseSize, int mapWidth, int mapHeight)
    {
        LevelData.Round = 1;
        LevelData.CoordsToHouseId.Clear();

        // Traverse all tiles and assign a random GUID to each house
        foreach (var y in Enumerable.Range(0, mapHeight))
        foreach (var x in Enumerable.Range(0, mapWidth))
        {
            // Assign a random GUID to a house as its id
            var coordX = startCoord.X + x * houseSize.X;
            var coordY = startCoord.Y + y * houseSize.Y;
            var houseId = Guid.NewGuid()
                .ToString();
            GD.Print($"[Server] Created house {houseId} at {coordX}, {coordY}");
            LevelData.CoordsToHouseId[Utils.CoordToIndex(coordX, coordY)] = houseId;
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

    public void CreatePlayer(long peerId, string playerId)
    {
        var reconnecting = false;
        if (PeerIdToPlayerId.ContainsKey(peerId))
        {
            GD.PrintErr($"[Server${peerId}] Already logged in");
            return;
        }

        if (PeerIdToPlayerId.ContainsValue(playerId))
        {
            GD.Print($"[Server${peerId}] {playerId} reconnection");
            reconnecting = true;
        }

        if (!reconnecting)
        {
            var (index, houseId) = LevelData.CoordsToHouseId.ElementAt(_tempHouseIndex);
            _tempHouseIndex++;

            // 添加一所房子给新玩家
            HouseDataDictionary[houseId] = new House
            {
                Owner = playerId,
                HouseMoney = InitialHouseMoney
            };

            GD.Print($"[Server${peerId}] Assigned house {houseId} at {Utils.IndexToCoord(index)} to player {playerId}");

            PeerIdToPlayerId[peerId] = playerId;
            PlayerDataDictionary[playerId] = new Inventory
            {
                Keys = InitialKeys,
                Moneys = InitialMoney,
                RemainedPeek = InitialPeekChance,
                HouseId = houseId,
                HouseIndex = index
            };
        }
        else
        {
            GD.Print($"[Server${peerId}] Reconnected");
        }
    }
}