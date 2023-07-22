using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using NewDemo.Models;
using Google.Protobuf;
using Utils = NewDemo.Scripts.Utils;

namespace NewDemo.Scenes.Singletons;

public partial class Server : Node
{
    public const int Port = 11451;
    public static readonly Vector2I StartCoord = new(21, 9);
    public static readonly Vector2I HouseSize = new(4, 4);

    private ENetMultiplayerPeer _network = new();
    private uint _round = 1;
    private int _tempHouseIndex;

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

    /// <summary>
    /// 服务端/客户端是否已经初始化
    /// </summary>
    public bool Initialized { get; private set; }

    /// <summary>
    /// 当客户端时，表示是否已经连接到服务器
    /// </summary>
    public bool Connected { get; private set; }

    /// <summary>
    /// 当前回合数
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

    public string PlayerId { get; private set; } = Guid.NewGuid().ToString();
    public string ServerIp { get; private set; }
    public int ServerPort { get; private set; }

    /// <summary>
    /// 地图信息
    /// </summary>
    public Level LevelData { get; private set; } = new();

    /// <summary>
    /// 回合是否锁定（结算中）
    /// </summary>
    private bool _roundLocked;

    /// <summary>
    /// 玩家ID对应到玩家当前回合的操作
    /// </summary>
    private readonly Dictionary<string, PlayerOperation> _playerIdToPlayerOperation = new();

    /// <summary>
    /// 将 PeerId 对应到 玩家ID
    /// </summary>
    private readonly Dictionary<long, string> _peerIdToPlayerId = new();

    /// <summary>
    /// 将房子ID对应到房子信息
    /// </summary>
    private readonly Dictionary<string, House> _houseIdToHouseData = new();

    private readonly Dictionary<string, List<string>> _playerIdToOwnedHouses = new();

    private readonly Dictionary<string, Dictionary<string, bool>> _knowledge = new();

    private readonly Dictionary<string, PlayerData> _playerData = new();

    [Signal]
    public delegate void UserOperationEventHandler(long peerId, int operation, string playerId, string houseId);

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
            GD.Print($"[Client#{PlayerId}] Calling RPC HouseOperation");
            RpcId(1, nameof(HouseOperation), operationCode, houseId, "test secret");
            return;
        }

        var operation = (EnumPlayerOperation)operationCode;
        long peerId = Multiplayer.GetRemoteSenderId();
        var playerId = _peerIdToPlayerId[peerId];
        GD.Print($"[Server${peerId}] #{playerId}# Received HouseOperation RPC Call on {houseId}");

        #region Some Checks

        if (playerId == null)
        {
            OperationFailed(peerId, operation, "Not logged in");
            return;
        }

        if (!secret.Equals("test secret"))
        {
            OperationFailed(peerId, operation, "Unauthorized RPC Call");
            return;
        }

        if (_roundLocked)
        {
            OperationFailed(peerId, operation, "This round is locked");
            return;
        }

        if (operation != EnumPlayerOperation.StayAtHome)
        {
            if (!_houseIdToHouseData.ContainsKey(houseId))
            {
                OperationFailed(peerId, operation, "This house has no owner");
                return;
            }

            if (_houseIdToHouseData[houseId].Owner.Equals(playerId))
            {
                OperationFailed(peerId, operation, "It's your house");
                return;
            }
        }

        #endregion

        _playerIdToPlayerOperation[playerId] = new PlayerOperation
        {
            HouseId = houseId, Operation = operation
        };
        EmitSignal(SignalName.UserOperation, peerId, operationCode, playerId, houseId);
        RpcId(peerId, nameof(HouseOperationSuccessCallBack), operationCode, GetHouseCoord(houseId));
    }

    [Rpc]
    public void HouseOperationSuccessCallBack(int operation, Vector2I coord)
    {
        EmitSignal(SignalName.OperationSuccess, operation, coord);
    }


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

        var reconnecting = false;
        var peerId = Multiplayer.GetRemoteSenderId();
        if (_peerIdToPlayerId.ContainsKey(peerId))
        {
            GD.PrintErr($"[Server${peerId}] Already logged in");
            return;
        }

        if (_peerIdToPlayerId.ContainsValue(playerId))
        {
            reconnecting = true;
        }

        if (!reconnecting)
        {
            // New player joining
            var (coord, houseId) = LevelData.CoordsToHouseId.ElementAt(_tempHouseIndex);
            _tempHouseIndex++;
            // 添加一所房子给新玩家
            _playerIdToOwnedHouses[playerId] = new() { houseId };
            _houseIdToHouseData[houseId] = new House { Owner = playerId, HouseMoney = 100000 };
            GD.Print($"[Server${peerId}] Assigned house {houseId} at {Utils.IndexToCoord(coord)} to player {playerId}");

            _peerIdToPlayerId[peerId] = playerId;
            _playerData[playerId] = new PlayerData();
        }
        else
        {
            GD.Print($"[Server${peerId}] Reconnected");
        }

        ManualSyncInventory(peerId, playerId);
        ManualSyncLevel(peerId);
        EmitSignal(SignalName.UserLoginOrLogout, peerId, playerId, true);
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
            RpcId(1, nameof(Logout), PlayerId, playerSecret);
            Initialized = false;
            return;
        }

        var peerId = Multiplayer.GetRemoteSenderId();
        _peerIdToPlayerId.Remove(peerId);
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
    /// <param name="peerId">玩家的PeerId</param>
    /// <param name="text">消息内容</param>
    private void SendMessage(long peerId, string text)
    {
        GD.Print($"[Server] Sending Message to {peerId}, content: {text}");
        RpcId(peerId, nameof(ServerMessage), text);
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

    public void FinishGame()
    {
    }

    /// <summary>
    /// 结束回合，由 ServerScene 手动触发
    /// </summary>
    public void FinishRound()
    {
        if (!Multiplayer.IsServer()) return;

        GD.Print("Finish Round received");
        _roundLocked = true;
        if (InnerFinishRound())
        {
            BroadcastMessage($"[color=blue]Round {Round} Finished[/color]");
            Round++;
            Rpc(nameof(RoundFinished), Round);
        }

        _roundLocked = false;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void RoundFinished(uint newRound)
    {
        if (!Multiplayer.IsServer())
        {
            Round = newRound;
        }
    }

    private bool InnerFinishRound()
    {
        // 房子到进入的玩家的映射
        var houseToPlayerEntered = new Dictionary<string, List<string>>();

        // Traverse all peers calculating data
        // 结算遍历
        foreach (var (peerId, playerId) in _peerIdToPlayerId)
        {
            if (!_playerIdToPlayerOperation.ContainsKey(playerId))
            {
                GD.PrintErr("[Server] Not all player is ready");
                return false;
            }

            var operation = _playerIdToPlayerOperation[playerId];
            switch (operation.Operation)
            {
                case EnumPlayerOperation.Peek:
                {
                    var houseId = operation.HouseId;
                    _playerData[playerId].UseKey();
                    if (!_knowledge.ContainsKey(playerId))
                    {
                        _knowledge[playerId] = new();
                    }

                    _knowledge[playerId][houseId] = true;
                    var owner = _houseIdToHouseData[houseId].Owner;
                    var ownerOperation = _playerIdToPlayerOperation[owner].Operation;

                    SendMessage(peerId,
                        $"[color=#39c5bb]Peek result: Player {owner} is {(ownerOperation == EnumPlayerOperation.StayAtHome ? "at home" : "not at home")}[/color]");
                    break;
                }
                case EnumPlayerOperation.Steel:
                {
                    var houseId = operation.HouseId;
                    if (!_knowledge.ContainsKey(playerId) || !_knowledge[playerId].ContainsKey(houseId) ||
                        !_knowledge[playerId][houseId])
                    {
                        _playerData[playerId].UseKey();
                    }

                    var owner = _houseIdToHouseData[houseId].Owner;
                    var ownerOperation = _playerIdToPlayerOperation[owner].Operation;
                    if (ownerOperation == EnumPlayerOperation.StayAtHome)
                    {
                        // 被抓，损失一半的钱
                        var lostMoney = _playerData[playerId].Caught();
                        _playerData[owner].PlayerInventory.Moneys += lostMoney;

                        SendMessage(peerId, "[color=yellow]You were Caught, lost half of your money[/color]");
                    }
                    else
                    {
                        // 进入并且主人不在家，获得一半的钱
                        _playerData[playerId].PlayerInventory.Moneys += _houseIdToHouseData[houseId].HouseMoney / 2;
                        _houseIdToHouseData[houseId].HouseMoney /= 2;
                    }

                    break;
                }
            }
        }

        // Traverse all peers again sending information
        // 同步遍历
        foreach (var (peerId, playerId) in _peerIdToPlayerId)
        {
            ManualSyncInventory(peerId, playerId);
            ManualSyncLevel(peerId);
        }

        _playerIdToPlayerOperation.Clear();
        return true;
    }

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
        LevelData.CoordsToHouseId.Clear();
        LevelData.Round = 1;
        // Traverse all tiles and assign a random GUID to each house
        foreach (var y in Enumerable.Range(0, mapHeight))
        foreach (var x in Enumerable.Range(0, mapWidth))
        {
            // Assign a random GUID to a house as its id
            var coordX = StartCoord.X + x * HouseSize.X;
            var coordY = StartCoord.Y + y * HouseSize.Y;
            var houseId = Guid.NewGuid().ToString();
            GD.Print($"[Server] Created house {houseId} at {coordX}, {coordY}");
            LevelData.CoordsToHouseId[Utils.CoordToIndex(coordX, coordY)] = houseId;
        }

        Initialized = true;
        GD.Print("[Server] Started");
    }

    #endregion

    #region 同步物品信息 (Event, RPC)

    public delegate void ReceivedInventoryDataEventHandler(Inventory inventoryData);

    public event ReceivedInventoryDataEventHandler ReceivedInventoryData;

    /// <summary>
    /// [客户端 -> 服务端] 请求更新物品栏
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncInventoryRequest()
    {
        if (Multiplayer.IsServer())
        {
            GD.Print("Received RefreshInventory Request From Client");
            var peerId = Multiplayer.GetRemoteSenderId();
            var playerId = _peerIdToPlayerId[peerId];
            var inventoryData = _playerData[playerId].PlayerInventory;
            var inventoryBytes = new byte[inventoryData.CalculateSize()];
            using (var output = new CodedOutputStream(inventoryBytes))
            {
                inventoryData.WriteTo(output);
            }

            RpcId(peerId, nameof(SyncInventoryReturn), inventoryBytes);
        }
        else
        {
            RpcId(1, nameof(SyncInventoryRequest));
        }
    }

    /// <summary>
    /// [服务端 -> 客户端] 收到更新物品栏
    /// </summary>
    /// <param name="inventoryData">Bytes of <see cref="Inventory"/></param>
    [Rpc]
    public void SyncInventoryReturn(byte[] inventoryData)
    {
        if (Multiplayer.IsServer())
        {
            GD.PrintErr("SyncInventoryReturn can only be called on client side");
            return;
        }

        GD.Print($"[Client#{PlayerId}] Received InventoryData From Server");
        var inventory = Inventory.Parser.ParseFrom(inventoryData);
        ReceivedInventoryData?.Invoke(inventory);
    }

    /// <summary>
    /// 手动触发同步物品栏（服务端）
    /// </summary>
    /// <param name="peerId"></param>
    /// <param name="playerId"></param>
    private void ManualSyncInventory(long peerId, string playerId)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("[Client] ManualSyncInventory can only be called on server side");
            return;
        }

        GD.Print($"[Server${peerId}] Sending Inventory Data to {playerId}");
        var inventory = _playerData[playerId].PlayerInventory;
        var inventoryBytes = new byte[inventory.CalculateSize()];
        using (var stream = new CodedOutputStream(inventoryBytes))
        {
            inventory.WriteTo(stream);
        }

        RpcId(peerId, nameof(SyncInventoryReturn), inventoryBytes);
    }

    #endregion

    #region 同步地图信息 (Event, RPC)

    public delegate void ReceivedLevelDataEventHandler(Level levelData);

    public event ReceivedLevelDataEventHandler ReceivedLevelData;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    public void SyncLevelRequest()
    {
        if (Multiplayer.IsServer())
        {
            GD.Print("Received RefreshLevel Request From Client");
            var bytes = new byte[LevelData.CalculateSize()];
            using var output = new CodedOutputStream(bytes);
            LevelData.WriteTo(output);
            RpcId(Multiplayer.GetRemoteSenderId(), nameof(SyncLevelReturn), bytes);
        }
        else
        {
            RpcId(1, nameof(SyncLevelRequest));
        }
    }

    [Rpc]
    public void SyncLevelReturn(byte[] levelData)
    {
        if (Multiplayer.IsServer()) return;

        GD.Print($"[Client#{PlayerId}] Received LevelData From Server");
        var level = Level.Parser.ParseFrom(levelData);
        LevelData = level;
        ReceivedLevelData?.Invoke(level);
    }

    /// <summary>
    /// 手动触发同步物品栏（服务端）
    /// </summary>
    /// <param name="peerId"></param>
    private void ManualSyncLevel(long peerId)
    {
        if (!Multiplayer.IsServer())
        {
            GD.PrintErr("ManualSyncLevel can only be called on server side");
            return;
        }

        GD.Print($"[Server${peerId}] Sending Level data");
        var levelBytes = new byte[LevelData.CalculateSize()];
        using (var stream = new CodedOutputStream(levelBytes))
        {
            LevelData.WriteTo(stream);
        }

        RpcId(peerId, nameof(SyncLevelReturn), levelBytes);
    }

    #endregion

    /// <summary>
    /// 从坐标获取房屋 ID
    /// </summary>
    /// <param name="coord">房屋坐标</param>
    /// <returns>房屋的ID</returns>
    /// <exception cref="ArgumentOutOfRangeException">当坐标X或Y小于0时抛出</exception>
    public string GetHouseId(Vector2I coord)
    {
        var index = Utils.CoordToIndex(coord);
        GD.Print($"{coord} => {index}");
        return LevelData.CoordsToHouseId[index];
    }

    public Vector2I GetHouseCoord(string houseId)
    {
        var index = LevelData.CoordsToHouseId.FirstOrDefault(x => x.Value.Equals(houseId)).Key;
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