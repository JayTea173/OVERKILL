using System;
using System.Linq;
using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades.RocketLauncher;


public class RocketBarrageUpgrade : LeveledUpgrade, IRandomizable
{
    public override double AppearChanceWeighting => RarityChances.Overkill * 1.3f * AppearChanceWeightingOptionMultiplier;

    public override int MaxLevel => 3;

    public override string Name => "ROCKET BARRAGE";

    public override string Description => $"When firing your rocket launcher, fire 3 additional missiles. Your missiles automatically home towards the closest target. They prioritize airborne targets. Reduce overall fire rate by 100%. Your Timefreeze variant is unchanged, so you can still rocketride like you're used to :)";

    public override Rarity MaxRarity => Rarity.Overkill;
    

    public override void Apply()
    {
        PatchRocketLauncherFireRate.multiplier *= .5f;
        PatchMultiRocket.barrageSize += 3 * level;
        PatchMultiRocket.extraRockets = PatchMultiRocket.barrageSize;
    }

    public override void Absolve()
    {
        PatchRocketLauncherFireRate.multiplier *= 2f;
        PatchMultiRocket.barrageSize -= 3 * level;
        PatchMultiRocket.extraRockets = PatchMultiRocket.barrageSize;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);


        Rarity = Rarity.Overkill;
    }
}


[HarmonyPatch(typeof(global::RocketLauncher), nameof(global::RocketLauncher.Shoot))]
public class PatchMultiRocket
{
    public static int barrageSize = 1;
    public static int extraRockets = barrageSize-1;
    public static void Postfix(global::RocketLauncher __instance)
    {
        if (barrageSize <= 1)
            return;

        if (__instance != null &&
            __instance.TryGetComponent(out WeaponTypeComponent wt) &&
            wt.value != WeaponVariationType.FreezeframeRocketLauncher)
        {

            if (extraRockets > 0)
            {
                extraRockets -= 1;
                __instance.Invoke("Shoot", (float)(0.05d / Math.Min(3d, PatchRocketLauncherFireRate.multiplier)));
            }
            else
            {
                extraRockets = barrageSize - 1;
            }
        }

    }
}

public class RocketHomingComponent : MonoBehaviour
{
    public EnemyIdentifier enemyId;
    public Grenade grenade;

    private void Update()
    {
        if (grenade == null)
            return;

        if (grenade.playerRiding)
            return;
        
        if (enemyId == null || enemyId.dead)
        {
            enemyId = null;
            var hits = Physics.OverlapSphere(transform.position, 500f, 1 << 12, QueryTriggerInteraction.Ignore);

            foreach (var hit in hits.Where(h => h.attachedRigidbody != null && h.attachedRigidbody.TryGetComponent(out EnemyIdentifier e) && !e.dead && (e.gce == null || !e.gce.onGround)).OrderBy(h => Vector3.Distance(h.transform.position, transform.position)))
            {
                if (hit.attachedRigidbody.TryGetComponent(out EnemyIdentifier e) && !e.dead)
                {
                    enemyId = e;

                    break;
                }

            }

            if (enemyId == null)
            {
                foreach (var hit in hits.Where(h => h.attachedRigidbody != null && h.attachedRigidbody.TryGetComponent(out EnemyIdentifier e) && !e.dead && (e.gce != null && e.gce.onGround)).OrderBy(h => Vector3.Distance(h.transform.position, transform.position)))
                {
                    if (hit.attachedRigidbody.TryGetComponent(out EnemyIdentifier e) && !e.dead)
                    {
                        enemyId = e;

                        break;
                    }
                }
            }

        }

        Aim();
    }

    private void Aim()
    {
        if (enemyId != null)
        {
            var targetDir = (enemyId.transform.position + Vector3.up * 1.8f - transform.position).normalized;
            var rot = Quaternion.LookRotation(targetDir.normalized, transform.up);
            //transform.eulerAngles = targetEuler;

            transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * 8f);
        }
    }
}

[HarmonyPatch(typeof(global::Grenade), "Start")]
public class PatchRocketSpread
{
    public static void Postfix(global::Grenade __instance)
    {
        if (__instance.sourceWeapon != null &&
            __instance.sourceWeapon.TryGetComponent(out WeaponTypeComponent wt) &&
            wt.value != WeaponVariationType.FreezeframeRocketLauncher)
        {
            if (PatchMultiRocket.barrageSize > 1)
            {
                if (PatchMultiRocket.extraRockets < 4)
                    __instance.transform.localEulerAngles += new Vector3(
                        Random.Range(-8f, 2f),
                        Random.Range(-16f, 16f));


                __instance.GetOrAddComponent <RocketHomingComponent>().grenade = __instance;
            }
        }



    }
}


