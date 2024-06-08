using System;
using UnityEngine;

namespace OVERKILL.Upgrades;

public enum Rarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Overkill,
    _COUNT
}

/// <summary>
/// when rnd is lower or equal to these values, return that rarity.
/// </summary>
public class RarityChances
{        
    /*rarity = r switc
    {
    >= 0.5f => Rarity.Common,
        >= 0.25f => Rarity.Uncommon,
        >= 0.1f => Rarity.Rare,
        >= 0.025f => Rarity.Epic,
    _ => Rarity.Overkill
};*/
    public const float Uncommon = 0.5f;
    public const float Rare = 0.25f;
    public const float Epic = 0.1f;
    public const float Overkill = 0.025f;

}

public static class RarityColor
{
    public static Color Get(Rarity r)
    {
        return r switch
               {
                   Rarity.Common => new Color32(0xdd, 0xdd, 0xdd, 0xff),
                   Rarity.Uncommon => new Color32(0x33, 0xff, 0x00, 0xff),
                   Rarity.Rare => new Color32(0x00, 0x66, 0xff, 0xff),
                   Rarity.Epic => new Color32(0xa3, 0x35, 0xee, 0xff),
                   Rarity.Overkill => new Color32(0xff, 0x88, 0x00, 0xff),
                   _ => new Color32(255, 255, 255, 0xff)
               };
    }

    public static string ToHex(float f01)
    {
        return BitConverter.ToString(new byte[] { (byte)Mathf.RoundToInt(f01 * 255f) });
    }

    public static string ToHex(Color c)
    {
        return ToHex(c.r) + ToHex(c.g) + ToHex(c.b) + ToHex(c.a);
    }

    public static string ColoredRTF(this string s, Color c)
    {
        return "<color=#" + ToHex(c) + ">" + s + "</color>";
    }

}
