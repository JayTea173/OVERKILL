using System;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades;

public class StaminaRegenUpgrade : LeveledUpgrade, IRandomizable
{
    public override int MaxLevel => 10;

    public override double AppearChanceWeighting => 0.8d * AppearChanceWeightingOptionMultiplier;

    public override string Name => "Stamina Regen Up";

    public override string Description => $"Increases your stamina regeneration by {multiplier[Rarity] * level:0.%} (of what you'd have playing brutal difficulty).";

    public override Rarity MaxRarity => Rarity.Epic;

    public DoubleRarityValue multiplier;

    public override void Apply()
    {
        PatchStaminaRegenSpeed.bonusMultiplier += multiplier[Rarity] * level;

        //PlayerUpgradeStats.HPBonusFlat += multiplier[Rarity] * level;
    }

    public override void Absolve()
    {
        PatchStaminaRegenSpeed.bonusMultiplier -= multiplier[Rarity] * level;
        
        //PlayerUpgradeStats.HPBonusFlat -= multiplier[Rarity] * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        multiplier = new DoubleRarityValue(0);
        multiplier[Rarity.Uncommon] = .03;
        multiplier[Rarity.Rare] = 0.04;
        multiplier[Rarity.Epic] = 0.05;
        multiplier[Rarity.Overkill] = 0;

        var r = Random.value * RarityChances.Uncommon;

        Rarity = r switch
                 {
                     >= RarityChances.Uncommon => Rarity.Common,
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Epic
                 };
    }
}

[HarmonyPatch(typeof(global::NewMovement), "Update")]
public class PatchStaminaRegenSpeed
{
    public static double bonusMultiplier = 0d;
    public static float maxStamina = 300f;
    
    private static bool wasDashingLastFrame;
    public static float lastDashTime;

    public static float TimeSinceLastDash
    {
        get
        {
            return Time.time - lastDashTime;
        }
    }

    public static void Prefix(global::NewMovement __instance)
    {
        wasDashingLastFrame = __instance.boost;
    }

    public static void Postfix(global::NewMovement __instance)
    {
        if (__instance.boost && !wasDashingLastFrame)
        {
            if (PatchStaminaRegenSpeed.TimeSinceLastDash > 1.25f)
                PatchDashInvincibility.currAudioIndex = 0;
            lastDashTime = Time.time;
            PatchDashInvincibility.dodgeTriggeredFlash = false;

        }
        
        if ((double) __instance.boostCharge != maxStamina && !__instance.sliding && !__instance.slowMode)
        {
            __instance.boostCharge = Mathf.MoveTowards(__instance.boostCharge, maxStamina, (float)(70f * Time.deltaTime * bonusMultiplier));
        }

        if (__instance.boostCharge > maxStamina)
            __instance.boostCharge = maxStamina;


    }
}

