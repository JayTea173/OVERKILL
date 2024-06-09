using System.Linq;
using System.Reflection;
using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class WallPiercerUpgrade : WeaponUpgrade
{
    public override int OptionsSortPriority => 1;
    
    public override string Name => "PIERCE HER";

    public override string Description => "Allows the Piercer Revolver charged shot to fire through solid walls. Deals extra damage depending on how close an obstructing wall is to and how many WORTHLESS non-enemies were <i>penetrated</i> along the way.";

    public override Rarity Rarity => Rarity.Overkill;

    public override int MaxLevel => 1;

    public override double AppearChanceWeighting =>
        RarityChances.Overkill * 1.6d * AppearChanceWeightingOptionMultiplier;

    #region Public

    public override void Absolve()
    {
        WallPiercerPatches.enabled = false;
    }

    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return wtype.value == WeaponVariationType.PiercerRevolver ||
               wtype.value == WeaponVariationType.PiercerSlabRevolver;
    }

    public override void Apply()
    {
        WallPiercerPatches.enabled = true;
    }

    #endregion
}

[HarmonyPatch(typeof(RevolverBeam))]
public static class WallPiercerPatches
{
    
    public static bool enabled;
    private static RaycastHit[] sortedWallHits;
    private static float unmodifiedPiercerDamage;
    private static readonly FieldInfo pierceLayerMaskField = typeof(RevolverBeam).GetField(
        "pierceLayerMask",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo enemyLayerMaskField = typeof(RevolverBeam).GetField(
        "enemyLayerMask",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo hitField = typeof(RevolverBeam).GetField(
        "hit",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo shotHitPointField = typeof(RevolverBeam).GetField(
        "shotHitPoint",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo didntHitField = typeof(RevolverBeam).GetField(
        "didntHit",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo lrField = typeof(RevolverBeam).GetField(
        "lr",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo allHitsField = typeof(RevolverBeam).GetField(
        "allHits",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo checkWaterMethod = typeof(RevolverBeam).GetMethod(
        "CheckWater",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo PiercingShotOrderMethod = typeof(RevolverBeam).GetMethod(
        "PiercingShotOrder",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static bool currentShotIsPiercer;
    private static Transform lastEnemyHit;

    #region Public

    [HarmonyPatch(nameof(RevolverBeam.ExecuteHits)), HarmonyPrefix]
    public static void ApplyDamageBonusPiercer(RevolverBeam __instance, RaycastHit currentHit)
    {
        if (!enabled || !currentShotIsPiercer || sortedWallHits == null || sortedWallHits.Length <= 0 || currentHit.transform == null)
            return;

        var gameObject = currentHit.transform.gameObject;
        
        if (gameObject.CompareTag("Enemy") ||
            gameObject.CompareTag("Body") ||
            gameObject.CompareTag("Limb") ||
            gameObject.CompareTag("EndLimb") ||
            gameObject.CompareTag("Head"))
        {
            int numWallsOnTheWay = 0;
            var distToPlayer = Vector3.Distance(__instance.transform.position, currentHit.transform.position);

            foreach (var h in sortedWallHits)
            {
                if (Vector3.Distance(__instance.transform.position, h.point) < distToPlayer)
                    numWallsOnTheWay++;
                else
                    break;
            }

            if (numWallsOnTheWay <= 0)
                return;
            
            var distToFirstWall = Vector3.Distance(currentHit.point, sortedWallHits[0].point);


            var bonusDamageFromDistance = Mathf.Pow(distToFirstWall * 0.2f, 1.35f) * 0.05f;
            //var bonusDamageFromDistance = 0f;
            bonusDamageFromDistance = Mathf.Clamp(bonusDamageFromDistance, 0f, 10f);
            var bonusDamageFromWalls = numWallsOnTheWay * 0.5f;

            unmodifiedPiercerDamage = __instance.damage;
            __instance.damage += bonusDamageFromWalls + bonusDamageFromDistance;

            if (lastEnemyHit == null)
            {
                lastEnemyHit = currentHit.transform;
                
                StyleHUD.Instance.AddPoints(150, "WALLBANG", __instance.sourceWeapon, count: numWallsOnTheWay);
            }


        }
    }

    [HarmonyPatch(nameof(RevolverBeam.ExecuteHits)), HarmonyPostfix]
    public static void ApplyDamageBonusPiercerPost(RevolverBeam __instance, RaycastHit currentHit)
    {
        if (!enabled || !currentShotIsPiercer)
            return;

        __instance.damage = unmodifiedPiercerDamage;
    }

    [HarmonyPatch("Shoot"), HarmonyPrefix]
    public static bool MakeWallPiercer(RevolverBeam __instance)
    {
        currentShotIsPiercer = false;
        
        if (!enabled)
            return true;

        //is there any way to check beam if it is beam from charged piercer revolver?
        if (__instance.beamType != BeamType.Revolver)
            return true;

        var isChargedPiercerShot = __instance.hitAmount >= 6;

        if (!isChargedPiercerShot)
            return true;

        if (!__instance.sourceWeapon.TryGetComponent(out WeaponTypeComponent wt))
            return true;

        if (wt.value != WeaponVariationType.PiercerRevolver && wt.value != WeaponVariationType.PiercerSlabRevolver)
            return true;
        

        currentShotIsPiercer = true;
        lastEnemyHit = null;

        var pierceLayerMask = (LayerMask)pierceLayerMaskField.GetValue(__instance);
        var shotHitPoint = (Vector3)shotHitPointField.GetValue(__instance);
        var hit = (RaycastHit)hitField.GetValue(__instance);
        var didntHit = (bool)didntHitField.GetValue(__instance);
        RaycastHit[] allHits = (RaycastHit[])allHitsField.GetValue(__instance);
        var enemyLayerMask = (LayerMask)enemyLayerMaskField.GetValue(__instance);
        var lr = (LineRenderer)lrField.GetValue(__instance);
        /*
        
        
        
        var pierceLayerMask = (LayerMask)pierceLayerMaskField.GetValue(__instance);
        */

        RaycastHit[] hits = Physics.RaycastAll(
            __instance.transform.position,
            __instance.transform.forward,
            float.PositiveInfinity,
            pierceLayerMask);
        
        
        

        
        if (hits.Length > 0)
        {
            hits = hits.OrderBy(h => h.distance).ToArray();
            sortedWallHits = hits;
            hit = hits[hits.Length - 1];
            
            //play hit effects at all hit points
            foreach (RaycastHit h in hits)
            {
                GameObject gameObject = Object.Instantiate(
                    __instance.hitParticle,
                    h.point,
                    __instance.transform.rotation);

                gameObject.transform.forward = h.normal;
            }

            shotHitPoint = __instance.transform.position + __instance.transform.forward * 1500f;
        }
        
        
        
        {
            shotHitPoint = __instance.transform.position + __instance.transform.forward * 1500f;
            didntHit = true;
        }

        hitField.SetValue(__instance, hit);

        checkWaterMethod.Invoke(
            __instance,
            new object[] {Vector3.Distance(__instance.transform.position, shotHitPoint)});

        var radius = 0.6f;

        if (__instance.beamType == BeamType.Railgun)
            radius = 1.2f;
        else if (__instance.beamType == BeamType.Enemy)
            radius = 0.3f;

        allHits = Physics.SphereCastAll(
            __instance.transform.position,
            radius,
            __instance.transform.forward,
            Vector3.Distance(__instance.transform.position, shotHitPoint),
            enemyLayerMask,
            QueryTriggerInteraction.Collide);

        Vector3 position = __instance.transform.position;

        if (__instance.alternateStartPoint != Vector3.zero)
            position = __instance.alternateStartPoint;

        lr.SetPosition(0, position);
        lr.SetPosition(1, shotHitPoint);

        pierceLayerMaskField.SetValue(__instance, pierceLayerMask);
        shotHitPointField.SetValue(__instance, shotHitPoint);
        hitField.SetValue(__instance, hit);
        didntHitField.SetValue(__instance, didntHit);
        allHitsField.SetValue(__instance, allHits);
        enemyLayerMaskField.SetValue(__instance, enemyLayerMask);

        //lrField.SetValue(__instance, lr);        

        if (__instance.hitAmount != 1)
            PiercingShotOrderMethod.Invoke(__instance, null);

        Transform child = __instance.transform.GetChild(0);

        if (!__instance.noMuzzleflash)
            child.SetPositionAndRotation(position, __instance.transform.rotation);
        else
            child.gameObject.SetActive(false);

        return false;

        /*
        var pierceLayerMask = (LayerMask)pierceLayerMaskField.GetValue(__instance);
        
        pierceLayerMask ^= 256;
        pierceLayerMask ^= 16777216;
    
        
        for (int i = 0; i < 32; i++)
        {
            var i0 = 1 << i;
            if ((pierceLayerMask.value & i0) != 0)
                OK.Log($"Revolver beam hits layer: {LayerMask.LayerToName(i)}");
        }
        
        
    
        pierceLayerMaskField.SetValue(__instance, pierceLayerMask);
        */
    }

    #endregion
}
