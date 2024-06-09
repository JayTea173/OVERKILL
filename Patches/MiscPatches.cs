using System;
using System.Collections.Generic;
using HarmonyLib;
using OVERKILL.UI.Upgrades;
using OVERKILL.Upgrades;
using UnityEngine;

namespace OVERKILL.Patches;

[HarmonyPatch(typeof(global::NewMovement), "GetHealth")]
public class PatchMaxHP
{
    public static double currMax = 100d;
    static void Prefix(NewMovement __instance, int health, bool silent, bool fromExplosion = false)
    {
        if (__instance.hp + health > 100)
        {
            __instance.hp = (int)Math.Min(__instance.hp + health, currMax + PlayerUpgradeStats.Instance.HPBonusFlat);
        }
    }
}

[HarmonyPatch(typeof(StyleHUD), nameof(StyleHUD.AddPoints))]
public class PatchPlayerUpdate
{
    public static void Prefix(
        StyleCalculator __instance,
        ref int points,
        string pointID,
        GameObject sourceWeapon = null,
        EnemyIdentifier eid = null,
        int count = -1,
        string prefix = "",
        string postfix = "")
    {
        if (points != 0)
        {
            
            
            double xpGained = points * PlayerUpgradeStats.Instance.StylePointsMultiplier * 1.5d;

            xpGained *= pointID switch
                        {
                            "explosionhit" => 0.5d,
                            "ultrakill.enraged" => 0.25d,
                            _ => 0.1d,
                        };
            //OK.Log($"Got points: {points} from \"{pointID}\", xp: {xpGained}");
            
            PlayerUpgradeStats.Instance.stylePoints += xpGained;
            
            var newLevel = StyleLevelupThresholds.GetLevelAtXP();

            if (newLevel != PlayerUpgradeStats.Instance.okLevel)
            {
                PlayerUpgradeStats.Instance.LevelUp(newLevel);
            }

        }
    }
}


[HarmonyPatch(typeof(EnemyIdentifier), "OnEnable")]
public class PatchEnemyMaxHP
{

        
    static void Postfix(EnemyIdentifier __instance)
    {
        EnemyMaxHP.Register(__instance, __instance.health);
    }
        
        
}

    
[HarmonyPatch(typeof(EnemyIdentifier), "OnDisable")]
public class EnemyMaxHPCleanup
{
        
    static void Postfix(EnemyIdentifier __instance)
    {
        EnemyMaxHP.Unregister(__instance);
            
    }
}

