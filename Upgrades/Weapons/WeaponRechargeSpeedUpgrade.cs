using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class WeaponRechargeSpeedUpgrade : WeaponUpgrade, IRandomizable
{
    public override int MaxLevel => 10;

    public override double AppearChanceWeighting => 0.4f;

    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return true;
    }

    public override string Name => "Overcharge";
    public override string Description => $"Increases the recharge speed of your weapon's special firing modes by {bonusPct[Rarity]*level:0.%}.";
    
    public override Rarity MaxRarity => Rarity.Overkill;

    public DoubleRarityValue bonusPct;

    public override void Apply()
    {
        PlayerUpgradeStats.Instance.chargeSpeed += (float)bonusPct[Rarity] * level;
    }

    public override void Absolve()
    {
        PlayerUpgradeStats.Instance.chargeSpeed -= (float)bonusPct[Rarity] * level;
    }

    public void Randomize(int seed)
    {
        bonusPct = new DoubleRarityValue(0d);
        bonusPct[Rarity.Uncommon] = 0.02d;
        bonusPct[Rarity.Rare] = 0.04d;
        bonusPct[Rarity.Epic] = 0.06d;
        bonusPct[Rarity.Overkill] = 0.1d;

        var r = Random.value * RarityChances.Uncommon;

        Rarity = r switch
                 {
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
}

[HarmonyPatch(typeof(WeaponCharges), nameof(WeaponCharges.Charge))]
public class TestPatch
{
    public static void Prefix(WeaponCharges __instance, ref float amount)
    {
        amount *= PlayerUpgradeStats.Instance.chargeSpeed;
    }
}

