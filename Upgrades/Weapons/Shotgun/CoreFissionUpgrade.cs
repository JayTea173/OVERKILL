using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades.Shotgun;

public class CoreFissionUpgrade : LeveledUpgrade
{

   
    public override double AppearChanceWeighting => RarityChances.Epic;

    public override string Name => "Core Fission";

    public override string Description =>
        $"\"Hold on, you meant to say Fusion Core, right\", I hear you say...\nNo, screw that!\n\nSo anyway, I cut this core in half.\nWhen bouncing the ejected core, it is split in {level + 1}. Try using your knuckleblaster shockwave on your shotgun core for projectile boost 2.0";

    public override int MaxLevel => 3;

    public override Rarity Rarity => Rarity.Epic;
    public override Rarity MaxRarity => Rarity.Epic;

    public override void Apply()
    {
        PatchKnuckleBlasterSplitGrenade.numCopies += 3 * level;
    }

    public override void Absolve()
    {
        PatchKnuckleBlasterSplitGrenade.numCopies -= 3 * level;
    }

}

public class IsSplitGrenadeComponent : MonoBehaviour
{
    public float preventSplitTime = 2.0f;
    public int frames = 20;

    private void FixedUpdate()
    {
        preventSplitTime -= Time.timeScale * Time.fixedDeltaTime;
        frames--;
        if (preventSplitTime <= 0f || frames <= 0)
            Destroy(this);
    }
}

[HarmonyPatch(typeof(Punch), "BlastCheck")]
public class PatchKnuckleBlasterBlastWaveIdentifier
{
    public static void Postfix(Punch __instance)
    {
        __instance.blastWave.GetComponentInChildren <Explosion>().sourceWeapon = __instance.gameObject;

    }
}

[HarmonyPatch(typeof(Explosion), "Collide")]
public class PatchKnuckleBlasterSplitGrenade
{
    public static int numCopies = 0;
    //private void Collide(Collider other)
    public static void Prefix(Explosion __instance, Collider other)
    {
        if (numCopies <= 0)
            return;
        
        if (__instance.sourceWeapon == null || __instance.sourceWeapon.GetComponent <Punch>() == null)
            return;
        
        if (other.TryGetComponent(out Grenade grenade) && !grenade.rocket && other.GetComponent<IsSplitGrenadeComponent>() == null)
        {
            MonoSingleton<TimeController>.Instance.ParryFlash();
            StyleHUD.Instance.AddPoints(20, "p-boost 2.0", grenade.sourceWeapon);
            grenade.gameObject.AddComponent <IsSplitGrenadeComponent>();
            
            var hits = Physics.OverlapSphere(grenade.transform.position, 500f, 1 << 12, QueryTriggerInteraction.Ignore);

            var closestHits = hits.
                              Where(
                                  h => h.attachedRigidbody != null &&
                                       h.attachedRigidbody.TryGetComponent(out EnemyIdentifier e) &&
                                       !e.dead).
                              OrderBy(h => Vector3.Distance(h.transform.position, grenade.transform.position)).ToArray();
            
            
            for (int i = 0; i < numCopies; i++)
            {
                var inst = Object.Instantiate(grenade.gameObject, grenade.transform.parent);
                inst.transform.position += Random.onUnitSphere * 1.5f;
                var rb = inst.GetComponent <Rigidbody>();
                var speed = rb.velocity.magnitude;

                var targetPos = (closestHits.Length > 0 ? closestHits[i % Math.Min(4, closestHits.Length)].attachedRigidbody.position : (inst.transform.position + Random.insideUnitSphere)) +
                                Vector3.up * 0.8f;

                //targetPos.y += Vector3.Distance(inst.transform.position, targetPos);
                //rb.useGravity = false;
                
                //rb.velocity = (targetPos - inst.transform.position).normalized * speed * 1000f;
                rb.mass = 15f;
                rb.velocity = NewMovement.Instance.cc.cam.transform.forward * Random.Range(200f, 300f);
            }

            var rb0 = grenade.GetComponent <Rigidbody>();
            rb0.mass = 15f;
            rb0.velocity = NewMovement.Instance.cc.cam.transform.forward * 250f;
        }

        //__instance.GetComponent <SphereCollider>().radius *= 10f;

    }
}


