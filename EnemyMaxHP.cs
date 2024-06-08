using System.Collections.Generic;
using BepInEx.Logging;
using HarmonyLib;

namespace OVERKILL;

public static class EnemyMaxHP
{
    private static Dictionary <EnemyIdentifier, float> maxHPs = new Dictionary <EnemyIdentifier, float>(128);

    public static bool TryGet(EnemyIdentifier enemy, out float value)
    {
        return maxHPs.TryGetValue(enemy, out value);
    }

    public static void Register(EnemyIdentifier enemy, float value)
    {
        maxHPs.Add(enemy, value);
    }

    public static bool Unregister(EnemyIdentifier enemy)
    {
        var res = maxHPs.Remove(enemy);

        if (maxHPs.Count > 120)
        {
            OK.Log($"A lot of max hp trackers remain after removing {enemy.gameObject.name}. left now: {maxHPs.Count}", LogLevel.Warning);
        }

        return res;
    }
}

