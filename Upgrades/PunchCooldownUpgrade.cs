using HarmonyLib;
using OVERKILL.Upgrades.RocketLauncher;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class PunchCooldownUpgrade : LeveledUpgrade, IRandomizable
{
    public override double AppearChanceWeighting => RarityChances.Overkill;

    public override int MaxLevel => 1;

    public override string Name => "MISTER FISTER";

    public override string Description => $"Increases your normal punching speed by 100%.";
    
    public override Rarity MaxRarity => Rarity.Overkill;


    public override void Apply()
    {
        PatchResetPunchOnParry.effectEnabled = true;
        PatchPunchSpeed.effectEnabled = true;
    }

    public override void Absolve()
    {
        PatchResetPunchOnParry.effectEnabled = false;
        PatchPunchSpeed.effectEnabled = false;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        Rarity = Rarity.Overkill;
    }
}


[HarmonyPatch(typeof(global::Punch), nameof(Punch.Parry))]
public class PatchResetPunchOnParry
{
    public static bool effectEnabled;
    public static void Postfix(global::Punch __instance, bool hook = false, EnemyIdentifier eid = null, string customParryText = "")
    {
        if (!effectEnabled)
            return;
        
        FistControl.Instance.fistCooldown = 0f;
        FistControl.Instance.weightCooldown = 0f;

        if (__instance.type == FistType.Standard)
        {
            __instance.anim.speed = 2f;
        }

    }
}

[HarmonyPatch(typeof(global::Punch), nameof(Punch.PunchStart))]
public class PatchPunchSpeed
{
    public static bool effectEnabled;
    public static void Postfix(global::Punch __instance)
    {
        if (!effectEnabled)
            return;
        
        if (__instance.type == FistType.Standard)
        {
            FistControl.Instance.fistCooldown *= .5f;
            FistControl.Instance.weightCooldown *= .5f;
            __instance.anim.speed = 2f;
        }

    }
}
