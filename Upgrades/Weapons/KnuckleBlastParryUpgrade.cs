using System.Linq;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class KnuckleBlastParryUpgrade : IUpgrade
{
    public int OptionsSortPriority => 0;
    protected bool Equals(KnuckleBlastParryUpgrade other)
    {
        return Name == other.Name;
    }

    public bool Equals(IUpgrade other)
    {
        return other != null && Name == other.Name;
    }

    public override bool Equals(object obj)
    {
        if (ReferenceEquals(null, obj))
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != this.GetType())
            return false;

        return Equals((KnuckleBlastParryUpgrade)obj);
    }

    public override int GetHashCode()
    {
        return (Name != null ? Name.GetHashCode() : 0);
    }

    public bool IsObtainable => !PlayerUpgradeStats.Instance.upgrades.ContainsKey(this.GetHashCode());

    public double AppearChanceWeighting => RarityChances.Rare * AppearChanceWeightingOptionMultiplier;

    public double AppearChanceWeightingOptionMultiplier {get; set;} = 1d;

    public string Name => "Blast wave";

    public string Description =>
        "So which part is blasting and which part is shockwaving, exactly? So anyway, I started blasting.\nYour Knuckleblaster's shockwave comes out way faster.";

    [JsonIgnore]
    public Rarity Rarity
    {
        get => Rarity.Rare;
        set
        {
        }
    }
    [JsonIgnore]
    public Rarity MaxRarity => Rarity.Rare;

    public void Apply()
    {
        PatchKnuckleBlasterPunchStartSpeed.animationSpeedBonus += 2f;
    }

    public void Absolve()
    {
        PatchKnuckleBlasterPunchStartSpeed.animationSpeedBonus -= 2f;
    }
}

[HarmonyPatch(typeof(Punch), "BlastCheck")]
public class PatchKnuckleBlasterPunchStartSpeed
{
    public static float animationSpeedBonus = 1f;
    public static void Prefix(Punch __instance)
    {
        if (__instance.type == FistType.Heavy)
            __instance.anim.speed = animationSpeedBonus;
        else
            __instance.anim.speed = 1f;
    }
}


[HarmonyPatch(typeof(Punch), "BlastCheck")]
public class PatchKnuckleBlasterBlast
{
    public static void Postfix(Punch __instance)
    {
        __instance.anim.speed = PatchKnuckleBlasterPunchStartSpeed.animationSpeedBonus;
    }
}

