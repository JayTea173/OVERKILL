using OVERKILL.UI.Upgrades;

namespace OVERKILL.Upgrades.Cybergrind;

public class RarityIncreaseUpgrade : LeveledUpgrade
{
    public override string Name => "Rarity Up!";

    public override string Description => $"Get those dirty commons out of here! No more {((Rarity)(level-1)).ToString().ColoredRTF(RarityColor.Get((Rarity)level-1))} show up. However, this also goes for the enemies. Waves spawn {GetSpawnChanceBonusPct():0.%} more special enemies.\n\nWarning! This might upgrade lock you completely out of certain upgrades!";

    public float GetSpawnChanceBonusPct()
    {
        switch (level)
        {
            case (int)Rarity.Uncommon:
                return 1f / RarityChances.Uncommon * 0.2f;
            case (int)Rarity.Rare:
                return 1f / RarityChances.Rare * 0.25f;
            case (int)Rarity.Epic:
                return 1f / RarityChances.Epic * 0.3f;
        }

        return 0f;
    }
    
    public override int MaxLevel => 3;

    public override Rarity Rarity => Rarity.Epic;

    public override Rarity MaxRarity => Rarity.Epic;

    public override bool IsObtainable => base.IsObtainable && EndlessGrid.Instance != null;

    public override double AppearChanceWeighting => RarityChances.Epic * 0.666d;

    public override void Apply()
    {
        PatchCybergrindEnemySpawning.specialEnemySpawnCostMultiplier += GetSpawnChanceBonusPct();
        UpgradeScreen.Instance.minUpgradeRarity += level;
    }

    public override void Absolve()
    {
        PatchCybergrindEnemySpawning.specialEnemySpawnCostMultiplier -= GetSpawnChanceBonusPct();
        UpgradeScreen.Instance.minUpgradeRarity -= level;
    }
}
