using System;

namespace NewDemo.Models;

[Serializable]
public class PlayerOperation
{
    public string HouseId;
    public EnumPlayerOperation Operation;
};

public enum EnumPlayerOperation
{
    None,
    Peek,
    Steel,
    StayAtHome
}