using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OVERKILL.UI;
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
    public Dictionary <EndlessEnemy, Entry> friendlySpawns = new Dictionary <EndlessEnemy, Entry>();

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

    public int DoCustomSpawns(
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

            for (int i = 0; i < num; i++)
            {
                if (kv.Value.spawnsInRangedPosition)
                    spawnPosition = projectilePositions[UnityEngine.Random.Range(0, meleePositions.Count)];
                else
                    spawnPosition = meleePositions[UnityEngine.Random.Range(0, meleePositions.Count)];

                //OK.Log($"Custom spawn: {num} {kv.Key.prefab.name} @{spawnPosition}");

                var go = (GameObject)PatchCybergrindEnemySpawning.spawnOnGridMethod.Invoke(
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

                if (go != null)
                {

                    ++v.usedMeleePositions;
                    ++endlessGrid.tempEnemyAmount;
                    
                    go.GetComponent<EnemyIdentifier>().onDeath.AddListener(
                        () =>
                        {
                            OK.Log("ONDEATH!");
                            go.GetComponentInParent<ActivateNextWave>().AddDeadEnemy();
                        });

                    if (num == 1)
                        PatchCybergrindEnemySpawning.DisplayInvasionText(
                            $"You have been invaded by <color=red>{kv.Key.prefab.name}</color>!");
                    else
                        PatchCybergrindEnemySpawning.DisplayInvasionText(
                            $"You have been invaded by <color=red>the {kv.Key.prefab.name} gang</color>!");

                    //Options.DumpHierarchy(go.transform);
                    numSpawned++;
                }
            }


            var indexOfEnemyType = spawnedEnemyTypes.FindIndex(tracker => tracker.type == kv.Key.enemyType);
            if (indexOfEnemyType < 0)
                spawnedEnemyTypes.Add(new EnemyTypeTracker(kv.Key.enemyType){ amount = num });
            else
                spawnedEnemyTypes[indexOfEnemyType].amount += num;

        }

        return numSpawned;
    }

    private static Dictionary <EndlessEnemy, HashSet<EnemyIdentifier>> activeFriendliesForEntry = new Dictionary <EndlessEnemy, HashSet<EnemyIdentifier>>();
    private static Coroutine friendliesSpawnCoroutine;

    private void CleanupFriendlies(EndlessGrid endlessGrid)
    {
        foreach (var kv in activeFriendliesForEntry)
        {
            foreach (var eid in kv.Value)
            {
                if (eid != null)
                    UnityEngine.Object.Destroy(eid);
            }
        }
        activeFriendliesForEntry.Clear();
        if (friendliesSpawnCoroutine != null)
            endlessGrid.StopCoroutine(friendliesSpawnCoroutine);
    }

     public void DoFriendlySpawns(
        EndlessGrid endlessGrid,
        List <Vector2> meleePositions,
        List <Vector2> projectilePositions,
        ref PatchCybergrindEnemySpawning.ReflectedValueTypes v,
        List <EnemyTypeTracker> spawnedEnemyTypes)
     {
         CleanupFriendlies(endlessGrid);
         
         if (friendliesSpawnCoroutine != null)
             endlessGrid.StopCoroutine(friendliesSpawnCoroutine);

         friendliesSpawnCoroutine = endlessGrid.StartCoroutine(
             DoFriendlySpawnsCoroutine(endlessGrid, meleePositions, projectilePositions, spawnedEnemyTypes));
     }
     
     public IEnumerator DoFriendlySpawnsCoroutine(
        EndlessGrid endlessGrid,
        List <Vector2> meleePositions,
        List <Vector2> projectilePositions,
        List <EnemyTypeTracker> spawnedEnemyTypes)
     {
         yield return new WaitForSeconds(6f);
        //while (NewMovement.Instance.gc.touchingGround)
        int numSpawned = 0;
        
        CleanupFriendlies(endlessGrid);

        StringBuilder sb = new StringBuilder();
        sb.Append("\n\n");
        
        foreach (var kv in friendlySpawns)
        {
            if (!SpawnsAt(endlessGrid.currentWave, kv.Value))
                continue;

            var num = kv.Value.GetNumSpawned();

            if (num <= 0)
                continue;
            
            

            Vector2 spawnPosition = default;

            for (int i = 0; i < num; i++)
            {
                if (kv.Value.spawnsInRangedPosition)
                    spawnPosition = projectilePositions[UnityEngine.Random.Range(0, meleePositions.Count)];
                else
                    spawnPosition = meleePositions[UnityEngine.Random.Range(0, meleePositions.Count)];

                //OK.Log($"Custom spawn: {num} {kv.Key.prefab.name} @{spawnPosition}");
                
                
                var go = (GameObject)PatchCybergrindEnemySpawning.spawnOnGridMethod.Invoke(
                    endlessGrid,
                    new object[]
                    {
                        kv.Key.prefab,
                        spawnPosition,
                        false, //prefab
                        false,  //enemy
                        CyberPooledType.None,
                        UnityEngine.Random.value < kv.Value.radiantChance
                    });

                if (go != null)
                {
                    var eid = go.GetComponent <EnemyIdentifier>();

                    eid.ignorePlayer = true;
                    eid.attackEnemies = true;

                    if (!activeFriendliesForEntry.TryGetValue(kv.Key, out var hs))
                    {
                        hs = new HashSet <EnemyIdentifier>();
                        activeFriendliesForEntry.Add(kv.Key, hs);
                    }

                    hs.Add(eid);

                    //Options.DumpHierarchy(go.transform);
                    numSpawned++;
                }
            }


            var indexOfEnemyType = spawnedEnemyTypes.FindIndex(tracker => tracker.type == kv.Key.enemyType);
            if (indexOfEnemyType < 0)
                spawnedEnemyTypes.Add(new EnemyTypeTracker(kv.Key.enemyType){ amount = num });
            else
                spawnedEnemyTypes[indexOfEnemyType].amount += num;

            if (num > 0)
            {
                GetFriendlyJoinMessage(sb, kv);
            }

        }

        if (numSpawned > 0)
        {
            PatchCybergrindEnemySpawning.DisplayInvasionText(sb.ToString());
        }

        yield break;
    }


     private static readonly string[] friendlyJoinFormats = new[]
     {
         "<color=green>{0}</color> is here to kick some ass!",
         "<color=green>{0}</color> came for your birthday party!",
         "What in the fuck is <color=green>{0}</color> doing here?",
         "<color=green>{0}</color> is here to help you out!",
         "<color=green>{0}</color> seems awfully helpful today!",
         "<color=green>{0}</color> has joined the V1 army!",
         "Do you smell that? It's <color=green>{0}</color>!",
         "<color=green>{0}</color> is the main character now!",
     };
     private static void GetFriendlyJoinMessage(StringBuilder sb, KeyValuePair <EndlessEnemy, Entry> kv)
     {
         int r = UnityEngine.Random.Range(0, friendlyJoinFormats.Length);
         sb.AppendLine(string.Format(friendlyJoinFormats[r], kv.Key.enemyType.ToString()));
     }
}
