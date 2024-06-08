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

[HarmonyPatch(typeof(StyleCalculator), "AddPoints")]
public class PatchPlayerUpdate
{
    public static void Prefix(
        StyleCalculator __instance,
        ref int points,
        string pointName,
        EnemyIdentifier eid,
        GameObject sourceWeapon = null)
    {
        if (points != 0)
        {
            points = (int)(points * PlayerUpgradeStats.Instance.StylePointsMultiplier);
            PlayerUpgradeStats.Instance.stylePoints += points;
            
            var newLevel = StyleLevelupThresholds.GetLevelAtXP();

            //OK.Log($"Points to add: {points}, now got {PlayerUpgradeStats.stylePoints}, oldLevel: {PlayerUpgradeStats.okLevel}, newLevel {newLevel}");

            if (newLevel != PlayerUpgradeStats.Instance.okLevel)
            {
                PlayerUpgradeStats.Instance.okLevel = newLevel;
                UpgradeScreen.Instance.Show();
            }

        }
    }
    /*
  private void AddPoints(
    int points,
    string pointName,
    EnemyIdentifier eid,
    GameObject sourceWeapon = null)
  {
    int num = Mathf.RoundToInt((float) points * this.airTime - (float) points);
    this.shud.AddPoints(points + num, pointName, sourceWeapon, eid);
  }
     */
    static void Postfix(NewMovement __instance)
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (UpgradeScreen.Instance.Shown)
                UpgradeScreen.Instance.Hide();
            else
                UpgradeScreen.Instance.Show();
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

