using System;
using System.Reflection;
using GameConsole.pcon;
using HarmonyLib;
using UnityEngine;
using Object = System.Object;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades;

public class ParryProjectileHomingUpgrade : LeveledUpgrade
{
    public override int MaxLevel => 3;

    public override double AppearChanceWeighting => RarityChances.Epic * 0.33f * AppearChanceWeightingOptionMultiplier;

    public override string Name => "PARRY THIS CASUL";

    public override string Description => $"Parried projectiles home to the closest target. Homing projectiles retarget if their current target dies. If no target is found, they turn hostile towards you again. When parrying a projectile, create {2*level} clones of it.";

    public override Rarity Rarity => Rarity.Epic;
    public override Rarity MaxRarity => Rarity.Epic;

    public override void Apply()
    {
        ParryPatcher.effectActive = true;
        ParryPatcher.numCopies += 2 * level;
    }

    public override void Absolve()
    {
        ParryPatcher.effectActive = false;
        ParryPatcher.numCopies -= 2 * level;
    }
}

[HarmonyPatch(typeof(Punch), "ParryProjectile")]
public class ParryPatcher
{
    public static bool effectActive;
    public static int numCopies;
    
    public static void Postfix(Punch __instance, Projectile proj)
    {
        if (!effectActive)
            return;
        
        if (proj.TryGetComponent <ProjectileSourceComponent>(out var src))
        {
            proj.target = new EnemyTarget(src.enemyId);
        } else 
            OK.Log($"Unknown projectile for parry handler: {proj.gameObject.name}");
        
        proj.homingType = HomingType.Gradual;

        for (int i = 0; i < numCopies; i++)
        {
            var cpy = GameObject.Instantiate(proj.gameObject);
            cpy.transform.localEulerAngles += new Vector3(Random.Range(-2f, 5f), Random.Range(-45f, 45), 0f);

            if (cpy.TryGetComponent <ProjectileSourceComponent>(out src))
                cpy.GetComponent<Projectile>().target = proj.target;
        }
        proj.transform.localEulerAngles += new Vector3(Random.Range(-2f, 5f), Random.Range(-45f, 45), 0f);
        
    }
}

public class ProjectileSourceComponent : MonoBehaviour
{
    public EnemyIdentifier enemyId;

    private void Update()
    {
        if (enemyId == null)
            return;

        if (this == null)
            return;

        if (enemyId.dead)
        {
            var hits = Physics.OverlapSphere(transform.position, 500f, 1 << enemyId.gameObject.layer, QueryTriggerInteraction.Ignore);

            if (hits == null)
                return;
            
            if (hits.Length <= 0)
            {
                if (!gameObject.TryGetComponent <Projectile>(out Projectile proj)) 
                    return;
                
                proj.homingType = HomingType.Instant;
                proj.friendly = false;
                proj.target = EnemyTarget.TrackPlayer();
            }
            else
            {
                foreach (var hit in hits)
                {
                    if (hit.attachedRigidbody == null)
                        continue;
                    
                    if (hit.attachedRigidbody.TryGetComponent <EnemyIdentifier>(out var newEnemyId))
                    {
                        if (!newEnemyId.dead)
                        {
                            enemyId = newEnemyId;
                        }
                    }
                }
            }
            
        }
    }
}

[HarmonyPatch(typeof(Projectile), "Start")]
public class ProjectileStartPatcher
{
    public static bool hasEffect;
    
    public static void Postfix(Projectile __instance)
    {
        if (hasEffect)
            __instance.canHitCoin = true;
    }
}

[HarmonyPatch(typeof(Coin), nameof(Coin.DelayedEnemyReflect))]
public class CoinReflectPatcher
{
    public static void Prefix(Coin __instance)
    {
        var hits = Physics.OverlapSphere(__instance.transform.position, 500f, 1 << 12, QueryTriggerInteraction.Ignore);

        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody == null || hit.gameObject == null)
                    continue;
                
                if (hit.attachedRigidbody.TryGetComponent <EnemyIdentifier>(out var newEnemyId))
                {
                    if (!newEnemyId.dead)
                    {
                        __instance.customTarget = new EnemyTarget(newEnemyId);
                        StyleHUD.Instance.AddPoints(30, "ultrakill.ricoshot", prefix: "OVER");
                    }
                }
            }
        }


    }
    //DelayedEnemyReflect
}

[HarmonyPatch(typeof(ZombieProjectiles), "ShootProjectile")]
public class ZombieProjectilesShootPatcher
{
    public static void Postfix(ZombieProjectiles __instance, int skipOnEasy)
    {
        __instance.projectile.GetOrAddComponent<ProjectileSourceComponent>().enemyId = __instance.GetComponent<EnemyIdentifier>();


    }
}

[HarmonyPatch(typeof(ZombieProjectiles), nameof(ZombieProjectiles.ThrowProjectile))]
public class ZombieProjectilesThrowPatcher
{
    public static void Postfix(ZombieProjectiles __instance)
    {
        __instance.projectile.GetOrAddComponent<ProjectileSourceComponent>().enemyId = __instance.GetComponent<EnemyIdentifier>();


    }
}
