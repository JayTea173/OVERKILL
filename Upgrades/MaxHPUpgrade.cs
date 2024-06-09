using Newtonsoft.Json;
using UnityEngine;

namespace OVERKILL.Upgrades;

public sealed class MaxHPUpgrade : LeveledUpgrade, IRandomizable
{
    public override int MaxLevel => 10;

    public override double AppearChanceWeighting => 0.8d * AppearChanceWeightingOptionMultiplier;

    public override string Name => "Max HP Up";

    public override string Description => $"Increases your maximum HP by {flatBonus[Rarity]*level}";
    
    public override Rarity MaxRarity => Rarity.Epic;

    public LongRarityValue flatBonus;

    public override void Apply()
    {
        PlayerUpgradeStats.Instance.HPBonusFlat += flatBonus[Rarity] * level;
    }

    public override void Absolve()
    {
        PlayerUpgradeStats.Instance.HPBonusFlat -= flatBonus[Rarity] * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        flatBonus = new LongRarityValue(0);
        flatBonus[Rarity.Uncommon] = 5;
        flatBonus[Rarity.Rare] = 10;
        flatBonus[Rarity.Epic] = 20;
        flatBonus[Rarity.Overkill] = 0;

        var r = Random.value * RarityChances.Uncommon;

        Rarity = r switch
                 {
                     >= RarityChances.Uncommon => Rarity.Common,
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Epic
                 };
    }
}
