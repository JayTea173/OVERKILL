using Newtonsoft.Json;
using OVERKILL.UI.Upgrades;
using UnityEngine;

namespace OVERKILL.Upgrades.Cybergrind;


public class ExtraChoicesUpgrade : LeveledUpgrade, IRandomizable
{
    public override string Name => "Extra Choices";

    public override int MaxLevel => 1;

    public override double AppearChanceWeighting => RarityChances.Epic;

    public override string Description =>
        $"Increases the number of upgrades offered by {(int)Rarity - 2}, but increases the number of radiant enemies and lowers wave threshold at which they start spawning by 10. All enemies can now spawn regardless of their starting wave. No holds barred!";

    [JsonProperty]
    private Rarity rarity;
    [JsonIgnore]
    public override Rarity Rarity => rarity;

    public override bool IsObtainable => base.IsObtainable && EndlessGrid.Instance != null;

    public override void Apply()
    {
        UpgradeScreen.numExtraChoices += (int)Rarity - 2;
        PatchCybergrindEnemySpawning.waveThresholdRadiantSpawnBonus -= 10;
        PatchCybergrindEnemySpawning.waveThresholdSpawnBonus -= 100;
    }

    public override void Absolve()
    {
        UpgradeScreen.numExtraChoices -= (int)Rarity - 2;
        PatchCybergrindEnemySpawning.waveThresholdRadiantSpawnBonus += 10;
        PatchCybergrindEnemySpawning.waveThresholdSpawnBonus += 100;
    }

    public void Randomize(int seed)
    {
        var r = Random.value * RarityChances.Epic;

        rarity = r switch
                 {
                     >= RarityChances.Overkill => Rarity.Epic,
                     _ => Rarity.Overkill
                 };
    }
}
