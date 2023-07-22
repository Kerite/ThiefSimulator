using System;
using Godot;

namespace NewDemo.Scripts;

public static class Utils
{
    public const int BasicRent = 100;
    public const int KRent = 100;

    /// <summary>
    /// 计算当前回合的租金
    /// </summary>
    /// <param name="round">回合数</param>
    /// <returns></returns>
    public static int CalculateRent(int round)
    {
        return BasicRent + (round - 1) * KRent;
    }

    public static ulong CoordToIndex(Vector2I coord)
    {
        return CoordToIndex(coord.X, coord.Y);
    }

    public static ulong CoordToIndex(int x, int y)
    {
        if (x < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(x));
        }

        if (y < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(y));
        }

        var uX = (ulong)x;
        var uY = (ulong)y;
        return (uX << 32) + uY;
    }

    public static Vector2I IndexToCoord(ulong index)
    {
        var x = index >> 32;
        var y = index & 0xFFFFFFFF;
        return new Vector2I((int)x, (int)y);
    }
}