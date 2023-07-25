using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NewDemo.Models;
using Google.Protobuf;
using NewDemo.Scripts;
using Utils = NewDemo.Scripts.Utils;

namespace NewDemo.Scenes.Singletons;

public partial class Server : Node
{
    public const int Port = 11451;
    public static readonly Vector2I StartCoord = new(21, 9);
    public static readonly Vector2I HouseSize = new(4, 4);

    private ENetMultiplayerPeer _network = new();

    /// <summary>
    /// 服务端/客户端是否已经初始化
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// 当客户端时，表示是否已经连接到服务器
    /// </summary>
    public bool Connected { get; private set; }

    public string ServerIp { get; private set; } = "localhost";
    public int ServerPort { get; private set; }

    #region [Properties] Game data

    /// <summary>
    /// [Client &amp; Server] 地图信息
    /// </summary>
    public Level LevelData
    {
        get => Multiplayer.IsServer() ? ServerData.LevelData : ClientData.LevelData;
        set
        {
            if (Multiplayer.IsServer())
                ServerData.LevelData = value;
            else
                ClientData.LevelData = value;
        }
    }

    /// <summary>
    /// [Client &amp; Server] 当前回合数
    /// </summary>
    public uint Round
    {
        get => LevelData.Round;
        private set
        {
            LevelData.Round = value;

            EmitSignal(SignalName.RoundChanged, value);
        }
    }

    private ServerData? _serverData;

    private ClientData? _clientData;

    public ServerData ServerData => _serverData!;
    public ClientData ClientData => _clientData!;

    #endregion

    #region [RPC] House Operation

    /// <summary>
    /// [客户端 -> 服务端] 提交当前回合的操作
    /// <example>HouseOperation()</example>
    /// </summary>
    /// <param name="operationCode"></param>
    /// <param name="houseId"></param>
    /// <param name="secret"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void HouseOperation(int operationCode, string houseId = "", string secret = "")
    {
        if (!Multiplayer.IsServer())
        {
            GD.Print($"[Client#{ClientData.PlayerId}] Calling RPC HouseOperation");
            RpcId(1, nameof(HouseOperation), operationCode, houseId, "test secret");
            return;
        }

        var operation = (EnumPlayerOperation)operationCode;
        long peerId = Multiplayer.GetRemoteSenderId();
        if (!ServerData.PeerIdToPlayerId.ContainsKey(peerId))
        {
            OperationFailed(peerId, operation, "Not logged in");
            return;
        }

        var playerId = ServerData.PeerIdToPlayerId[peerId];
        var operationName = Enum.GetName(operation) ?? "Unknown";
        GD.Print($"[Server${peerId}] #{playerId}# Received HouseOperation RPC Call on {houseId}");

        #region Some Checks

        if (!secret.Equals("test secret"))
        {
            OperationFailed(peerId, operation, "Unauthorized RPC Call");
            return;
        }

        if (ServerData.RoundLocked)
        {
            OperationFailed(peerId, operation, "This round is locked");
            return;
        }

        if (operation != EnumPlayerOperation.StayAtHome)
        {
            if (operation == EnumPlayerOperation.Peek &&
                ServerData.PlayerDataDictionary[playerId].RemainedPeek <= 0)
            {
                OperationFailed(peerId, operation, "You have no peek chance");
                return;
            }

            if (!ServerData.HouseDataDictionary.ContainsKey(houseId))
            {
                OperationFailed(peerId, operation, "This house has no owner");
                return;
            }

            if (playerId.Equals(ServerData.HouseDataDictionary[houseId].Owner))
            {
                OperationFailed(peerId, operation, "It's your house");
                return;
            }
        }

        #endregion

        ServerData.PlayerOperation[playerId] = new PlayerOperation
        {
            HouseId = houseId,
            Operation = operation
        };
        GD.Print($"[Server${peerId}] Operation committed");
        EmitSignal(SignalName.UserOperation, peerId, operationCode, playerId, houseId);
        EmitSignal(SignalName.OperationLog, Round, peerId, playerId, operationName,
            $"Target house: {houseId} {GetHouseCoord(houseId)}");
        RpcId(peerId, nameof(HouseOperationSuccessCallBack), operationCode, GetHouseCoord(houseId));
    }

    [Rpc]
    public void HouseOperationSuccessCallBack(int operation, Vector2I coord)
    {
        EmitSignal(SignalName.OperationSuccess, operation, coord);
    }

    #endregion

    #region [RPC] Login and Logout

    /// <summary>
    /// [客户端 -> 服务端] 使用 playerId 和 playerSecret 模拟身份验证
    /// </summary>
    /// <param name="playerId">用于模拟钱包地址</param>
    /// <param name="playerSecret"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Login(string playerId, string playerSecret)
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, nameof(Login), playerId, playerSecret);
            return;
        }

        var peerId = Multiplayer.GetRemoteSenderId();

        ServerData.CreatePlayer(peerId, playerId);

        SendInventoryData(peerId, playerId);
        SendLevelData(peerId);
        EmitSignal(SignalName.UserLoginOrLogout, peerId, playerId, true);
        SendMessage(peerId, "[color=green]Login Success[/color]");
        SendMessage(peerId, $"Welcome, [color=green]{playerId}[/color]");
        var houseCoord = Utils.IndexToCoord(ServerData.PlayerDataDictionary[playerId].HouseIndex);
        houseCoord =  (houseCoord - StartCoord) / HouseSize;
        SendMessage(peerId, $"Your house is at [color=green]{houseCoord}[/color]");
    }

    /// <summary>
    /// [客户端 -> 服务端] 断开连接
    /// </summary>
    /// <param name="playerId">Ignored on client side</param>
    /// <param name="playerSecret"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void Logout(string playerId = "", string playerSecret = "Test secret")
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, nameof(Logout), ClientData.PlayerId, playerSecret);
            Initialized = false;
            return;
        }

        var peerId = Multiplayer.GetRemoteSenderId();
        ServerData.PeerIdToPlayerId.Remove(peerId);
        _network.DisconnectPeer(peerId);
        EmitSignal(SignalName.UserLoginOrLogout, peerId, playerId, false);
    }

    #endregion

    #region Server Messages

    private void OperationFailed(long peerId, EnumPlayerOperation operation, string reason)
    {
        var operationName = Enum.GetName(operation);
        GD.Print($"[Server${peerId}] Operation {operationName} Failed, reason: {reason}");
        SendMessage(peerId, $"[color=red]{operationName} failed, reason: {reason}[/color]");
    }

    /// <summary>
    /// 向指定玩家发送消息
    /// </summary>
    /// <param name="peerId">玩家的 PeerId</param>
    /// <param name="text">消息内容</param>
    private void SendMessage(long peerId, string text)
    {
        GD.Print($"[Server${peerId}] Sending Message, content: {text}");
        if (peerId != 0)
            RpcId(peerId, nameof(ServerMessage), text);
        else
            GD.PrintErr("PeerId not found");
    }

    private void SendMessage(string playerId, string text)
    {
        var peerId = ServerData.GetPeerId(playerId);
        SendMessage(peerId, text);
    }

    private void BroadcastMessage(string text)
    {
        GD.Print($"[Server] Sending Message to all peers, context: {text}");
        Rpc(nameof(ServerMessage), text);
    }

    [Rpc]
    public void ServerMessage(string text)
    {
        EmitSignal(SignalName.ServerMessageReceived, text);
    }

    #endregion

    #region [Methods] Game Progress

    /// <summary>
    /// 结束游戏
    /// </summary>
    public void FinishGame()
    {
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RoundFinished(uint newRound)
    {
        if (!Multiplayer.IsServer())
        {
            Round = newRound;
        }
    }

    /// <summary>
    /// [服务端逻辑] 结束回合，由 ServerScene 手动触发
    /// </summary>
    public void FinishRound()
    {
        if (!Multiplayer.IsServer()) return;

        GD.Print("Finish Round received");
        ServerData.RoundLocked = true;
        if (InnerFinishRound())
        {
            BroadcastMessage($"{Constants.ColorTagRoundFinished}Round {Round} Finished{Constants.ColorTagEnd}");
            Round++;
            Rpc(nameof(RoundFinished), Round);
            EmitSignal(SignalName.OperationLog, Round - 1, 1, "Server", nameof(FinishRound), "");
        }

        ServerData.RoundLocked = false;
    }

    /// <summary>
    /// 回合结束的实际逻辑
    /// </summary>
    /// <returns>是否继续下一回合</returns>
    private bool InnerFinishRound()
    {
        foreach (var (_, playerId) in ServerData.PeerIdToPlayerId)
        {
            if (ServerData.PlayerOperation.ContainsKey(playerId)) continue;

            GD.PrintErr("[Server] Not all player is ready");
            OS.Alert("Not all player is ready", "Not Ready");
            return false;
        }

        // 获取概览
        var houseIdToPlayerIdEntered = InnerFinishRound_Summary();

        // 结算偷窃金钱
        foreach (var (houseId, playerList) in houseIdToPlayerIdEntered)
        {
            GD.Print("[Server] 结算房子：", houseId, "中...");
            var owner = ServerData.HouseDataDictionary[houseId].Owner;
            var houseMoney = ServerData.HouseDataDictionary[houseId].HouseMoney;
            var moneyToSteel = houseMoney / playerList.Count;
            foreach (var playerId in playerList)
            {
                GD.Print("[Server] 玩家 ", playerId, "进入");
                ServerData.PlayerDataDictionary[playerId].Moneys += (uint)moneyToSteel;
                SendMessage(playerId,
                    $"{Constants.ColorGoldGot}You Stole {moneyToSteel} Golds.{Constants.ColorTagEnd}");
                SendMessage(owner,
                    $"{Constants.ColorGoldLost}Someone Stole {moneyToSteel} Golds from your house.{Constants.ColorTagEnd}");
            }
        }

        // 向 登录的Peer 发送消息
        foreach (var (peerId, playerId) in ServerData.PeerIdToPlayerId)
        {
            SendLevelData(peerId);
            SendInventoryData(peerId, playerId);
        }

        ServerData.PlayerOperation.Clear();
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>房子 => 进入该房子的玩家</returns>
    private Dictionary<string, List<string>> InnerFinishRound_Summary()
    {
        var houseIdToPlayersEntered = new Dictionary<string, List<string>>();

        // Traverse all peers logged in
        // 结算遍历玩家
        foreach (var (peerId, playerId) in ServerData.PeerIdToPlayerId)
        {
            var operation = ServerData.PlayerOperation[playerId];

            if (operation.Operation == EnumPlayerOperation.StayAtHome)
            {
                // 玩家在家，扣除房租（水电费）
                var rent = Utils.CalculateRent(Round);
                ServerData.PlayerDataDictionary[playerId].Moneys -= rent;
                SendMessage(peerId, $"{Constants.ColorGoldLost}You paid {rent} for rent [/color]");
            }
            else
            {
                var targetHouseId = operation.HouseId;
                var targetOwnerId = ServerData.HouseDataDictionary[targetHouseId].Owner;
                var targetOwnerOperation = ServerData.PlayerOperation[targetOwnerId].Operation;

                if (operation.Operation == EnumPlayerOperation.Steel)
                {
                    if (targetOwnerOperation != EnumPlayerOperation.StayAtHome)
                    {
                        houseIdToPlayersEntered.GetOrCreate(targetHouseId).Add(playerId);
                    }
                    else
                    {
                        // 目标主人在家
                        var lostGolds = ServerData.Caught(playerId);
                        ServerData.PlayerDataDictionary[targetOwnerId].Moneys += lostGolds;

                        SendMessage(peerId,
                            $"{Constants.ColorGoldLost}You were caught, lost {lostGolds} Golds{Constants.ColorTagEnd}");
                        SendMessage(targetOwnerId,
                            $"{Constants.ColorGoldGot}You caught a thief, got {lostGolds} Golds{Constants.ColorTagEnd}");
                    }
                }
                else if (operation.Operation == EnumPlayerOperation.Peek)
                {
                    if (targetOwnerOperation != EnumPlayerOperation.StayAtHome)
                    {
                        // 目标主人不在家
                        houseIdToPlayersEntered.GetOrCreate(targetHouseId).Add(playerId);
                        SendMessage(peerId, $"{Constants.ColorPeekResult}Peek result: target is not at home[/color]");
                    }
                    else
                    {
                        SendMessage(peerId, $"{Constants.ColorPeekResult}Peek result: target is at home[/color]");
                    }

                    ServerData.PlayerDataDictionary[playerId].RemainedPeek--;
                }
            }
        }

        return houseIdToPlayersEntered;
    }

    #endregion

    #region Server/Client Creation

    /// <summary>
    /// 连接到服务器
    /// </summary>
    /// <param name="ip">服务器IP</param>
    /// <param name="port">服务器端口</param>
    public void Connect(string ip = "localhost", int port = Port)
    {
        if (Initialized)
        {
            GD.PrintErr("[Server] Already created.");
            return;
        }

        ServerIp = ip;
        ServerPort = port;

        _network.CreateClient(ip, port);
        _clientData = new();

        Multiplayer.MultiplayerPeer = _network;
        Initialized = true;
    }

    /// <summary>
    /// 创建服务器
    /// </summary>
    /// <param name="mapWidth">地图宽度</param>
    /// <param name="mapHeight">地图高度</param>
    /// <param name="port">监听的端口</param>
    public void CreateServer(int mapWidth = 8, int mapHeight = 8, int port = Port)
    {
        if (Initialized)
        {
            GD.PrintErr("Already created.");
            return;
        }

        _network.CreateServer(port);

        Multiplayer.MultiplayerPeer = _network;
        _serverData = new ServerData(StartCoord, HouseSize, mapWidth, mapHeight);

        Initialized = true;
        GD.Print("[Server] Started");
    }

    #endregion

    #region [Event, RPC] 同步物品信息

    public delegate void ReceivedInventoryDataEventHandler(Inventory inventoryData);

    public event ReceivedInventoryDataEventHandler? ReceivedInventoryData;

    /// <summary>
    /// [客户端 -> 服务端] 请求更新物品栏
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RpcSyncInventoryDataRequest()
    {
        if (Multiplayer.IsServer())
        {
            GD.Print("Received RefreshInventory Request From Client");
            var peerId = Multiplayer.GetRemoteSenderId();
            var playerId = ServerData.PeerIdToPlayerId[peerId];

            var inventoryData = ServerData.PlayerDataDictionary[playerId];
            var inventoryBytes = new byte[inventoryData.CalculateSize()];
            using (var output = new CodedOutputStream(inventoryBytes))
            {
                inventoryData.WriteTo(output);
            }

            RpcId(peerId, nameof(RpcSyncInventoryDataReturn), inventoryBytes);
        }
        else
        {
            RpcId(1, nameof(RpcSyncInventoryDataRequest));
        }
    }

    /// <summary>
    /// [服务端 -> 客户端] 收到更新物品栏
    /// </summary>
    /// <param name="inventoryData">Bytes of <see cref="Inventory"/></param>
    [Rpc]
    public void RpcSyncInventoryDataReturn(byte[] inventoryData)
    {
        if (Multiplayer.IsServer())
        {
            GD.PrintErr("SyncInventoryReturn can only be called on client side");
            return;
        }

        GD.Print($"[Client#{_clientData!.PlayerId}] Received InventoryData From Server");
        var inventory = Inventory.Parser.ParseFrom(inventoryData);
        ReceivedInventoryData?.Invoke(inventory);
    }

    /// <summary>
    /// 手动触发同步物品栏（服务端）
    /// </summary>
    /// <param name="peerId"></param>
    /// <param name="playerId"></param>
    private void SendInventoryData(long peerId, string playerId)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr($"[Client] {nameof(SendInventoryData)} can only be called on server side");
            return;
        }

        GD.Print($"[Server${peerId}] Sending Inventory data to");
        var inventory = ServerData.PlayerDataDictionary[playerId];
        var inventoryBytes = new byte[inventory.CalculateSize()];
        using (var stream = new CodedOutputStream(inventoryBytes))
        {
            inventory.WriteTo(stream);
        }

        RpcId(peerId, nameof(RpcSyncInventoryDataReturn), inventoryBytes);
    }

    #endregion

    #region [Event, RPC] 同步地图信息

    public delegate void ReceivedLevelDataEventHandler(Level levelData);

    public event ReceivedLevelDataEventHandler? ReceivedLevelData;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RpcSyncLevelDataRequest()
    {
        if (Multiplayer.IsServer())
        {
            GD.Print("Received RefreshLevel Request From Client");
            var bytes = new byte[LevelData.CalculateSize()];
            using var output = new CodedOutputStream(bytes);
            LevelData.WriteTo(output);
            RpcId(Multiplayer.GetRemoteSenderId(), nameof(RpcSyncLevelDataReturn), bytes);
        }
        else
        {
            RpcId(1, nameof(RpcSyncLevelDataRequest));
        }
    }

    [Rpc]
    public void RpcSyncLevelDataReturn(byte[] levelData)
    {
        if (Multiplayer.IsServer())
        {
            GD.PrintErr();
            return;
        }

        GD.Print($"[Client#{_clientData!.PlayerId}] Received LevelData From Server");
        var level = Level.Parser.ParseFrom(levelData);
        LevelData = level;
        ReceivedLevelData?.Invoke(level);
    }

    /// <summary>
    /// 手动触发同步物品栏（服务端）
    /// </summary>
    /// <param name="peerId"></param>
    private void SendLevelData(long peerId)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("[Client#{PlayerId}] can only be called on server side");
            return;
        }

        GD.Print($"[Server${peerId}] Sending Level data");
        var levelBytes = new byte[LevelData.CalculateSize()];
        using (var stream = new CodedOutputStream(levelBytes))
        {
            LevelData.WriteTo(stream);
        }

        RpcId(peerId, nameof(RpcSyncLevelDataReturn), levelBytes);
    }

    #endregion

    #region [Signals]

    [Signal]
    public delegate void ConnectedToServerEventHandler(string ip, int port);

    [Signal]
    public delegate void DisconnectedFromServerEventHandler(string ip, int port);

    [Signal]
    public delegate void RoundChangedEventHandler(uint currentRound);

    [Signal]
    public delegate void UserLoginOrLogoutEventHandler(long peerId, string playerId, bool isLogin);

    [Signal]
    public delegate void ServerMessageReceivedEventHandler(string messageContent);

    [Signal]
    public delegate void OperationSuccessEventHandler(int operation, Vector2I coord);

    [Signal]
    public delegate void UserOperationEventHandler(long peerId, int operation, string playerId, string houseId);

    [Signal]
    public delegate void OperationLogEventHandler(uint round, long peerId, string playerId, string operation,
        string details);

    #endregion

    /// <summary>
    /// 
    /// </summary>
    /// <param name="count"></param>
    /// <param name="transferToHouse"></param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RpcTransferGolds(uint count, bool transferToHouse)
    {
        if (!Multiplayer.IsServer())
        {
            RpcId(1, nameof(RpcTransferGolds), count, transferToHouse);
            return;
        }

        var peerId = Multiplayer.GetRemoteSenderId();
        var playerId = ServerData.PeerIdToPlayerId[peerId];
        var houseId = ServerData.PlayerDataDictionary[playerId].HouseId;
        var houseMoney = ServerData.HouseDataDictionary[houseId].HouseMoney;

        switch (transferToHouse)
        {
            case true when ServerData.PlayerDataDictionary[playerId].Moneys < count:
                SendMessage(peerId, $"[color=red]You don't have enough money in your inventory[/color]");
                break;
            case false when houseMoney < count:
                SendMessage(peerId, $"[color=red]You don't have enough money in your house ({houseMoney})[/color]");
                break;
            case true:
                ServerData.PlayerDataDictionary[playerId].Moneys -= count;
                ServerData.HouseDataDictionary[houseId].HouseMoney += count;
                SendMessage(peerId, $"[color=green]Transfer {count} to house[/color]");
                break;
            default:
                ServerData.PlayerDataDictionary[playerId].Moneys += count;
                ServerData.HouseDataDictionary[houseId].HouseMoney -= count;
                SendMessage(peerId, $"[color=green]Transfer {count} from house[/color]");
                break;
        }

        SendInventoryData(peerId, playerId);
        var detail = $"Transfer {count} {(transferToHouse ? "to" : "from")} house at {GetHouseCoord(houseId)}";
        EmitSignal(SignalName.OperationLog, Round, peerId, playerId, "TransferGolds", detail);
    }

    /// <summary>
    /// 从坐标获取房屋 ID
    /// </summary>
    /// <param name="coord">房屋坐标</param>
    /// <returns>房屋的ID</returns>
    /// <exception cref="ArgumentOutOfRangeException">当坐标X或Y小于0时抛出</exception>
    public string GetHouseId(Vector2I coord)
    {
        var index = Utils.CoordToIndex(coord);
        var houseId = LevelData.CoordsToHouseId[index];
        GD.Print($"{coord} => {index} ({houseId})");
        return houseId;
    }

    public Vector2I GetHouseCoord(string houseId)
    {
        var index = LevelData.CoordsToHouseId
            .FirstOrDefault(x => x.Value.Equals(houseId))
            .Key;
        return Utils.IndexToCoord(index);
    }

    public override void _Ready()
    {
        Multiplayer.ConnectedToServer += _On_Server_Connected;
        Multiplayer.ServerDisconnected += _On_Server_Disconnected;
        Multiplayer.PeerDisconnected += _On_Peer_Disconnected;
        Multiplayer.PeerConnected += _On_Peer_Connected;
    }

    public void _On_Server_Connected()
    {
        Connected = true;
        GD.Print("[Client] Server Connected");
        EmitSignal(SignalName.ConnectedToServer, ServerIp, ServerPort);
    }

    public void _On_Server_Disconnected()
    {
        Connected = false;
        GD.Print("[Client] Server Disconnected");
        EmitSignal(SignalName.DisconnectedFromServer, ServerIp, ServerPort);
    }

    public void _On_Peer_Connected(long peerId)
    {
        if (peerId == 1) return;
        GD.Print($"[Server${peerId}] Connected.");
    }

    public void _On_Peer_Disconnected(long peerId)
    {
        if (peerId == 1) return;
        GD.Print($"[Server${peerId}] Disconnected.");
    }
}