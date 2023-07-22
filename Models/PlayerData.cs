namespace NewDemo.Models;

public class PlayerData
{
    public const int InitialKeys = 20;
    public const int InitialMoney = 100000;

    public Inventory PlayerInventory;

    public EnumPlayerOperation PlayerOperation;

    public PlayerData()
    {
        PlayerInventory = new Inventory()
        {
            Keys = InitialKeys,
            Moneys = InitialMoney
        };
        PlayerOperation = EnumPlayerOperation.None;
    }

    public bool HasKey()
    {
        return PlayerInventory.Keys > 0;
    }

    public void UseKey()
    {
        PlayerInventory.Keys--;
    }

    public void AddMoney(uint money)
    {
        PlayerInventory.Moneys += money;
    }

    public uint Caught()
    {
        var lostMoney = PlayerInventory.Moneys / 2;
        PlayerInventory.Moneys -= lostMoney;
        return lostMoney;
    }
}