using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Logging;
using HarmonyLib;
using OVERKILL.HakitaPls;
using UnityEngine;

namespace OVERKILL.Upgrades.RocketLauncher;

public class BloodFreezeUpgrade : WeaponUpgrade
{
    public override int OptionsSortPriority => 1;

    public override double AppearChanceWeighting =>
        RarityChances.Overkill * AppearChanceWeightingOptionMultiplier * 1.1d;
    
    public override string Name => "BLOOD FREEZE";

    public override string Description =>
        $"When using the Freezeframe Rocket Launcher, all health and stamina is also frozen and only used up once the timefreeze ends.\n\nAt Level 2: Enemies are affected by this as well and take additional EXPONENTIAL damage (e.g. 50 => 70 and 120 => 200). Sandified enemies are immune.\n\n\n<size=75%>I can see the future... <color=red>FRIEZA!!</color></size>";

    public override int MaxLevel => 2;
    //Level 3 idea: enemies that dealt damage but die at the end wont deal damage to you

    public override Rarity Rarity => Rarity.Overkill; 

    public override void Apply()
    {
        BloodFreezePatch.enabled = true;
        Events.OnFreezeStart.Post += OnFreezeStart;
        Events.OnFreezeEnd.Post += OnFreezeEnd;
        Events.OnPlayerRespawn.Pre += OnDeath;
        Events.OnDealDamage.LatePre += OnDamageDealtLatePre;
        

        if (level > 0)
        {
            //OK.Log("CHANGING MASS FROM " + NewMovement.Instance.rb.mass);
            
        }

    }
    
    public override void Absolve()
    {
        BloodFreezePatch.enabled = false;
        Events.OnFreezeStart.Post -= OnFreezeStart;
        Events.OnFreezeEnd.Post -= OnFreezeEnd;
        Events.OnDealDamage.LatePre -= OnDamageDealtLatePre;
    }

    private void OnDamageDealtLatePre(EventDelegates.DeliverDamageEventData evntdata, EnemyIdentifier enemy, GameObject target, Vector3 force, Vector3 hitpoint, ref float multiplier, bool tryforexplode, float critmultiplier, GameObject sourceweapon, bool ignoretotaldamagetakenmultiplier, bool fromexplosion)
    {
        if (!BloodFreezePatch.enabled || !FreezeFrameRocketLauncherPatches.TimeFrozen || enemy == null || level < 2)
            return;

        //sandified enemies are immune to BLOOD FREEZE
        if (enemy.sandified)
            return;

        if (BloodFreezePatch.affectedEnemies.TryGetValue(enemy, out var e))
        {
            e.healthStored -= multiplier;
            e.lastTarget = target;
        }
        else
        {
            e = new BloodFreezePatch.EnemyEntry() {healthStored = -multiplier};
            e.lastTarget = target;
            BloodFreezePatch.affectedEnemies.Add(enemy, e);
        }
        
        var dmg = -e.healthStored;
        dmg = Math.Round(Math.Pow(dmg, 1.2));

        if (!e.alreadyDead && dmg >= enemy.health * enemy.totalHealthModifier)
        {
            e.alreadyDead = true;
            StyleHUD.Instance.AddPoints(50, "<color=red>omae wa mou</color>");
        }

        multiplier = 0f;
    }

    private void OnFreezeStart(global::RocketLauncher sender)
    {
        BloodFreezePatch.diedDuringFreeze = BloodFreezePatch.resurrectedDuringFreeze = false;
    }



    private void OnDeath()
    {
        BloodFreezePatch.healthStored = 0;
        BloodFreezePatch.staminaStored = 0;
        BloodFreezePatch.diedDuringFreeze = BloodFreezePatch.resurrectedDuringFreeze = false;
        BloodFreezePatch.storedVelocity = Vector3.zero;
        BloodFreezePatch.affectedEnemies.Clear();

    }

    private void OnFreezeEnd(global::RocketLauncher sender)
    {
        //OK.Log($"Stored HP: {BloodFreezePatch.healthStored}, SP: {BloodFreezePatch.staminaStored}");

        var nm = NewMovement.Instance;
        
        if (BloodFreezePatch.healthStored > 0)
        {
            StyleHUD.Instance.AddPoints(10, $"FREEZE", postfix: $" +{BloodFreezePatch.healthStored:0.}".ColoredRTF(Color.green));
            nm.GetHealth((int)BloodFreezePatch.healthStored, true);
            
        } else if (BloodFreezePatch.healthStored < 0)
        {
            StyleHUD.Instance.AddPoints(10, $"FREEZE", postfix: $" {BloodFreezePatch.healthStored:0.}".ColoredRTF(Color.red));
            nm.GetHurt((int)-BloodFreezePatch.healthStored, false, 1f, ignoreInvincibility: true);
        }

        if (BloodFreezePatch.staminaStored != 0)
        {
            nm.boostCharge += BloodFreezePatch.staminaStored;
            nm.boostCharge = Mathf.Clamp(nm.boostCharge, -100f, PatchStaminaRegenSpeed.maxStamina);

        }
        else
        {
            nm.boostCharge = Mathf.Max(0, nm.boostCharge - BloodFreezePatch.staminaStored);
        }

        NewMovement.Instance.rb.velocity += BloodFreezePatch.storedVelocity;
        var copy = new Dictionary <EnemyIdentifier, BloodFreezePatch.EnemyEntry>(BloodFreezePatch.affectedEnemies);
        NewMovement.Instance.StartCoroutine(AffectNextEnemy(sender, copy));


        OnDeath();
       
    }

    private IEnumerator AffectNextEnemy(global::RocketLauncher sender, Dictionary <EnemyIdentifier, BloodFreezePatch.EnemyEntry> copy)
    {
        int num = copy.Count;

        int sanity = 0;
        while (copy.Any())
        {
            var kv = copy.FirstOrDefault(
                e0 => e0.Key != null && !e0.Key.dead && e0.Value.healthStored < 0d);

            if (kv.Key == null)
                yield break;


            copy.Remove(kv.Key);

            var dmg = -kv.Value.healthStored;
            dmg = Math.Round(Math.Pow(dmg, 1.2));

            kv.Key.DeliverDamage(
                kv.Value.lastTarget,
                Vector3.up * 1000f,
                kv.Value.lastTarget.transform.position,
                (float)dmg, true,
                sourceWeapon: sender.gameObject);

            yield return new WaitForSeconds(0.05f);

            sanity++;

            if (sanity > num)
            {
                OK.LogTraced("FAILED SANITY!", LogLevel.Error);
                break;
            }
        }
    }

    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return wtype.value == WeaponVariationType.FreezeframeRocketLauncher;
    }
}

[HarmonyPatch]
public class BloodFreezePatch
{
    public class EnemyEntry
    {
        public GameObject lastTarget;
        public double healthStored;
        public bool alreadyDead;
    }

    public static Dictionary <EnemyIdentifier, EnemyEntry> affectedEnemies = new Dictionary <EnemyIdentifier, EnemyEntry>();
    
    public static bool enabled;

    public static double healthStored;
    public static float staminaStored;
    public static Vector3 storedVelocity;

    private static float staminaBeforeUpdate;
    private static bool borrowedDashThisUpdate;
    private static Vector3 velocityLastFrame;
    public static bool diedDuringFreeze;
    public static bool resurrectedDuringFreeze;

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHurt))]
    [HarmonyPrefix]
    [HarmonyPriority(Priority.LowerThanNormal)]
    public static bool OnDamageTaken(NewMovement __instance, int damage,
        bool invincible,
        float scoreLossMultiplier = 1f,
        bool explosion = false,
        bool instablack = false,
        float hardDamageMultiplier = 0.35f,
        bool ignoreInvincibility = false)
    {
        if (!FreezeFrameRocketLauncherPatches.TimeFrozen || !enabled)
            return true;

        healthStored -= damage;

        if (healthStored + __instance.hp <= 0 && !diedDuringFreeze)
        {
            diedDuringFreeze = true;
            StyleHUD.Instance.AddPoints(900, "<color=red><b>I'M DEAD!</b></color>");
        }
        __instance.FakeHurt();
        return false;
    }
    
    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.GetHealth))]
    [HarmonyPrefix]
    public static bool OnHealed(NewMovement __instance, int health, bool silent, bool fromExplosion = false)
    {
        if (!FreezeFrameRocketLauncherPatches.TimeFrozen || !enabled)
            return true;

        healthStored += health;

        if (diedDuringFreeze && !resurrectedDuringFreeze && healthStored + __instance.hp > 0)
        {
            resurrectedDuringFreeze = true;
            StyleHUD.Instance.AddPoints(100, "<color=green>nevermind...</color>");
        }
        return false;
    }

    [HarmonyPatch(typeof(NewMovement), "Update")]
    [HarmonyPrefix]
    public static void OnUpdateGetDashUsage(NewMovement __instance)
    {
        if (!FreezeFrameRocketLauncherPatches.TimeFrozen || !enabled)
            return;
        
        staminaBeforeUpdate = __instance.boostCharge;
        borrowedDashThisUpdate = false;
        
        /*
        if (velocityLastFrame.sqrMagnitude > 0.01f)
        {
            var velDelta = __instance.rb.velocity - velocityLastFrame;
            storedVelocity += velDelta;
            OK.Log($"Velocity changed by: {velDelta}, stored now: {storedVelocity}");
        }
        */
        
        
        if (MonoSingleton <InputManager>.Instance.InputSource.Dodge.WasPerformedThisFrame &&
            __instance.activated &&
            !__instance.slowMode &&
            !GameStateManager.Instance.PlayerInputLocked)
        {
            if ((bool) (UnityEngine.Object) __instance.groundProperties && !__instance.groundProperties.canDash || __instance.modNoDashSlide)
            {
                if (__instance.modNoDashSlide || !__instance.groundProperties.silentDashFail)
                    UnityEngine.Object.Instantiate<GameObject>(__instance.staminaFailSound);
            }
            else if ((double)__instance.boostCharge < 100.0)
            {
                //OK.Log("Wanna dash but no stam");
                StyleHUD.Instance.AddPoints(1, "BORROWED WINGS");
                staminaStored -= 100f;
                __instance.boostCharge += 100f;
                borrowedDashThisUpdate = true;
            }
            
        }
    }

    [HarmonyPatch(typeof(NewMovement), "Update")]
    [HarmonyPostfix]
    public static void OnUpdateGetDashUsagePost(NewMovement __instance)
    {
        if (!FreezeFrameRocketLauncherPatches.TimeFrozen || !enabled)
            return;

        var staminaDelta = __instance.boostCharge - staminaBeforeUpdate;
        
        staminaStored += staminaDelta;
        __instance.boostCharge = staminaBeforeUpdate;
        velocityLastFrame = __instance.rb.velocity;
    }



}
