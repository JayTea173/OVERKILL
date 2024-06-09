using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using Newtonsoft.Json;
using Sandbox;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OVERKILL.Upgrades.Cybergrind;

public class SpawnBossUpgrade : LeveledUpgrade, IRandomizable
{
    public override int OptionsSortPriority => 100;

    public override string Name => "INVASION: " + (enemyType.HasValue ? enemyType.ToString().ToUpper() : "[BOSS ENEMY]");

    public override string Description
    {
        get
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"Increase your style points and XP gained by {GetStyleBonusPerLevel() * level:0.%}.\n");
            
            
            sb.AppendLine($"{enemyType.Value} can now spawn as a special enemy.");
            
            var e = GetSpawnEntry(enemyType.Value, level);

            if (e.numMax == 1)
            {
                sb.AppendLine($"1 enemy per encounter.");
            }
            else
            {
                sb.AppendLine($"between {e.numMin} and {e.numMax} enemies per encounter.");
            }


            var currWave = EndlessGrid.Instance.currentWave;

            int w = e.waveStart;

            int i = 0;
            while (w < currWave)
            {
                w += e.waveIntervals[i % e.waveIntervals.Length];
            
                i++;
            }

            int[] spawns = new int[e.waveIntervals.Length];

            for (int j = 0; j < e.waveIntervals.Length; j++)
            {
                spawns[j] = w;
                w += e.waveIntervals[(i + j) % e.waveIntervals.Length];
            }

            sb.AppendLine($"They spawn at waves [{string.Join(", ", spawns)}], repeating.");

            if (e.radiantChance > 0f)
                sb.AppendLine($"They have a {e.radiantChance:0.%} chance to be radiant.");

            return sb.ToString();
        }
    }

    public override Rarity Rarity => Rarity.Epic;
    public override Rarity MaxRarity => Rarity.Epic;

    public override double AppearChanceWeighting => RarityChances.Epic * 4d * AppearChanceWeightingOptionMultiplier;

    public override bool IsObtainable => base.IsObtainable && EndlessGrid.Instance != null;

    public EnemyType? enemyType;

    [JsonIgnore]
    private EndlessEnemy _endlessEnemy;

    public override int MaxLevel => GetMaxLevel();

    private int GetMaxLevel()
    {
        return enemyType switch
               {
                   EnemyType.Gabriel => 3,
                   EnemyType.GabrielSecond => 3,
                   EnemyType.V2 => 4,
                   EnemyType.MinosPrime => 2,
                   EnemyType.SisyphusPrime => 2,
                   EnemyType.Mannequin => 3,
                   _ => 1
               };
    }

    public static GameObject GetEnemyPrefab(EnemyType enemyType)
    {
        var objectsField = typeof(SandboxSaver).GetField("objects", BindingFlags.Instance | BindingFlags.NonPublic);
        var objects = (SpawnableObjectsDatabase)objectsField.GetValue(SandboxSaver.Instance);

        EnemyIdentifier eidFound = null;
        //find that mofo
        foreach (var o in objects.enemies)
        {
            if (!o.gameObject.TryGetComponent(out EnemyIdentifier eid))
                continue;

            if (eid.enemyType == enemyType)
            {
                eidFound = eid;

                break;
            }
        }

        if (eidFound == null)
        {
            OK.Log($"Failed to find prefab for enemy type: {enemyType}!", LogLevel.Error);
            return null;
        }

        return eidFound?.gameObject;
    }

    public double GetStyleBonusPerLevel()
    {
        return enemyType switch
               {
                   EnemyType.Gabriel => 0.1,
                   EnemyType.GabrielSecond => 0.12,
                   EnemyType.V2 => 0.05,
                   EnemyType.MinosPrime => 0.15,
                   EnemyType.SisyphusPrime => 0.15,
                   EnemyType.Leviathan => 0.1,
                   EnemyType.Mindflayer => 0.25,
                   EnemyType.Mannequin => 0.1,
                   _ => 0.05
               };
    }

    public static CybergrindCustomSpawns.Entry GetSpawnEntry(EnemyType enemyType, int level)
    {
        var e =  new CybergrindCustomSpawns.Entry()
        {
            numMin = 1,
            numMax = 1,
            spawnsInRangedPosition = false,
            waveStart = 0
        };

        switch (enemyType)
        {
            case EnemyType.V2:
                e.numMin = level;
                e.numMax = Math.Min(10, level * level);
                e.waveIntervals = new[] {3, 5};
                e.radiantChance = level < 4 ? 0 : 0.1f;
                e.waveStart = 2;
                break;
            case EnemyType.SisyphusPrime:
                e.waveIntervals = new[] {11, 13, 11};
                e.radiantChance = level < 2 ? 0 : 0.25f;
                e.waveStart = 15;
                break;
            case EnemyType.MinosPrime:
                e.waveIntervals = new[] {9, 11, 13};
                e.radiantChance = level < 2 ? 0 : 0.35f;
                e.waveStart = 13;
                break;
            case EnemyType.Gabriel:
                e.numMax = Math.Max(1, (level + 1) / 2);
                e.waveIntervals = new[] {7, 11};
                e.radiantChance = (level - 1) * 0.25f;
                e.waveStart = 2;
                break;
            case EnemyType.GabrielSecond:
                e.numMin = 1;
                e.numMax = Math.Max(1, level);
                e.waveIntervals = new[] {13, 5};
                e.radiantChance = 0.05f;
                e.waveStart = 7;
                break;
            case EnemyType.Mindflayer:
                e.numMin = 2;
                e.numMax = 4;
                e.waveIntervals = new[] {13};
                e.radiantChance = 1f;
                e.waveStart = 1;
                break;
            case EnemyType.Mannequin:
                e.numMin = (int)(8 * (level * level / 2f));
                e.numMax = e.numMin + 8;
                e.waveIntervals = new[] {11};
                e.radiantChance = 0f;
                e.waveStart = -1;
                break;
            
            default:
                e.waveIntervals = new[] {10, 11, 9};

                break;
        }

        return e;
    }

    public static EndlessEnemy CreateEndlessEnemy(EnemyType enemyType)
    {
        if (EndlessGrid.Instance == null)
            return null;
        var prefabs = (PrefabDatabase)PatchCybergrindEnemySpawning.prefabsField.GetValue(EndlessGrid.Instance);
        
        var endlessEnemy = ScriptableObject.CreateInstance <EndlessEnemy>();
        endlessEnemy.prefab = GetEnemyPrefab(enemyType);

        if (endlessEnemy.prefab == null)
            return null;

        endlessEnemy.enemyType = enemyType;
        endlessEnemy.spawnCost = (int)(prefabs.specialEnemies[0].spawnCost * 1.5f);
        endlessEnemy.spawnWave = 0;
        endlessEnemy.costIncreasePerSpawn = endlessEnemy.spawnCost / 2;

        return endlessEnemy;

    }

    public override void Apply()
    {
        var prefabs = (PrefabDatabase)PatchCybergrindEnemySpawning.prefabsField.GetValue(EndlessGrid.Instance);

        _endlessEnemy = CreateEndlessEnemy(enemyType.Value);

        Array.Resize(ref prefabs.specialEnemies, prefabs.specialEnemies.Length + 1);
        prefabs.specialEnemies[prefabs.specialEnemies.Length - 1] = _endlessEnemy;

        PlayerUpgradeStats.Instance.StylePointsMultiplier += GetStyleBonusPerLevel() * level;
        
        PatchCybergrindEnemySpawning.customSpawns.customSpawns.Add(_endlessEnemy, GetSpawnEntry(enemyType.Value, level));
        
        //OK.Log($"{enemyType} spawn available! Cost: {e.spawnCost}, prefabs now {prefabs.specialEnemies.Length}:\n{string.Join(", ", prefabs.specialEnemies.Select(e0 => e0.prefab.name))}");
    }

    public override void Absolve()
    {
        var prefabs = (PrefabDatabase)PatchCybergrindEnemySpawning.prefabsField.GetValue(EndlessGrid.Instance);

        var asList = prefabs.specialEnemies.ToList();
        asList.RemoveAll(e => e.enemyType == enemyType);

        prefabs.uncommonEnemies = asList.ToArray();

        PatchCybergrindEnemySpawning.customSpawns.customSpawns.Remove(_endlessEnemy);

        PlayerUpgradeStats.Instance.StylePointsMultiplier -= GetStyleBonusPerLevel() * level;
    }

    public void Randomize(int seed)
    {
        var prefabs = (PrefabDatabase)PatchCybergrindEnemySpawning.prefabsField.GetValue(EndlessGrid.Instance);

        var rnd = new System.Random(UnityEngine.Random.Range(int.MinValue, int.MaxValue));

        HashSet <EnemyType> existing = new HashSet <EnemyType>();

        
        foreach (var enemy in prefabs.meleeEnemies)
            existing.Add(enemy.enemyType);

        foreach (var enemy in prefabs.projectileEnemies)
            existing.Add(enemy.enemyType);

        foreach (var enemy in prefabs.uncommonEnemies)
            existing.Add(enemy.enemyType);
        
        foreach (var enemy in prefabs.specialEnemies)
            existing.Add(enemy.enemyType);
        
        WeightedRandom <EnemyType> enemyRnd = new WeightedRandom <EnemyType>(rnd);

        if (!existing.Contains(EnemyType.V2))
            enemyRnd.AddEntry(EnemyType.V2, new FixedRandomWeight(.75d));

        if (!existing.Contains(EnemyType.Gabriel))
            enemyRnd.AddEntry(EnemyType.Gabriel, new FixedRandomWeight(0.24d));
        
        if (!existing.Contains(EnemyType.MinosPrime))
            enemyRnd.AddEntry(EnemyType.MinosPrime, new FixedRandomWeight(0.08d));
        
        if (!existing.Contains(EnemyType.SisyphusPrime))
            enemyRnd.AddEntry(EnemyType.SisyphusPrime, new FixedRandomWeight(0.07d));
        
        if (!existing.Contains(EnemyType.Leviathan))
            enemyRnd.AddEntry(EnemyType.Leviathan, new FixedRandomWeight(0.05d));
        
        if (!existing.Contains(EnemyType.Mindflayer))
            enemyRnd.AddEntry(EnemyType.Mindflayer, new FixedRandomWeight(0.06d));
        
        if (!existing.Contains(EnemyType.Mannequin))
            enemyRnd.AddEntry(EnemyType.Mannequin, new FixedRandomWeight(0.04d));

        //prefab not found?
        /*
        if (!existing.Contains(EnemyType.Mandalore))
            enemyRnd.AddEntry(EnemyType.Mandalore, new FixedRandomWeight(0.2d)); 
         
        if (!existing.Contains(EnemyType.BigJohnator))
            enemyRnd.AddEntry(EnemyType.BigJohnator, new FixedRandomWeight(0.16d));
            
        if (!existing.Contains(EnemyType.VeryCancerousRodent))
            enemyRnd.AddEntry(EnemyType.VeryCancerousRodent, new FixedRandomWeight(0.28d));
            
        if (!existing.Contains(EnemyType.CancerousRodent))
            enemyRnd.AddEntry(EnemyType.CancerousRodent, new FixedRandomWeight(0.38d));
            */
        
       
        
        if (!existing.Contains(EnemyType.GabrielSecond))
            enemyRnd.AddEntry(EnemyType.GabrielSecond, new FixedRandomWeight(0.22d));

        if (enemyRnd.weightSum <= 0d)
            return; //dunno how to handle the case where all bosses are unlocked.
        
        this.enemyType = enemyRnd.Get();
    }
}
