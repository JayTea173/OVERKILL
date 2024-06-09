using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace OVERKILL.Patches;

[HarmonyPatch(typeof(NewMovement))]
public class HardDamagePatches
{
    private static float hardDamageBefore, antiHpCooldownBefore;
    private static readonly FieldInfo antiHpCooldownField = typeof(NewMovement).GetField("antiHpCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo difficultyField = typeof(NewMovement).GetField("difficulty", BindingFlags.Instance | BindingFlags.NonPublic);

    
    [HarmonyPatch(nameof(NewMovement.GetHurt))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static void FixHardDamageAbove100MaxHP(NewMovement __instance, int damage,
        bool invincible,
        float scoreLossMultiplier = 1f,
        bool explosion = false,
        bool instablack = false,
        float hardDamageMultiplier = 0.35f,
        bool ignoreInvincibility = false)
    {
        hardDamageBefore = __instance.antiHp;
        antiHpCooldownBefore = (float)antiHpCooldownField.GetValue(__instance);
    }
    
    

    [HarmonyPatch(nameof(NewMovement.GetHurt))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void FixHardDamageAbove100MaxHPPost(
        NewMovement __instance,
        int damage,
        bool invincible,
        float scoreLossMultiplier = 1f,
        bool explosion = false,
        bool instablack = false,
        float hardDamageMultiplier = 0.35f,
        bool ignoreInvincibility = false)
    {
        var difficulty = (int)difficultyField.GetValue(__instance);
        
        if (invincible &&
            difficulty >= 2 &&
            (double)scoreLossMultiplier != 0.0 &&
            (!__instance.asscon.majorEnabled || !__instance.asscon.disableHardDamage) &&
            __instance.hp <= PatchMaxHP.currMax)
        {
            if ((double)hardDamageBefore + (double)damage * (double)hardDamageMultiplier < PatchMaxHP.currMax - 1)
            {
                __instance.antiHp = Mathf.Clamp(
                    hardDamageBefore + (float)(damage * hardDamageMultiplier),
                    0f,
                    (float)PatchMaxHP.currMax - 1);
                
                if (antiHpCooldownBefore == 0f)
                    ++antiHpCooldownBefore;
                if (difficulty >= 3)
                    ++antiHpCooldownBefore;

                antiHpCooldownField.SetValue(__instance, antiHpCooldownBefore);
            }

        }
        
        hardDamageBefore = 0f;
        antiHpCooldownBefore = 0f;

    }

    [HarmonyPatch(nameof(NewMovement.Parry))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.Last)]
    public static bool MakeParryBase100Health(NewMovement __instance, EnemyIdentifier eid = null, string customParryText = "")
    {
        MonoSingleton<TimeController>.Instance.ParryFlash();
        __instance.exploded = false;
        __instance.GetHealth(100, false);
        __instance.FullStamina();


        if ((bool)(UnityEngine.Object)eid && eid.blessed)
            return true;
        
        StyleHUD.Instance.AddPoints(100, customParryText != "" ? "<color=green>" + customParryText + "</color>" : "ultrakill.parry");

        return false;
    }
}
