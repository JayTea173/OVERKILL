using System;
using System.Collections.Generic;
using HarmonyLib;
using OVERKILL.Upgrades;

namespace OVERKILL.Patches;

public interface IOnPlayerDeath
{
    public void OnPlayerDeath(NewMovement nm);
}

[HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
public class CleanupPlayerOnDeathPatch
{
    public static HashSet <IOnPlayerDeath> registry = new HashSet <IOnPlayerDeath>();

    public static void Postfix(NewMovement __instance)
    {
        /*
        if (registry.Count == 0)
        {
            PlayerDeathHandler.Instance.Register();
        }
        OK.Log($"ONDEATH GOT: {registry.Count}");
        foreach (var e in registry)
        {
            
            e.OnPlayerDeath(__instance);
        }
        */
        PlayerDeathHandler.Instance.OnPlayerDeath(__instance);
    }
}

[HarmonyPatch(typeof(NewMovement), "Start")]
public class CleanupPlayerOnCreationPatch
{
    public static HashSet <IOnPlayerDeath> registry = new HashSet <IOnPlayerDeath>();

    public static void Postfix(NewMovement __instance)
    {
        /*
        if (registry.Count == 0)
        {
            PlayerDeathHandler.Instance.Register();
        }
        OK.Log($"ONDEATH ONENABLE: {registry.Count}");
        foreach (var e in registry)
        {
            
            e.OnPlayerDeath(__instance);
        }
        */
        PlayerDeathHandler.Instance.OnPlayerDeath(__instance);
    }
}

public static class OnPlayerDeathExtensions
{
    public static bool Register(this IOnPlayerDeath inst)
    {
        return CleanupPlayerOnDeathPatch.registry.Add(inst);
    }
    public static bool Deregister(this IOnPlayerDeath inst)
    {
        return CleanupPlayerOnDeathPatch.registry.Remove(inst);
    }
}

public class PlayerDeathHandler : MonoSingleton <PlayerDeathHandler>, IOnPlayerDeath
{
    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        this.Register();
    }

    private void OnDisable()
    {
        this.Deregister();
    }

    public void OnPlayerDeath(NewMovement nm)
    {
        PlayerUpgradeStats.Instance.Reset();
    }
}
