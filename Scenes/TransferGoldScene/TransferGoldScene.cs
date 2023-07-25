using Godot;
using NewDemo.Scenes.Singletons;

namespace NewDemo.Scenes.TransferGoldScene;

public partial class TransferGoldScene : Control
{
    private CheckButton? _toInventoryCheckButton;
    private LineEdit? _goldTextInput;
    private Server? _server;

    public override void _Ready()
    {
        _toInventoryCheckButton = GetNode<CheckButton>("%ToInventoryCheckButton");
        _goldTextInput = GetNode<LineEdit>("%GoldTextInput");
        _server = GetNode<Server>("/root/Server");
    }

    public void _On_TransferButton_Pressed()
    {
        var count = _goldTextInput!.Text.ToInt();
        if (count < 0)
        {
            OS.Alert("Invalid gold count!");
        }
        else
        {
            var toInventory = _toInventoryCheckButton!.ButtonPressed;
            _server!.RpcTransferGolds((uint)count, toInventory);
            Visible = false;
        }
    }
}