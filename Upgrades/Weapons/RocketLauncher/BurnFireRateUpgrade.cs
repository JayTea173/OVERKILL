using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades.RocketLauncher;

public class BurnFireRateUpgrade : LeveledUpgrade, IRandomizable
{
    public override double AppearChanceWeighting => RarityChances.Rare * 1.2f;

    public override int MaxLevel => 2;

    public override string Name => "BURN BABY BURN";

    public override string Description => $"Increases the fire rate of your Rocket Launcher by {fireRateMultiplier[Rarity]*level:0.%} for every burning enemy within a certain distance, but decreases the overall firing rate of the SRS and Firestarter by 20%. The maximum rate of fire you can gain is 500%. When am ememy dies while being on fire, the effect persists 8 more seconds after death.";
    
    public override Rarity MaxRarity => Rarity.Overkill;
    
    public DoubleRarityValue fireRateMultiplier;

    public override void Apply()
    {
        PatchRocketLauncherFireRate.multiplier -= 0.2f;
        PatchStartBurning.rocketLauncherFireRatePerBurningTarget += fireRateMultiplier[Rarity] * level;
    }

    public override void Absolve()
    {
        PatchRocketLauncherFireRate.multiplier += 0.2f;
        PatchStartBurning.rocketLauncherFireRatePerBurningTarget -= fireRateMultiplier[Rarity] * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        fireRateMultiplier = new DoubleRarityValue(0);
        fireRateMultiplier[Rarity.Uncommon] = 0.0;
        fireRateMultiplier[Rarity.Rare] = 0.25;
        fireRateMultiplier[Rarity.Epic] = 0.35;
        fireRateMultiplier[Rarity.Overkill] = 0.45;
        var r = Random.value * RarityChances.Rare;

        Rarity = r switch
                 {
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
}


[HarmonyPatch(typeof(global::RocketLauncher), nameof(global::RocketLauncher.Shoot))]
public class PatchRocketLauncherFireRate
{
    public static double multiplier = 1d;
    public static void Postfix(global::RocketLauncher __instance)
    {
        var cdField = typeof(global::RocketLauncher).GetField(
            "cooldown",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (__instance.GetComponent <WeaponTypeComponent>().value != WeaponVariationType.FreezeframeRocketLauncher)
        {
            cdField.SetValue(__instance, (float)(__instance.rateOfFire / Math.Min(5d, multiplier)));
        } else
            cdField.SetValue(__instance, (float)(__instance.rateOfFire));

    }
}

public class BurningEnemyComponent : MonoBehaviour
{
    public EnemyIdentifier eid;
    private void OnDestroy()
    {
        if (PlayerUpgradeStats.Instance.upgrades.TryGetValue(new BurnFireRateUpgrade().GetHashCode(), out var existing))
        {
            NewMovement.Instance.StartCoroutine(PatchStartBurning.ReduceRocketLauncherFireRateDelayed(8f));
        }

        PatchStartBurning.RemoveBurning(eid);
    }
}


[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.StartBurning))]
public class PatchStartBurning
{
    public static double rocketLauncherFireRatePerBurningTarget = 0d;
    public static Dictionary <EnemyIdentifier, List <Flammable>> burningEnemies =
        new Dictionary <EnemyIdentifier, List <Flammable>>();
    public static void Postfix(EnemyIdentifier __instance, float heat)
    {

        burningEnemies.Add(__instance, __instance.burners);
        
        if (PlayerUpgradeStats.Instance.upgrades.TryGetValue(new BurnFireRateUpgrade().GetHashCode(), out var existing) && __instance.GetComponent<BurningEnemyComponent>() == null)
            PatchRocketLauncherFireRate.multiplier += rocketLauncherFireRatePerBurningTarget;

        __instance.gameObject.GetOrAddComponent <BurningEnemyComponent>().eid = __instance;
    }

    public static void RemoveBurning(EnemyIdentifier eid)
    {
        if (eid == null)
        {
            if (burningEnemies.Count > 0)
            {
                OK.Log($"Failed to remove burning enemy (is null), still burning: {burningEnemies.Count}");
            }
            return;
        }

        if (eid.gameObject.TryGetComponent(out BurningEnemyComponent b) && b != null)
        {
            UnityEngine.Object.Destroy(b);
        }
       
    }

    public static IEnumerator ReduceRocketLauncherFireRateDelayed(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        PatchRocketLauncherFireRate.multiplier -= rocketLauncherFireRatePerBurningTarget;
    }
}

[HarmonyPatch(typeof(Flammable), nameof(Flammable.Pulse))]
public class PatchStopBurning
{
    public static void Postfix(Flammable __instance)
    {
        if (!__instance.burning && __instance.heat <= 0f)
        {
            var eid = __instance.GetComponentInParent <EnemyIdentifier>();

            if (eid != null)
                PatchStartBurning.RemoveBurning(eid);

        }
    }
}

[HarmonyPatch(typeof(EnemyIdentifier), "OnDisable")]
public class PatchStopBurningOnDisable
{
    public static void Postfix(EnemyIdentifier __instance)
    {
        PatchStartBurning.RemoveBurning(__instance);


    }
}


[HarmonyPatch(typeof(EnemyIdentifier), nameof(EnemyIdentifier.Death), typeof(bool))]
public class PatchStopBurningOnDeath
{
    public static void Postfix(EnemyIdentifier __instance, bool fromExplosion)
    {
        PatchStartBurning.RemoveBurning(__instance);
        
    }
}

