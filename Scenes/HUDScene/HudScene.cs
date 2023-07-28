using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using NewDemo.Models;
using NewDemo.Scenes.Singletons;
using NewDemo.Sources;

namespace NewDemo.Scenes.HUDScene;

#pragma warning disable IDE1006 // Naming Styles
public partial class HudScene : Control
{
    private Server? _server;

    private Label? _keys;
    private Label? _money;
    private Label? _round;
    private Label? _operation;
    private Label? _peekChance;
    private PanelContainer? _messagePanel;
    private RichTextLabel? _messageHistory;
    private Button? _showMessageButton;
    private CheckButton? _lockMessageHistoryButton;

    private CancellationTokenSource? _cts;
    private bool _locked = true;

    public override void _Ready()
    {
        _server = GetNode<Server>("/root/Server");

        _keys = GetNode<Label>("TopContainer/TopPanel/HBoxContainer/Keys");
        _money = GetNode<Label>("TopContainer/TopPanel/HBoxContainer/Moneys");
        _round = GetNode<Label>("TopContainer/TopPanel/HBoxContainer/Round");
        _peekChance = GetNode<Label>("TopContainer/TopPanel/HBoxContainer/PeekChance");
        _showMessageButton = GetNode<Button>("MessageContainer/ShowMessageButton");
        _operation = GetNode<Label>("TopContainer/TopPanel/HBoxContainer/Operation");
        _messagePanel = GetNode<PanelContainer>("MessageContainer/MessagePanel");
        _messageHistory = GetNode<RichTextLabel>("%MessageHistory");
        _lockMessageHistoryButton = GetNode<CheckButton>("%LockBox");

        _server.ReceivedInventoryData += data =>
        {
            _keys.Text = data.Keys.ToString();
            _money.Text = data.Moneys.ToString();
            _peekChance.Text = data.RemainedPeek.ToString();
        };
        _server.ReceivedLevelData += data => { _round.Text = data.Round.ToString(); };
        _server.RoundChanged += _On_Server_RoundChanged;
        _server.OperationSuccess += _On_Server_OperationSuccess;
        _server.ServerMessageReceived += _On_Server_MessageReceived;

        StartHideMessage();
    }

    public void _On_Server_OperationSuccess(int operation, Vector2I _)
    {
        _operation!.Text = Enum.GetName((EnumPlayerOperation)operation);
    }

    public void _On_Server_MessageReceived(string message)
    {
        _messageHistory!.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
        StartHideMessage();
    }

    public void _On_Server_RoundChanged(uint round)
    {
        _round!.Text = round.ToString();
        _operation!.Text = "";
    }

    public void SyncInventory()
    {
        _server!.RpcSyncInventoryDataRequest();
    }

    private void StartHideMessage()
    {
        async Task HideMessageAsync(CancellationToken token)
        {
            await Task.Delay(Configs.MessageDuration, token);
            _messagePanel!.Visible = false;
            _showMessageButton!.Visible = true;
        }

        _messagePanel!.Visible = true;
        _showMessageButton!.Visible = false;
        _cts?.Cancel();

        if (_locked) return;
        _cts = new CancellationTokenSource();
        _ = HideMessageAsync(_cts.Token);
    }


    public void _On_StayAtHomeButton_Pressed()
    {
        _server!.HouseOperation((int)EnumPlayerOperation.StayAtHome);
    }

    public void _On_TransferMoneyButton_Pressed()
    {
        GetNode<TransferGoldScene.TransferGoldScene>("TransferGoldScene").Visible = true;
    }

    public void _On_MessageHistoryContainer_MouseEntered()
    {
        _cts?.Cancel();
    }

    public void _On_MessageHistoryContainer_MouseExited()
    {
        StartHideMessage();
    }

    public void _On_LockMessageHistoryButton_Toggled(bool buttonPressed)
    {
        _locked = buttonPressed;
        if (_locked)
        {
            _cts?.Cancel();
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles