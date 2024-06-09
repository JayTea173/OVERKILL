using System.Collections.Specialized;
using System.Reflection;
using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class CoinDistanceDamageUpgrade : WeaponUpgrade, IRandomizable
{
    //public override double AppearChanceWeighting => 0.002f;
    public override int MaxLevel => 5;

    public override double AppearChanceWeighting => RarityChances.Rare * 0.9f * AppearChanceWeightingOptionMultiplier;

    public DoubleRarityValue multiplier;

    public override string Name => "NUKE FROM ORBIT";

    public override string Description =>
        $"Your coins grant additional bonus damage based on their distance to you, up to {multiplier[Rarity] * level:0.%}. The damage bonus starts at 30m away and ends at 300m away.";
    
    public override Rarity MaxRarity => Rarity.Overkill;

    public override void Apply()
    {
        CoinRangeBonusDamagePatch.damageMultiplier += multiplier[Rarity] * level;

    }

    public override void Absolve()
    {
        CoinRangeBonusDamagePatch.damageMultiplier -= multiplier[Rarity] * level;
    }

    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return wtype.value == WeaponVariationType.MarskmanRevolver || wtype.value == WeaponVariationType.MarskmanSlabRevolver;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        
        multiplier = new DoubleRarityValue(0.05d);
        multiplier[Rarity.Uncommon] = 0.2d;
        multiplier[Rarity.Rare] = 0.4d;
        multiplier[Rarity.Epic] = 0.6d;
        multiplier[Rarity.Overkill] = 0.8d;

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

[HarmonyPatch(typeof(Coin), "ReflectRevolver")]
public class CoinRangeBonusDamagePatch
{
    public static double damageMultiplier = 1d;
    
    public static void Prefix(Coin __instance)
    {
        if (damageMultiplier == 1d)
            return;
        
        var dist = Vector3.Distance(__instance.transform.position, NewMovement.Instance.transform.position);


        if (dist >= 30f)
        {
            if (dist >= 300f)
                dist = 300f;

            var dmgDistMultiplierPct = Mathf.Clamp01((dist - 30f) / 270f);
            __instance.power *= 1f + dmgDistMultiplierPct * (float)damageMultiplier;

            if (dmgDistMultiplierPct >= 1f)
            {
                PlayBoomSound(NewMovement.Instance.transform.position, 0f, 3f);
                __instance.ricochets += 2;
                StyleHUD.Instance.AddPoints(30, "far.ricochet", __instance.sourceWeapon);
                CameraController.Instance.CameraShake(2f);
                
            }
            
        }
    }

    private static async void PlayBoomSound(Vector3 position, float spatialBlend = 1f, float volume = 1f)
    {
        var clip = await CustomSound.LoadAsync("boom.wav");
        clip.PlayClipAtPoint(null, position, 128, spatialBlend, minimumDistance: 0f, maximumDistance: 1000f, volume: volume);
    }
}