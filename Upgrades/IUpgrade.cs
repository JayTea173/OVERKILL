using System;
using Newtonsoft.Json;

namespace OVERKILL.Upgrades;

public interface IUpgrade : IEquatable <IUpgrade>
{
    public int OptionsSortPriority {get;}
    public double AppearChanceWeightingOptionMultiplier {get; set;}
    [JsonIgnore]
    public bool IsObtainable {get;}
    [JsonIgnore]
    public double AppearChanceWeighting {get;}
    public string Name {get;}
    public string Description {get;}
    [JsonIgnore]
    public Rarity Rarity {get; set;}
    [JsonIgnore]
    public Rarity MaxRarity {get;} 
    public void Apply();
    public void Absolve();
}

public interface IRandomizable
{
    public void Randomize(int seed);
}

public static class UpgradeExtensions
{
    public static string RTFName (this IUpgrade upgrade) => upgrade.Name.ColoredRTF(RarityColor.Get(upgrade.Rarity));
}
