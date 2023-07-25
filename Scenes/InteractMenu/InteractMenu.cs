using Godot;
using NewDemo.Scenes.Singletons;

namespace NewDemo.Scenes.InteractMenu;

public partial class InteractMenu : Node2D
{
    private Server? _server;

    [Signal]
    public delegate void MenuInteractedEventHandler(EnumMenu menu);

    public override void _Ready()
    {
        _server = GetNode<Server>("/root/Server");
        GetNode<Button>("Control/HBoxContainer/Steel").Pressed += () => { EmitMenuInteracted(EnumMenu.Steel); };
        GetNode<Button>("Control/HBoxContainer/Peek").Pressed += () => { EmitMenuInteracted(EnumMenu.Peek); };
        _server.OperationSuccess += (_, _) => _On_OperationSuccess();
    }

    public enum EnumMenu
    {
        Steel,
        Peek
    }

    private void EmitMenuInteracted(EnumMenu menu)
    {
        EmitSignal(SignalName.MenuInteracted, (int)menu);
    }

    public void _On_OperationSuccess()
    {
        Visible = false;
    }
}