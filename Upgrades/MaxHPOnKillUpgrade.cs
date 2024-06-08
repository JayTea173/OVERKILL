using Newtonsoft.Json;
using UnityEngine;

namespace OVERKILL.Upgrades;

public class MaxHPOnKillUpgrade : IUpgrade, IRandomizable
{
    public int OptionsSortPriority => 0;

    public bool IsObtainable => !PlayerUpgradeStats.Instance.upgrades.ContainsKey(this.GetHashCode());

    public double AppearChanceWeighting => 0.6d * RarityChances.Rare * AppearChanceWeightingOptionMultiplier;

    public double AppearChanceWeightingOptionMultiplier {get; set;} = 1d;

    public string Name => "OVERHEALTH";

    public string Description => $"Whenever killing an enemy, increase your maximum HP by {pctHP[Rarity] / 10d:0.0%} of that enemy's maximum HP (Overkill HP values, which are 10x base game to get rid of some decimals). After gaining 50 HP from this upgrade, the rate at which you gain more slows down.";

    [JsonProperty]
    private Rarity rarity;

    [JsonIgnore]
    public Rarity Rarity
    {
        get => rarity;
        set => rarity = value;
    }

    public Rarity MaxRarity => Rarity.Overkill;

    [JsonIgnore]
    public DoubleRarityValue pctHP => new(0d, 0.04d, 0.07d, 0.1d);

    public void Apply()
    {
        
        PlayerUpgradeStats.Instance.PctMaxHPGainOnKill += pctHP[Rarity];
    }

    public void Absolve()
    {
        PlayerUpgradeStats.Instance.PctMaxHPGainOnKill -= pctHP[Rarity];
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);

        var r = Random.value * RarityChances.Rare;

        rarity = r switch
                 {
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }

    public bool Equals(IUpgrade other)
    {
        return Name.Equals(other.Name);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
