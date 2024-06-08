using System;
using System.Collections.Generic;
using System.Linq;
using GameConsole.pcon;
using HarmonyLib;
using UnityEngine;
using Random = System.Random;

namespace OVERKILL.Upgrades;

public static class RandomUpgrade
{
    private static IUpgrade[] upgrades;
    private static double weightTotal;

    private static IUpgrade[] currentlyAvailable;
    private static double weightCurrentTotal;

    public static void UpdateAvailable()
    {
        currentlyAvailable = upgrades.Where(u => u.IsObtainable).ToArray();
        weightCurrentTotal = currentlyAvailable.Sum(u => u.AppearChanceWeighting);
    }

    public static IUpgrade[] All => upgrades;

    public static IUpgrade Get(Random rnd)
    {
        if (upgrades == null)
            Initialize();


        int index = 0;
        int lastIndex = currentlyAvailable.Length;
        var r = rnd.NextDouble() * weightCurrentTotal;
        
        while (index < lastIndex)
        {
            // Do a probability check with a likelihood of weights[index] / weightSum.
            if (r < currentlyAvailable[index].AppearChanceWeighting)
            {
                var upgrade = (IUpgrade)Activator.CreateInstance(currentlyAvailable[index].GetType());
                if (upgrade is IRandomizable randomizableUpgrade)
                    randomizableUpgrade.Randomize(rnd.Next());

                if (PlayerUpgradeStats.Instance.upgrades.TryGetValue(upgrade.GetHashCode(), out var existing))
                    upgrade = existing;

                return upgrade;
            }
 
            // Remove the last item from the sum of total untested weights and try again.
            r -= currentlyAvailable[index++].AppearChanceWeighting;
        }

        throw new Exception("WTF");
    }

    public static void Initialize()
    {
        List <IUpgrade> l = new List <IUpgrade>();

        foreach (var t in typeof(RandomUpgrade).Assembly.GetTypes())
        {
            if (t.IsClass && !t.IsAbstract && typeof(IUpgrade).IsAssignableFrom(t))
            {
                OK.Log($"Registered upgrade type: {t.Name}");
                IUpgrade inst = (IUpgrade)Activator.CreateInstance(t);
                l.Add(inst);
                weightTotal += inst.AppearChanceWeighting;
            }
        }

        upgrades = l.ToArray();
    }
}
