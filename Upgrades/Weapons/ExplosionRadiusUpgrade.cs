using System.Collections.Generic;
using System.Linq;
using GameConsole.pcon;
using HarmonyLib;
using Newtonsoft.Json;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class ExplosionRadiusUpgrade : WeaponUpgrade, IRandomizable
{
    //public override double AppearChanceWeighting => 0.002f;
    public override int MaxLevel => 5;

    public override double AppearChanceWeighting => 0.8f * RarityChances.Overkill;

    [JsonIgnore]
    public float multiplier = .5f;
    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return false;
    }

    public override string Name => "ULTRABOOM";

    public override string Description =>
        $"Increase the radius of explosions while at ULTRAKILL rank by {multiplier + multiplier*level:0.%}";
    
    public override Rarity MaxRarity => Rarity.Overkill;

    public override void Apply()
    {
        ExplosionPatch.explosionRadiusMultiplier += multiplier + multiplier * level;
    }

    public override void Absolve()
    {
        ExplosionPatch.explosionRadiusMultiplier -= multiplier + multiplier * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        Rarity = Rarity.Overkill;
    }
}

[HarmonyPatch(typeof(Explosion), "Start")]
public class ExplosionPatch
{
    public static float explosionRadiusMultiplier = 1f;
    public static void Postfix(Explosion __instance)
    {
        __instance.speed *= explosionRadiusMultiplier;
        __instance.maxSize *= explosionRadiusMultiplier;

    }
}
