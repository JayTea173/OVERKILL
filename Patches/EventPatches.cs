using HarmonyLib;
using UnityEngine;

namespace OVERKILL.Patches;

[HarmonyPatch(typeof(EnemyIdentifier), "Death", typeof(bool))]
public class PatchEnemyDeath
{
    static void Prefix(EnemyIdentifier __instance, bool fromExplosion)
    {
        Events.OnEnemyDeath.Pre?.Invoke(__instance, fromExplosion);
        Events.OnEnemyDeath.LatePre?.Invoke(__instance, fromExplosion);
    }
    
    static void Postfix(EnemyIdentifier __instance, bool fromExplosion)
    {
        Events.OnEnemyDeath.Post?.Invoke(__instance, fromExplosion);
    }
}

[HarmonyPatch(typeof(EnemyIdentifier), "DeliverDamage")]
public class PatchEnemyDeliverDamage
{
    private static EventDelegates.DeliverDamageEventData evntData;
    
    static void Prefix(EnemyIdentifier __instance,  
        GameObject target,
        Vector3 force,
        Vector3 hitPoint,
        ref float multiplier,
        bool tryForExplode,
        float critMultiplier = 0.0f,
        GameObject sourceWeapon = null,
        bool ignoreTotalDamageTakenMultiplier = false,
        bool fromExplosion = false)
    {
        evntData = new EventDelegates.DeliverDamageEventData();
        
        if (target != null)
        {
            var targetName = target.name;

            if (targetName.Contains("Head") || targetName.Contains("spine"))
            {
                evntData.isHeadshot = true;
            }
        }
        
        Events.OnDealDamage.Pre?.Invoke(evntData, __instance, target, force, hitPoint, ref multiplier, tryForExplode, critMultiplier, sourceWeapon, ignoreTotalDamageTakenMultiplier, fromExplosion);
        Events.OnDealDamage.LatePre?.Invoke(evntData, __instance, target, force, hitPoint, ref multiplier, tryForExplode, critMultiplier, sourceWeapon, ignoreTotalDamageTakenMultiplier, fromExplosion);

    }
    
    static void Postfix(EnemyIdentifier __instance,  
        GameObject target,
        Vector3 force,
        Vector3 hitPoint,
        ref float multiplier,
        bool tryForExplode,
        float critMultiplier = 0.0f,
        GameObject sourceWeapon = null,
        bool ignoreTotalDamageTakenMultiplier = false,
        bool fromExplosion = false)
    {
        Events.OnDealDamage.Post?.Invoke(evntData, __instance, target, force, hitPoint, ref multiplier, tryForExplode, critMultiplier, sourceWeapon, ignoreTotalDamageTakenMultiplier, fromExplosion);
    }
}