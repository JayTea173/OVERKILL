using System;
using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using OVERKILL.Upgrades.Cybergrind;

namespace OVERKILL.Upgrades;

public class FriendUpgrade : LeveledUpgrade, IRandomizable
{
    public override string Name => "FRIENDSHIP (" + (enemyType.HasValue ? enemyType.ToString().ToUpper() : "[TYPE]") + ")";
    
    public override double AppearChanceWeighting => RarityChances.Overkill * 2d * AppearChanceWeightingOptionMultiplier;

    public override int MaxLevel => 3;

    public override int OptionsSortPriority => 999;
    
    private EndlessEnemy _endlessEnemy;

    public override string Description
    {
        get
        {
            StringBuilder sb = new StringBuilder();
            
            sb.AppendLine($"Increase your style points and XP gained by {GetStyleBonusPerLevel() * level:0.%}.\n");
            
            
            sb.AppendLine($"<b>{enemyType.Value}</b> can now spawn as your <color=green>friend</color>, but will always arrive 6 seconds late to the wave.");
            
            var e = GetSpawnEntry(enemyType.Value, level);

            if (e.numMax == 1)
            {
                sb.AppendLine($"1 friend per encounter.");
            }
            else
            {
                sb.AppendLine($"between {e.numMin} and {e.numMax} friends per encounter.");
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
    
    public override Rarity Rarity => Rarity.Overkill;
    public override Rarity MaxRarity => Rarity.Overkill;

    public override bool IsObtainable => base.IsObtainable && EndlessGrid.Instance != null;

    public EnemyType? enemyType;
    
    public double GetStyleBonusPerLevel()
    {
        return enemyType switch
               {
                   EnemyType.Gabriel => 0.1,
                   EnemyType.GabrielSecond => 0.1,
                   EnemyType.V2 => 0.05,
                   EnemyType.MinosPrime => 0.15,
                   EnemyType.SisyphusPrime => 0.15,
                   EnemyType.Leviathan => 0.1,
                   EnemyType.Mindflayer => 0.1,
                   EnemyType.Mannequin => 0.05,
                   _ => 0.05
               } * 3d;
    }
    
     public static CybergrindCustomSpawns.Entry GetSpawnEntry(EnemyType enemyType, int level)
    {
        var e =  new CybergrindCustomSpawns.Entry()
        {
            numMin = 1,
            numMax = 1,
            spawnsInRangedPosition = false,
            waveIntervals = new[]{1},
            waveStart = 0,
            radiantChance = .05f + (level - 1) * 0.35f
            
        };
        

        switch (enemyType)
        {
            case EnemyType.V2:
                e.numMin = 1;
                e.numMax = 1;
                //e.waveIntervals = new[] {3, 5};
                //e.radiantChance = level < 4 ? 0 : 0.1f;
                //e.waveStart = 0;
                e.waveIntervals = new[] {2};
                break;
            case EnemyType.SisyphusPrime:
                e.numMin = 1;
                e.numMax = 1;
                e.waveIntervals = new[] {2, 6};
                //e.radiantChance = level < 2 ? 0 : 0.25f;
                //e.waveStart = 0;
                break;
            case EnemyType.MinosPrime:
                e.numMin = 1;
                e.numMax = 1;
                e.waveIntervals = new[] {3, 5};
                //e.waveIntervals = new[] {9, 11, 13};
                //e.radiantChance = level < 2 ? 0 : 0.35f;
                //e.waveStart = 0;
                break;
            case EnemyType.Gabriel:
                e.numMin = 1;
                e.numMax = 1;
                //e.waveIntervals = new[] {7, 11};
                //e.radiantChance = (level - 1) * 0.25f;
                //e.waveStart = 0;
                break;
            case EnemyType.GabrielSecond:
                e.numMin = 1;
                e.numMax = 1;
                e.waveIntervals = new[] {2};
                //e.waveIntervals = new[] {13, 5};
                //e.radiantChance = 0.05f;
                e.waveStart = 1;
                break;
            case EnemyType.Mindflayer:
                e.numMin = 1;
                e.numMax = 1;
                //e.waveIntervals = new[] {13};
                //e.radiantChance = 1f;
                //e.waveStart = 0;
                break;
            case EnemyType.Mannequin:
                e.numMin = 2;
                e.numMax = 3;
                //e.waveIntervals = new[] {11};
                //e.radiantChance = 0f;
                //e.waveStart = -1;
                break;
            case EnemyType.Schism:
                e.numMin = 3;
                e.numMax = 6;
                //e.waveIntervals = new[] {11};
                //e.radiantChance = 0f;
                //e.waveStart = -1;
                break;
            case EnemyType.Drone:
                e.numMin = 4;
                e.numMax = 10;
                //e.waveIntervals = new[] {11};
                //e.radiantChance = 0f;
                //e.waveStart = -1;
                break;
            
            default:
                //e.waveIntervals = new[] {10, 11, 9};

                break;
        }
        

        return e;
    }

     public override void Apply()
     {

         _endlessEnemy = SpawnBossUpgrade.CreateEndlessEnemy(enemyType.Value);

         PatchCybergrindEnemySpawning.customSpawns.friendlySpawns.Add(_endlessEnemy, GetSpawnEntry(enemyType.Value, level));
         
         PlayerUpgradeStats.Instance.StylePointsMultiplier += GetStyleBonusPerLevel() * level;
        
         //OK.Log($"{enemyType} spawn available! Cost: {e.spawnCost}, prefabs now {prefabs.specialEnemies.Length}:\n{string.Join(", ", prefabs.specialEnemies.Select(e0 => e0.prefab.name))}");
     }

     public override void Absolve()
     {
         
         PatchCybergrindEnemySpawning.customSpawns.friendlySpawns.Remove(_endlessEnemy);

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
            enemyRnd.AddEntry(EnemyType.Mindflayer, new FixedRandomWeight(0.15d));
        
        if (!existing.Contains(EnemyType.Mannequin))
            enemyRnd.AddEntry(EnemyType.Mannequin, new FixedRandomWeight(0.20d));
        
        if (!existing.Contains(EnemyType.Schism))
            enemyRnd.AddEntry(EnemyType.Schism, new FixedRandomWeight(1.8d));
        if (!existing.Contains(EnemyType.Drone))
            enemyRnd.AddEntry(EnemyType.Drone, new FixedRandomWeight(1.8d));

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

[HarmonyPatch]
public static class FriendPatches //:)
{
    [HarmonyPatch(typeof(Gabriel), "Update")]
    public static void GabrielDestroyedLagFix()
    {
        
    }
}
