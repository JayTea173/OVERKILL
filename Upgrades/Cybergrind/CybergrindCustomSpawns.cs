using System.Collections.Generic;
using UnityEngine;

namespace OVERKILL.Upgrades.Cybergrind;

public class CybergrindCustomSpawns
{
    public class Entry
    {
        public int numMin = 1;
        public int numMax = 1;
        public int waveStart = 0;
        public bool spawnsInRangedPosition;
        public float radiantChance;
        public int[] waveIntervals;

        public int GetNumSpawned()
        {
            if (numMax <= numMin)
                return numMin;

            return UnityEngine.Random.Range(numMin, numMax);
        }
    }

    public Dictionary <EndlessEnemy, Entry> customSpawns = new Dictionary <EndlessEnemy, Entry>();

    public bool SpawnsAt(int currWave, Entry e)
    {
        var w = e.waveStart;

        if (currWave == w)
            return true;

        int i = 0;
        while (w < currWave)
        {
            w += e.waveIntervals[i % e.waveIntervals.Length];
            
            i++;
            
            if (currWave == w)
                return true;
        }

        return false;
    }

    public int DoSpawns(
        EndlessGrid endlessGrid,
        List <Vector2> meleePositions,
        List <Vector2> projectilePositions,
        ref PatchCybergrindEnemySpawning.ReflectedValueTypes v,
        List <EnemyTypeTracker> spawnedEnemyTypes)
    {
        int numSpawned = 0;
        
        foreach (var kv in customSpawns)
        {
            if (!SpawnsAt(endlessGrid.currentWave, kv.Value))
                continue;

            var num = kv.Value.GetNumSpawned();

            if (num <= 0)
                continue;
            
            

            Vector2 spawnPosition = default;

            if (kv.Value.spawnsInRangedPosition)
                spawnPosition = projectilePositions[UnityEngine.Random.Range(0, meleePositions.Count)];
            else
                spawnPosition = meleePositions[UnityEngine.Random.Range(0, meleePositions.Count)];
            
            //OK.Log($"Custom spawn: {num} {kv.Key.prefab.name} @{spawnPosition}");
            
            PatchCybergrindEnemySpawning.spawnOnGridMethod.Invoke(
                endlessGrid,
                new object[]
                {
                    kv.Key.prefab,
                    spawnPosition,
                    false, //prefab
                    true,  //enemy
                    CyberPooledType.None,
                    UnityEngine.Random.value < kv.Value.radiantChance
                });
            
            //++spawnedEnemyTypes[indexOfEnemyType].amount;
            //++v.usedMeleePositions;
            //++endlessGrid.tempEnemyAmount;

            if (num == 1)
                PatchCybergrindEnemySpawning.DisplayInvasionText($"You have been invaded by <color=red>{kv.Key.prefab.name}</color>!");
            else
                PatchCybergrindEnemySpawning.DisplayInvasionText($"You have been invaded by <color=red>the {kv.Key.prefab.name} gang</color>!");

            numSpawned++;

        }

        return numSpawned;
    }
}
