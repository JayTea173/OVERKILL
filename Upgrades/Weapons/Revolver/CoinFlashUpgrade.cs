using System.Reflection;
using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class CoinFlashUpgrade : WeaponUpgrade, IRandomizable
{
    //public override double AppearChanceWeighting => 0.002f;
    public override int MaxLevel => 2;

    public override double AppearChanceWeighting => RarityChances.Rare * 0.8f * AppearChanceWeightingOptionMultiplier;

    public DoubleRarityValue multiplier;

    public override string Name => "BLING BLING";

    public override string Description =>
        $"Increase the damage bonus gained on coins during their initial flash by {multiplier[Rarity]*level:0.%}, but decreases the overall damage of your Marksman Revolver by 20%. Also increases the number of split shots by {level} when using the default revolver.";
    
    public override Rarity MaxRarity => Rarity.Overkill;

    public override void Apply()
    {
        CoinFlashDamageStartPatch.damageMultiplier += multiplier[Rarity] * level;
        CoinFlashDamageStartPatch.extraRicochets += level;

        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponVariationTypeMultiplier[WeaponVariationType.MarskmanRevolver] -=
            0.2f;
        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponVariationTypeMultiplier[WeaponVariationType.MarskmanSlabRevolver] -=
            0.2f;
    }

    public override void Absolve()
    {
        CoinFlashDamageStartPatch.damageMultiplier -= multiplier[Rarity] * level;
        CoinFlashDamageStartPatch.extraRicochets -= level;
        
        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponVariationTypeMultiplier[WeaponVariationType.MarskmanRevolver] +=
            0.2f;
        PlayerUpgradeStats.Instance.weaponDamageMultplier.weaponVariationTypeMultiplier[WeaponVariationType.MarskmanSlabRevolver] +=
            0.2f;
        
        
    }

    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return wtype.value == WeaponVariationType.MarskmanRevolver || wtype.value == WeaponVariationType.MarskmanSlabRevolver;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        
        multiplier = new DoubleRarityValue(0.3d);
        multiplier[Rarity.Uncommon] = 0.4d;
        multiplier[Rarity.Rare] = 0.5d;
        multiplier[Rarity.Epic] = 0.75d;
        multiplier[Rarity.Overkill] = 1.0d;

        var r = Random.value * RarityChances.Rare;

        Rarity = r switch
                 {
                     >= RarityChances.Uncommon => Rarity.Common,
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill / 2f => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
}

[HarmonyPatch(typeof(Coin), "TripleTime")]
public class CoinFlashDamageStartPatch
{
    public static double damageMultiplier = 1d;
    public static int extraRicochets = 0;
    public static void Postfix(Coin __instance)
    {
        __instance.power *= (float)damageMultiplier;
        __instance.ricochets += extraRicochets;
    }
}

[HarmonyPatch(typeof(Coin), "TripleTimeEnd")]
public class CoinFlashDamageEndPatch
{
    public static void Postfix(Coin __instance)
    {
        __instance.power /= (float)CoinFlashDamageStartPatch.damageMultiplier;
        __instance.ricochets -= CoinFlashDamageStartPatch.extraRicochets;
    }
}