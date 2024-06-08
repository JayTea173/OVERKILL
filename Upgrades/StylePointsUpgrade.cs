using UnityEngine;

namespace OVERKILL.Upgrades;

public class StylePointsUpgrade : LeveledUpgrade, IRandomizable
{
    public override int MaxLevel => 10;

    public override double AppearChanceWeighting => 0.25d;

    public override string Name => "Style Up!";

    public override string Description => $"Increase the style points and XP you gain by {pct[Rarity]*level:0.%}. You're not actually cooler, though.";

    public override Rarity MaxRarity => Rarity.Overkill;

    public DoubleRarityValue pct;

    public override void Apply()
    {
        PlayerUpgradeStats.Instance.StylePointsMultiplier += pct[Rarity] * level;
    }

    public override void Absolve()
    {
        PlayerUpgradeStats.Instance.StylePointsMultiplier -= pct[Rarity] * level;
    }

    public void Randomize(int seed)
    {
        Random.InitState(seed);
        pct = new DoubleRarityValue(0.0);
        pct[Rarity.Uncommon] = 0.0;
        pct[Rarity.Rare] = 0.05;
        pct[Rarity.Epic] = 0.1;
        pct[Rarity.Overkill] = 0.2;

        var r = Random.value * RarityChances.Rare;

        Rarity = r switch
                 {
                     >= RarityChances.Uncommon => Rarity.Common,
                     >= RarityChances.Rare => Rarity.Uncommon,
                     >= RarityChances.Epic => Rarity.Rare,
                     >= RarityChances.Overkill / 2f => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
}
