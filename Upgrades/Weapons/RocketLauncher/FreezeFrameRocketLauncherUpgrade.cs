using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using OVERKILL.HakitaPls;
using OVERKILL.UI.Upgrades;
using UnityEngine;
using UnityEngine.Audio;

namespace OVERKILL.Upgrades.RocketLauncher;

public class FreezeFrameRocketLauncherUpgrade : WeaponUpgrade, IRandomizable
{
    public override int OptionsSortPriority => 1;
    
    public override string Name => "<color=#00ffff>ICE ICE BABY</color>";

    public override string Description => $"When activating your Freezeframe Rocket Launcher while you are slam-falling, time is slowed down for the duration that the time freeze remains active. The longer you have been slam falling, the stronger the time dilation. During slowed time, all damage dealt is increased depending on the slow down itself. At half speed, you deal {GetDamageBonusAtHalfSpeed():0.%} more damage.";

    public override double AppearChanceWeighting =>
        RarityChances.Overkill * AppearChanceWeightingOptionMultiplier;

    public override Rarity Rarity => Rarity.Overkill;

    public override int MaxLevel => 3;

    public double GetDamageBonusAtHalfSpeed()
    {
        return 0.5d + (level-1) * 0.5d;
    }

    public override void Apply()
    {
        FreezeFrameRocketLauncherPatches.SetAvailable(true);
        FreezeFrameRocketLauncherPatches.damageBonusAtHalfSpeed = GetDamageBonusAtHalfSpeed();


    }

    public override void Absolve()
    {
        FreezeFrameRocketLauncherPatches.SetAvailable(false);
    }

    public override bool AffectsWeapon(WeaponTypeComponent wtype)
    {
        return wtype.value == WeaponVariationType.FreezeframeRocketLauncher;
    }

    public void Randomize(int seed)
    {
        
        
    }
}

[HarmonyPatch(typeof(global::RocketLauncher))]
public static class FreezeFrameRocketLauncherPatches
{
    private static FieldInfo pitchField = typeof(NewMovement).GetField("currentAllPitch", BindingFlags.Instance | BindingFlags.NonPublic);
    private static FieldInfo audmixField = typeof(CameraController).GetField("audmix", BindingFlags.Instance | BindingFlags.NonPublic);
    private static Coroutine activeCoroutine;
    private static float currentTargetTimescale;
    public static double damageBonusAtHalfSpeed = 0d;
    private static bool available = false;
    public static bool TimeFrozen {get; private set;}

    public static void SetAvailable(bool b)
    {
        available = b;

        if (available)
        {
            Events.OnDealDamage.Pre += OnPreDamageDealt;

        } else
        {
            Events.OnDealDamage.Pre -= OnPreDamageDealt;
            
            CancelCurrentTimescaleMod();
            SetTimescale(1f);
        }
    }

    private static void OnPreDamageDealt(EventDelegates.DeliverDamageEventData evntdata, EnemyIdentifier enemy, GameObject target, Vector3 force, Vector3 hitpoint, ref float multiplier, bool tryforexplode, float critmultiplier, GameObject sourceweapon, bool ignoretotaldamagetakenmultiplier, bool fromexplosion)
    {
        if (available && Time.timeScale < 1f && TimeFrozen)
        {
            
            float mul = (float)damageBonusAtHalfSpeed;
            mul *= 0.5f / Time.timeScale;

            mul = Mathf.Clamp(1f + mul, 1f, 20f);

            multiplier *= mul;
            
        }
    }

    [HarmonyPatch(nameof(global::RocketLauncher.FreezeRockets))]
    [HarmonyPrefix]
    public static void StartFreeze(global::RocketLauncher __instance)
    {
        TimeFrozen = true;
        Events.OnFreezeStart.Pre?.Invoke(__instance);
        Events.OnFreezeStart.LatePre?.Invoke(__instance);
        
        if (!available)
            return;
        
        

        float slamForce = Mathf.Clamp(NewMovement.Instance.slamForce, 1.5f, 15f) / 1.5f;

        float ts = Mathf.Max(0.05f, 0.5f - ((slamForce - 1f) / 22f));

        if (NewMovement.Instance.slamForce > 0f)
        {
            activeCoroutine = NewMovement.Instance.StartCoroutine(FreezeCoroutine(__instance, ts, keepoverwriting: true));
        }
    }

    [HarmonyPatch(nameof(global::RocketLauncher.FreezeRockets))]
    [HarmonyPrefix]
    public static void StartFreezePost(global::RocketLauncher __instance)
    {
        Events.OnFreezeStart.Post?.Invoke(__instance);
        
    }

    public static void CancelCurrentTimescaleMod()
    {
        if (activeCoroutine != null)
            NewMovement.Instance.StopCoroutine(activeCoroutine);
        
        TimeFrozen = false;
    }

    public static void SetTimescale(float ts)
    {
        var audmix = (AudioMixer[])audmixField.GetValue(CameraController.Instance);
            
        foreach (AudioMixer audioMixer in audmix)
            audioMixer.SetFloat("allPitch", ts);
        
        Time.timeScale = ts;
    }
    
    private static IEnumerator FreezeCoroutine(global::RocketLauncher __instance, float targetTimeScale = 0.5f, float startupTime = 0.5f, bool keepoverwriting = false)
    {

        float t = 0f;
        float startTs = Time.timeScale;
        currentTargetTimescale = targetTimeScale;
        
        //startup anim
        while (t < startupTime)
        {
            float ts = startTs - (t / startupTime) * (startTs - currentTargetTimescale);
            SetTimescale(ts);
            yield return new WaitForEndOfFrame();
            t += Time.unscaledDeltaTime;
        }
        
        SetTimescale(currentTargetTimescale);

        if (keepoverwriting)
        {
            while (true)
            {
                if (!UpgradeScreen.Instance.gameObject.activeSelf && Time.timeScale > 0)
                {
                    SetTimescale(currentTargetTimescale);
                }

                yield return new WaitForEndOfFrame();
                
            }
        }
        
    }
    
    [HarmonyPatch(nameof(global::RocketLauncher.UnfreezeRockets))]
    [HarmonyPrefix]
    public static void EndFreeze(global::RocketLauncher __instance)
    {
        TimeFrozen = false;
        Events.OnFreezeEnd.Pre?.Invoke(__instance);
        Events.OnFreezeEnd.LatePre?.Invoke(__instance);
        //Events.OnDeath
       
        
        if (!available)
            return;
        
        CancelCurrentTimescaleMod();
        activeCoroutine = NewMovement.Instance.StartCoroutine(FreezeCoroutine(__instance, 1f, 0.3f, keepoverwriting: false));
    }

    [HarmonyPatch(nameof(global::RocketLauncher.UnfreezeRockets))]
    [HarmonyPrefix]
    public static void EndFreezePost(global::RocketLauncher __instance)
    {
        Events.OnFreezeEnd.Post?.Invoke(__instance);
    }
}

