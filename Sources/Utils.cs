using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Godot;

namespace NewDemo.Sources;

public static class Utils
{
    public const uint BasicRent = 1000;
    public const uint KRent = 1000;

    /// <summary>
    /// 计算当前回合的租金
    /// </summary>
    /// <param name="round">回合数</param>
    /// <returns></returns>
    public static uint CalculateRent(uint round)
    {
        return BasicRent + (round - 1) * KRent;
    }

    #region 坐标索引 => 其他
    /// <summary>
    /// 坐标索引 => 坐标
    /// <br/>
    /// </summary>
    /// <param name="index">坐标索引</param>
    /// <returns>坐标</returns>
    public static Vector2I HouseCoordIndexToCoord(ulong index)
    {
        var x = index >> 32;
        var y = index & 0xFFFFFFFF;
        return new Vector2I((int)x, (int)y);
    }

    /// <summary>
    /// 坐标索引 => 网格坐标
    /// <br/>
    /// </summary>
    /// <param name="coordIndex">索引</param>
    /// <returns>网格坐标</returns>
    public static Vector2I HouseCoordIndexToGridCoord(ulong coordIndex)
    {
        return HouseCoordToGridCoord(HouseCoordIndexToCoord(coordIndex));
    }
    #endregion 坐标索引 => 其他

    #region 坐标 => 其他
    /// <summary>
    /// 坐标 => 坐标索引
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public static ulong HouseCoordToCoordIndex(Vector2I coord)
    {
        return HouseCoordToCoordIndex(coord.X, coord.Y);
    }

    /// <summary>
    /// 坐标 => 坐标索引
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static ulong HouseCoordToCoordIndex(int x, int y)
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

    /// <summary>
    /// 坐标 => 网格坐标
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public static Vector2I HouseCoordToGridCoord(Vector2I coord)
    {
        return (coord - Configs.StartCoord) / Configs.GridSize;
    }

    /// <summary>
    /// 坐标 => 网格坐标
    /// </summary>
    /// <param name="coord"></param>
    /// <returns></returns>
    public static Vector2I HouseCoordToGridCoord(int x, int y)
    {
        return HouseCoordToGridCoord(new Vector2I(x, y));
    }
    #endregion

    #region 网格坐标 => 其他
    /// <summary>
    /// 网格坐标 => 坐标索引
    /// </summary>
    /// <param name="gridCoord"></param>
    /// <returns></returns>
    public static ulong HouseGridCoordToCoordIndex(Vector2I gridCoord)
    {
        return HouseCoordToCoordIndex(HouseGridCoordToCoord(gridCoord));
    }

    /// <summary>
    /// 网格坐标 => 坐标索引
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static ulong HouseGridCoordToCoordIndex(int x, int y)
    {
        return HouseGridCoordToCoordIndex(new Vector2I(x, y));
    }

    /// <summary>
    /// 网格坐标 => 坐标
    /// </summary>
    /// <param name="gridCoord"></param>
    /// <returns></returns>
    public static Vector2I HouseGridCoordToCoord(Vector2I gridCoord)
    {
        return gridCoord * Configs.GridSize + Configs.StartCoord;
    }

    /// <summary>
    /// 网格坐标 => 坐标
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <returns></returns>
    public static Vector2I HouseGridCoordToCoord(int x, int y)
    {
        return HouseGridCoordToCoord(new Vector2I(x, y));
    }
    #endregion

    public static List<TItem> GetOrCreate<TKey, TItem>(this Dictionary<TKey, List<TItem>> dictionary, TKey key)
    {
        if (dictionary.TryGetValue(key, out var list))
            return list;

        list = new List<TItem>();
        dictionary[key] = list;

        return list;
    }
}