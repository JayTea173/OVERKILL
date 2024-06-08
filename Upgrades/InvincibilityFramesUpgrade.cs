using System;
using GameConsole.pcon;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades;

public class InvincibilityFramesUpgrade : LeveledUpgrade, IRandomizable
{
    public override double AppearChanceWeighting => RarityChances.Overkill * 1.5f;

    public override int MaxLevel => 3;

    public override string Name => "I'M INVINCIBLE!";

    public override string Description => $"You gain full invincibilty for 0.35 seconds when dashing, but you lose 1 stamina bar." +
                                          (level > 1 ? $"\n\nLevel 2: Dodging an attack in this way refills 1 bar of stamina." : string.Empty) +
                                          (level > 2 ? $"\n\nLevel 3: Dodging an attack in this way recharges your Railcannon by 20%." : string.Empty);

    public override Rarity MaxRarity => Rarity.Overkill;


    public override void Apply()
    {
        PatchDashInvincibility.effectEnabled = true;
        PatchStaminaRegenSpeed.maxStamina -= 100f;

        if (level > 1)
            PatchDashInvincibility.staminaRefill = 100f;

        if (level > 2)
            PatchDashInvincibility.railRefill = 0.2f;
    }

    public override void Absolve()
    {
        PatchDashInvincibility.effectEnabled = false;
        PatchStaminaRegenSpeed.maxStamina += 100f;
        
        if (level > 1)
            PatchDashInvincibility.staminaRefill = 0f;
        
        if (level > 2)
            PatchDashInvincibility.railRefill = 0f;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        Rarity = Rarity.Overkill;
    }
}

[HarmonyPatch(typeof(global::NewMovement), nameof(NewMovement.GetHurt))]
public class PatchDashInvincibility
{
    public static bool effectEnabled = false;
    public static float staminaRefill = 0f;
    public static float railRefill = 0f;

    private static float lastFlashDodgeTime = 0f;

    public static bool Prefix(global::NewMovement __instance, 
        int damage,
        bool invincible,
        float scoreLossMultiplier = 1f,
        bool explosion = false,
        bool instablack = false,
        float hardDamageMultiplier = 0.35f,
        bool ignoreInvincibility = false)
    {
        if (PatchStaminaRegenSpeed.TimeSinceLastDash < 0.4f && effectEnabled)
        {
            StyleHUD.Instance.AddPoints(80, "panic roll?", null);

            if (staminaRefill > 0f)
                NewMovement.Instance.boostCharge += staminaRefill;

            WeaponCharges.Instance.raicharge += 0.2f * 5f;

            if (Time.time - lastFlashDodgeTime < 0.4f - PatchStaminaRegenSpeed.TimeSinceLastDash)
            {
                lastFlashDodgeTime = Time.time;
                MonoSingleton <TimeController>.Instance.ParryFlash();
            }

            return false;
        }

        return true;
    }
    
}
