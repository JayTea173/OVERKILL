using System;
using System.Threading.Tasks;
using GameConsole.pcon;
using HarmonyLib;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades;

public class InvincibilityFramesUpgrade : LeveledUpgrade, IRandomizable
{
    public override double AppearChanceWeighting => RarityChances.Overkill * 1.5f * AppearChanceWeightingOptionMultiplier;

    public override int MaxLevel => 3;

    public override string Name => "I'M INVINCIBLE!";

    public override string Description =>
        $"You gain full invincibilty for 0.35 seconds when dashing, but you lose 1 stamina bar permanently upon picking this upgrade. Dodging in this way reduces hard damage by 25." +
        "\n\nLevel 2: Dodging an attack in this way refills 1 bar of stamina." +
        $"\nLevel 3: Dodging an attack in this way recharges your Railcannon by 20%.";

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
    public static int currAudioIndex = 0;

    private static float lastFlashDodgeTime = 0f;

    public const float invincibilityTime = 0.5f;

    public static bool dodgeTriggeredFlash = false;

    [HarmonyPriority(Priority.First)]
    public static bool Prefix(global::NewMovement __instance, 
        int damage,
        bool invincible,
        float scoreLossMultiplier = 1f,
        bool explosion = false,
        bool instablack = false,
        float hardDamageMultiplier = 0.35f,
        bool ignoreInvincibility = false)
    {
        if (PatchStaminaRegenSpeed.TimeSinceLastDash < invincibilityTime && effectEnabled)
        {
            if (staminaRefill > 0f)
                NewMovement.Instance.boostCharge += staminaRefill;

            WeaponCharges.Instance.raicharge += railRefill * 5f;

            if (NewMovement.Instance.antiHp > 0)
            {
                NewMovement.Instance.antiHp -= 25;
                if (NewMovement.Instance.antiHp < 0f)
                    NewMovement.Instance.antiHp -= 0f;
            }

            if (!dodgeTriggeredFlash)
            {
                dodgeTriggeredFlash = true;
                lastFlashDodgeTime = Time.time - PatchStaminaRegenSpeed.TimeSinceLastDash;
                MonoSingleton <TimeController>.Instance.ParryFlash();
                PlayDodgeSoundAsync(__instance);
                
            }

            return false;
        }

        return true;
    }
    
    
    [HarmonyPatch(typeof(NewMovement), "Dodge")]
    [HarmonyPostfix]
    public static void OverrideVanillaInvincibility(NewMovement __instance)
    {
        if (effectEnabled && !__instance.sliding)
        {
            __instance.gameObject.layer = 2; //15 is invincibility layer
        }
    }

    private static async Task PlayDodgeSoundAsync(NewMovement __instance)
    {
        var clip = await CustomSound.LoadAsync($"doot{((currAudioIndex % 8) + 1)}.wav");

        currAudioIndex++;

        if (currAudioIndex > 1)
        {
            __instance.boostCharge = Mathf.Clamp(
                __instance.boostCharge + Mathf.Clamp(30f + (currAudioIndex - 1) * 20f, 0f, 90f),
                0f,
                PatchStaminaRegenSpeed.maxStamina);
            StyleHUD.Instance.AddPoints(60 + Math.Min(7, currAudioIndex) * 20, $"panic roll #{currAudioIndex}", null);
        }
        else
            StyleHUD.Instance.AddPoints(80, "panic roll?", null);
        

        clip.PlayClipAtPoint(
            null,
            __instance.transform.position,
            128, 
            0f,
            minimumDistance: 0f,
            maximumDistance: 1000f,
            volume: 1f);
    }

    
}
