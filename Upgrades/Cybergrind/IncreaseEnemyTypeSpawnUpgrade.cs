using Newtonsoft.Json;
using OVERKILL.JSON;
using UnityEngine;

namespace OVERKILL.Upgrades.Cybergrind;

public class IncreaseEnemyTypeSpawnUpgrade : LeveledUpgrade, IRandomizable
{
    public override string Name => $"More {(type != null ? type.prefab.name : "[ENEMY TYPE]")}!";

    public override string Description => $"Decreases the spawn cost of {type.prefab.name} enemies by {GetSpawnCostMultiplier():0.%}. You gain {level*(double)Rarity*.05d:0.%} (additive) more style points and XP.";

    public double GetSpawnCostMultiplier()
    {
        switch (level)
        {
            case 1:
                return .5d;
            case 2:
                return .4d;
            case 3:
                return .3d;
            case 4:
                return .2d;
            case 5:
                return .1d;
        }

        return 1d;
    }
    [JsonIgnore]
    public override Rarity MaxRarity => Rarity.Common; //disable rarity upgrades for this thing
    
    [JsonConverter(typeof(EndlessEnemyConverter))]
    public EndlessEnemy type;
    
    public override bool IsObtainable => base.IsObtainable && EndlessGrid.Instance != null;
    [JsonIgnore]
    public override int MaxLevel => 5;

    public override double AppearChanceWeighting => RarityChances.Uncommon * 0.85d;
    
    public override int GetHashCode()
    {
        unchecked
        {
            if (type == null)
                return (typeof(IncreaseEnemyTypeSpawnUpgrade).Name.GetHashCode() * 397);
            
            return (typeof(IncreaseEnemyTypeSpawnUpgrade).Name.GetHashCode() * 397) ^ type.GetHashCode();
        }
    }

    public override void Apply()
    {
        PlayerUpgradeStats.Instance.StylePointsMultiplier += level * (double)Rarity * .05d;

        if (!PatchCybergrindEnemySpawning.specificEnemySpawnCostMultipliers.TryGetValue(type, out double value))
        {
            PatchCybergrindEnemySpawning.specificEnemySpawnCostMultipliers.Add(type, GetSpawnCostMultiplier());
        }
        else
        {
            PatchCybergrindEnemySpawning.specificEnemySpawnCostMultipliers[type] += GetSpawnCostMultiplier();
        }
    }

    public override void Absolve()
    {
        PlayerUpgradeStats.Instance.StylePointsMultiplier -= level * (double)Rarity * .05d;
        
        if (PatchCybergrindEnemySpawning.specificEnemySpawnCostMultipliers.TryGetValue(type, out double value))
        {
            PatchCybergrindEnemySpawning.specificEnemySpawnCostMultipliers[type] -= GetSpawnCostMultiplier();
        }
    }

    public class EnemyWithRarity
    {
        public EndlessEnemy enemy;
        public Rarity rarity;
    }

    public void Randomize(int seed)
    {
        var prefabs = (PrefabDatabase)PatchCybergrindEnemySpawning.prefabsField.GetValue(EndlessGrid.Instance);

        var rnd = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        WeightedRandom <EnemyWithRarity> enemyRnd = new WeightedRandom <EnemyWithRarity>(rnd);

        foreach (var enemy in prefabs.meleeEnemies)
        {
            enemyRnd.AddEntry(new EnemyWithRarity(){enemy = enemy, rarity = Rarity.Uncommon}, new FixedRandomWeight(1.5d / enemy.spawnCost));
        }
        
        foreach (var enemy in prefabs.projectileEnemies)
        {
            enemyRnd.AddEntry(new EnemyWithRarity(){enemy = enemy, rarity = Rarity.Uncommon}, new FixedRandomWeight(1.5d / enemy.spawnCost));
        }
        
        foreach (var enemy in prefabs.uncommonEnemies)
        {
            enemyRnd.AddEntry(new EnemyWithRarity(){enemy = enemy, rarity = Rarity.Rare}, new FixedRandomWeight(2.0d / enemy.spawnCost));
        }
        
        foreach (var enemy in prefabs.specialEnemies)
        {
            enemyRnd.AddEntry(new EnemyWithRarity(){enemy = enemy, rarity = Rarity.Epic}, new FixedRandomWeight(3.0d / enemy.spawnCost));
        }


        var enemyPicked = enemyRnd.Get();

        type = enemyPicked.enemy;
        Rarity = enemyPicked.rarity;
    }
}
