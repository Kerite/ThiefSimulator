using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace NewDemo.Scripts;

public static class Utils
{
    public const uint BasicRent = 100;
    public const uint KRent = 100;

    /// <summary>
    /// 计算当前回合的租金
    /// </summary>
    /// <param name="round">回合数</param>
    /// <returns></returns>
    public static uint CalculateRent(uint round)
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

    public static List<TItem> GetOrCreate<TKey, TItem>(this Dictionary<TKey, List<TItem>> dictionary, TKey key)
    {
        if (dictionary.TryGetValue(key, out var list))
            return list;

        list = new List<TItem>();
        dictionary[key] = list;

        return list;
    }
}