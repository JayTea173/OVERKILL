using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameConsole.pcon;
using HarmonyLib;
using JetBrains.Annotations;
using OVERKILL.UI.Upgrades;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OVERKILL.Upgrades.Cybergrind;


public enum EnemySpawnRarity
{
    Common,
    Uncommon,
    Special
}


/// <summary>
///     **Get**Enemies? Do I need to explain what Get usually means? Obviously not, since we're using so many properties
///     throughout
///     ultrakill's codebase :)<br></br><br></br>
///     also, this only really spawns the uncommons and specials, then calls GetNextEnemy, which actually spawns all the
///     stuff before round 12.
/// </summary>
[HarmonyPatch(typeof(EndlessGrid), "GetEnemies")]
public class PatchCybergrindEnemySpawning
{
    /// <summary>
    /// +10 is 10 waves later, -10 is 10 waves earlier.
    /// </summary>
    public static int waveThresholdRadiantSpawnBonus = 0;
    
    /// <summary>
    /// +10 is 10 waves later, -10 is 10 waves earlier. For all enemies.
    /// </summary>
    public static int waveThresholdSpawnBonus = 0;

    public static float uncommonEnemySpawnCostMultiplier = 1f;
    public static float specialEnemySpawnCostMultiplier = 1f;

    public static Dictionary <EndlessEnemy, double> specificEnemySpawnCostMultipliers = new Dictionary <EndlessEnemy, double>();

    public static CybergrindCustomSpawns customSpawns = new CybergrindCustomSpawns();

    public static TMP_Text invasionText;

    public struct ReflectedValueTypes
    {
        public double points;
        public int usedMeleePositions;
        public int usedProjectilePositions;
        public int hideousMasses;
        public float uncommonAntiBuffer;
        public int specialAntiBuffer;

        #region Public

        public void GetReflectedFields(EndlessGrid __instance)
        {
            //set reflected value types. Not sure which ones are actually needed, so I just set them all.
            points = (double)((int)pointsField.GetValue(__instance));
            usedMeleePositions = (int)usedMeleePositionsField.GetValue(__instance);
            usedProjectilePositions = (int)usedProjectilePositionsField.GetValue(__instance);
            hideousMasses = (int)hideousMassesField.GetValue(__instance);

            //ah yes, the common single-element float value buffer. That's totally what this is :) I'm suffering programmer brainrot (my own).
            uncommonAntiBuffer = (float)uncommonAntiBufferField.GetValue(__instance);
            specialAntiBuffer = (int)specialAntiBufferField.GetValue(__instance);
        }

        public void SetReflectedFields(EndlessGrid __instance)
        {
            //set reflected value types. Not sure which ones are actually needed, so I just set them all.
            pointsField.SetValue(__instance, Math.Max(0, (int)points));
            usedMeleePositionsField.SetValue(__instance, usedMeleePositions);
            usedProjectilePositionsField.SetValue(__instance, usedProjectilePositions);
            hideousMassesField.SetValue(__instance, hideousMasses);
            uncommonAntiBufferField.SetValue(__instance, uncommonAntiBuffer);
            specialAntiBufferField.SetValue(__instance, specialAntiBuffer);
        }

        #endregion
    }

    private static readonly FieldInfo nvmhlprField = typeof(EndlessGrid).GetField(
        "nvmhlpr",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo meleePositionsField = typeof(EndlessGrid).GetField(
        "meleePositions",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo projectilePositionsField = typeof(EndlessGrid).GetField(
        "projectilePositions",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo pointsField = typeof(EndlessGrid).GetField(
        "points",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo usedMeleePositionsField = typeof(EndlessGrid).GetField(
        "usedMeleePositions",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo usedProjectilePositionsField = typeof(EndlessGrid).GetField(
        "usedProjectilePositions",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo spawnedEnemyTypesField = typeof(EndlessGrid).GetField(
        "spawnedEnemyTypes",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo hideousMassesField = typeof(EndlessGrid).GetField(
        "hideousMasses",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo uncommonAntiBufferField = typeof(EndlessGrid).GetField(
        "uncommonAntiBuffer",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly FieldInfo prefabsField = typeof(EndlessGrid).GetField(
        "prefabs",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo specialAntiBufferField = typeof(EndlessGrid).GetField(
        "specialAntiBuffer",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo spawnUncommonsMethod = typeof(EndlessGrid).GetMethod(
        "SpawnUncommons",
        BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo getIndexOfEnemyTypeMethod = typeof(EndlessGrid).GetMethod(
        "GetIndexOfEnemyType",
        BindingFlags.Instance | BindingFlags.NonPublic);

    public static readonly MethodInfo spawnOnGridMethod = typeof(EndlessGrid).GetMethod(
        "SpawnOnGrid",
        BindingFlags.Instance | BindingFlags.NonPublic);

    #region Public

    public static void Initialize()
    {
        
    }

    public static double GetEnemySpawnCost(EndlessEnemy enemy, EnemySpawnRarity rarity)
    {
        var cost = (double)enemy.spawnCost;

        switch (rarity)
        {
            case EnemySpawnRarity.Common:
                cost = cost;

                break;
            case EnemySpawnRarity.Uncommon:
                cost *= uncommonEnemySpawnCostMultiplier;

                break;
            case EnemySpawnRarity.Special:
                cost *= specialEnemySpawnCostMultiplier;

                break;
        }

        if (specificEnemySpawnCostMultipliers.TryGetValue(enemy, out var specificMul))
            cost *= specificMul;
        
        return cost;
    }

    public static bool Prefix(EndlessGrid __instance)
    {
        //this mod is a major assist, no leaderboards!
        MonoSingleton <StatsManager>.Instance.majorUsed = true;
        
        //all the private reflection crap
        //would use harmony traverse or whatever, but I didn't get that to work
        /*
        FieldInfo nvmhlprField = typeof(EndlessGrid).GetField(
            "nvmhlpr",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo meleePositionsField = typeof(EndlessGrid).GetField(
            "meleePositions",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo projectilePositionsField = typeof(EndlessGrid).GetField(
            "projectilePositions",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo pointsField = typeof(EndlessGrid).GetField("points", BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo usedMeleePositionsField = typeof(EndlessGrid).GetField(
            "usedMeleePositions",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo usedProjectilePositionsField = typeof(EndlessGrid).GetField(
            "usedProjectilePositions",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo spawnedEnemyTypesField = typeof(EndlessGrid).GetField(
            "spawnedEnemyTypes",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo hideousMassesField = typeof(EndlessGrid).GetField(
            "hideousMasses",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo uncommonAntiBufferField = typeof(EndlessGrid).GetField(
            "uncommonAntiBuffer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo prefabsField = typeof(EndlessGrid).GetField(
            "prefabs",
            BindingFlags.Instance | BindingFlags.NonPublic);

        FieldInfo specialAntiBufferField = typeof(EndlessGrid).GetField(
            "specialAntiBuffer",
            BindingFlags.Instance | BindingFlags.NonPublic);

        MethodInfo spawnUncommonsMethod = typeof(EndlessGrid).GetMethod(
            "SpawnUncommons",
            BindingFlags.Instance | BindingFlags.NonPublic);

        MethodInfo getIndexOfEnemyTypeMethod = typeof(EndlessGrid).GetMethod(
            "GetIndexOfEnemyType",
            BindingFlags.Instance | BindingFlags.NonPublic);

        MethodInfo spawnOnGridMethod = typeof(EndlessGrid).GetMethod(
            "SpawnOnGrid",
            BindingFlags.Instance | BindingFlags.NonPublic);

        MethodInfo spawnRadiantMethod = typeof(EndlessGrid).GetMethod(
            "SpawnRadiant",
            BindingFlags.Instance | BindingFlags.NonPublic);
            */

        //nevermindhelper
        var nvmhlpr = (CyberGrindNavHelper)nvmhlprField.GetValue(__instance);
        List <Vector2> meleePositions = (List <Vector2>)meleePositionsField.GetValue(__instance);
        List <Vector2> projectilePositions = (List <Vector2>)projectilePositionsField.GetValue(__instance);

        List <EnemyTypeTracker> spawnedEnemyTypes =
            (List <EnemyTypeTracker>)spawnedEnemyTypesField.GetValue(__instance);

        var prefabs = (PrefabDatabase)prefabsField.GetValue(__instance);

        var v = new ReflectedValueTypes();
        v.GetReflectedFields(__instance);

        __instance.nms.BuildNavMesh();
        nvmhlpr.GenerateLinks(__instance.cubes);

        for (var index1 = 0; index1 < meleePositions.Count; ++index1)
        {
            Vector2 meleePosition = meleePositions[index1];
            var index2 = Random.Range(index1, meleePositions.Count);
            meleePositions[index1] = meleePositions[index2];
            meleePositions[index2] = meleePosition;
        }

        for (var index3 = 0; index3 < projectilePositions.Count; ++index3)
        {
            Vector2 projectilePosition = projectilePositions[index3];
            var index4 = Random.Range(index3, projectilePositions.Count);
            projectilePositions[index3] = projectilePositions[index4];
            projectilePositions[index4] = projectilePosition;
        }

        __instance.tempEnemyAmount = 0;
        v.usedMeleePositions = 0;
        v.usedProjectilePositions = 0;
        spawnedEnemyTypes.Clear();
        __instance.tempEnemyAmount += v.hideousMasses;
        v.hideousMasses = 0;
        

        if (__instance.currentWave > 11 + waveThresholdRadiantSpawnBonus)
        {
            var currentWave = __instance.currentWave;
            var num1 = 0;

            //WHY is this in the orignal code?????
            /*
            while (currentWave >= 10)
            {
                currentWave -= 10;
                ++num1;
            }
            */
            //do this instead you dumbdumb
            num1 += currentWave / 10;
            
            if (__instance.tempEnemyAmount > 0)
                num1 -= __instance.tempEnemyAmount;

            if (v.uncommonAntiBuffer < 1.0 && num1 > 0)
            {
                var amount1 = Random.Range(0, __instance.currentWave / 10 + 1);

                if (v.uncommonAntiBuffer <= -0.5 && amount1 < 1)
                    amount1 = 1;

                if (amount1 > 0 && meleePositions.Count > 0)
                {
                    var target1 = Random.Range(0, prefabs.uncommonEnemies.Length);
                    var target2 = Random.Range(0, prefabs.uncommonEnemies.Length);
                    var amount2 = 0;

                    while (target1 >= 0 && __instance.currentWave < prefabs.uncommonEnemies[target1].spawnWave + waveThresholdSpawnBonus)
                        --target1;

                    for (;
                         target2 >= 0 &&
                         (__instance.currentWave < prefabs.uncommonEnemies[target2].spawnWave || target2 == target1);
                         --target2)
                    {
                        if (target2 == 0)
                        {
                            amount2 = -1;

                            break;
                        }
                    }

                    if (target1 >= 0)
                    {
                        if (__instance.currentWave > 16 + waveThresholdSpawnBonus)
                        {
                            if (__instance.currentWave < 25 + waveThresholdSpawnBonus)
                                ++amount1;
                            else if (amount2 != -1)
                                amount2 = amount1;
                        }

                        var flag1 = false;

                        //changes:
                        //points
                        //spawnedEnemyTypes
                        //usedProjectilePositions
                        //usedMeleePositions
                        //tempEnemyAmount
                        //also calls: SpawnOnGrid, changes:
                        //nothing?
                        v.SetReflectedFields(__instance);
                        //OK.Log($"Spawning uncommon1 #{target1} ({prefabs.uncommonEnemies[target1].prefab.name}");
                        var flag2 = (bool)spawnUncommonsMethod.Invoke(__instance, new object[] {target1, amount1});
                        v.GetReflectedFields(__instance);

                        if (amount2 > 0)
                        {
                            v.SetReflectedFields(__instance);
                            //OK.Log($"Spawning uncommon2 #{target1} ({prefabs.uncommonEnemies[target2].prefab.name}");
                            flag1 = (bool)spawnUncommonsMethod.Invoke(__instance, new object[] {target2, amount2});
                            v.GetReflectedFields(__instance);
                        }

                        if (flag2 | flag1)
                        {
                            if (v.uncommonAntiBuffer < 0.0)
                                v.uncommonAntiBuffer = 0.0f;

                            if (flag2)
                            {
                                v.uncommonAntiBuffer +=
                                    prefabs.uncommonEnemies[target1].enemyType == EnemyType.Stalker ||
                                    prefabs.uncommonEnemies[target1].enemyType == EnemyType.Idol
                                        ? 1f
                                        : 0.5f;
                            }

                            if (flag1)
                            {
                                v.uncommonAntiBuffer +=
                                    prefabs.uncommonEnemies[target2].enemyType == EnemyType.Stalker ||
                                    prefabs.uncommonEnemies[target2].enemyType == EnemyType.Idol
                                        ? 1f
                                        : 0.5f;
                            }

                            num1 -= flag2 & flag1 ? 2 : 1;
                        }
                    }
                }
            }
            else
                --v.uncommonAntiBuffer;

            //OK.Log($"Spawns wave {__instance.currentWave} (+{-waveThresholdSpawnBonus}), can spawn specials: {currentWave > 15 + waveThresholdSpawnBonus}, cond1: {v.specialAntiBuffer <= 0}, cond2: {num1 > 0}, num1: {num1}, melee pos: {meleePositions.Count}, tempcount: {__instance.tempEnemyAmount}");

            
            if (__instance.currentWave > 15 + waveThresholdSpawnBonus)
            {
                var flag = false;

                if (v.specialAntiBuffer <= 0 && num1 > 0)
                {
                    var num2 = Random.Range(0, num1 + 1);

                    if (v.specialAntiBuffer <= -2 && num2 < 1)
                        num2 = 1;

                    if (num2 > 0 && meleePositions.Count > 0)
                    {
                        for (var index5 = 0; index5 < num2; ++index5)
                        {
                            var index6 = Random.Range(0, prefabs.specialEnemies.Length);

                            var indexOfEnemyType =
                                (int)getIndexOfEnemyTypeMethod.Invoke(
                                    __instance,
                                    new object[] {prefabs.specialEnemies[index6].enemyType});

                            var num3 = 0.0f;
                            
                            while (index6 >= 0 && v.usedMeleePositions < meleePositions.Count - 1)
                            {
                                OK.Log($"Try to spawn special at thresholdbonus: {waveThresholdSpawnBonus}: {prefabs.specialEnemies[index6].prefab.name}, spawnwave: {prefabs.specialEnemies[index6].spawnWave}");
                                if (__instance.currentWave >= (prefabs.specialEnemies[index6].spawnWave + waveThresholdSpawnBonus) &&
                                    v.points >= (double)GetEnemySpawnCost(prefabs.specialEnemies[index6], EnemySpawnRarity.Special) + num3)
                                {
                                    var radiant = PatchedSpawnRadiant(prefabs.specialEnemies[index6], indexOfEnemyType);
                                    
                                    OK.Log($"Spawning special #{prefabs.specialEnemies[index6].prefab}");
                                    spawnOnGridMethod.Invoke(
                                        __instance,
                                        new object[]
                                        {
                                            prefabs.specialEnemies[index6].prefab,
                                            meleePositions[v.usedMeleePositions],
                                            false, //prefab
                                            true,  //enemy
                                            CyberPooledType.None,
                                            radiant
                                        });

                                    v.points -= GetEnemySpawnCost(prefabs.specialEnemies[index6], EnemySpawnRarity.Special) * (radiant ? 3d : 1d) + num3;

                                    var num4 = num3 +
                                               prefabs.specialEnemies[index6].costIncreasePerSpawn *
                                               (radiant ? 3 : 1);

                                    ++spawnedEnemyTypes[indexOfEnemyType].amount;
                                    ++v.usedMeleePositions;
                                    ++__instance.tempEnemyAmount;

                                    if (v.specialAntiBuffer < 0)
                                        v.specialAntiBuffer = 0;

                                    ++v.specialAntiBuffer;
                                    flag = true;

                                    break;
                                }

                                --index6;

                                if (index6 >= 0)
                                {
                                    indexOfEnemyType =
                                        (int)getIndexOfEnemyTypeMethod.Invoke(
                                            __instance,
                                            new object[] {prefabs.specialEnemies[index6].enemyType});
                                }
                            }
                        }
                    }
                }

                if (!flag)
                    --v.specialAntiBuffer;
            }
        }

        v.SetReflectedFields(__instance);

        //changes:
        //points
        //spawnedEnemyTypes
        //usedMeleePositions
        //usedProjectilePositions
        //tempEnemyAmount
        PatchCybergrindEnemySpawning.__instance = __instance;
        PatchedGetNextEnemy();

        return false;
    }

    #endregion

    #region Private

    private static object TryCallMethod(MethodInfo mi, EndlessGrid __instance, object[] parameters)
    {
        object res = null;
        try
        {
            res = mi.Invoke(__instance, parameters);
        }
        catch (Exception ex)
        {
            OK.Log($"Failed calling {mi.Name} (params: {string.Join(", ", mi.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"))})\n{ex.ToString()}");
        }

        return res;
    }

    private static EndlessGrid __instance;

    private static void PatchedGetNextEnemy()
    {
        if (__instance == null || !__instance.gameObject.scene.isLoaded)
            return;
        
        //nevermindhelper
        var nvmhlpr = (CyberGrindNavHelper)nvmhlprField.GetValue(__instance);
        List <Vector2> meleePositions = (List <Vector2>)meleePositionsField.GetValue(__instance);
        List <Vector2> projectilePositions = (List <Vector2>)projectilePositionsField.GetValue(__instance);

        List <EnemyTypeTracker> spawnedEnemyTypes =
            (List <EnemyTypeTracker>)spawnedEnemyTypesField.GetValue(__instance);

        var prefabs = (PrefabDatabase)prefabsField.GetValue(__instance);

        var v = new ReflectedValueTypes();
        v.GetReflectedFields(__instance);


        var pointsPrior = v.points;
        
        if (v.points > 0 && v.usedMeleePositions < meleePositions.Count ||
            v.points > 1 && v.usedProjectilePositions < projectilePositions.Count)
        {
            if ((Random.Range(0.0f, 1f) < 0.5 || v.usedProjectilePositions >= projectilePositions.Count) &&
                v.usedMeleePositions < meleePositions.Count)
            {
                int num1 = Random.Range(0, prefabs.meleeEnemies.Length);
                var flag = false;

                for (var index = num1; index >= 0; --index)
                {
                    EndlessEnemy meleeEnemy = prefabs.meleeEnemies[index];
                    int indexOfEnemyType = (int)TryCallMethod(getIndexOfEnemyTypeMethod, __instance, new object[]{meleeEnemy.enemyType});
                    var num2 = meleeEnemy.costIncreasePerSpawn * spawnedEnemyTypes[indexOfEnemyType].amount;
                    var num3 = GetEnemySpawnCost(meleeEnemy, EnemySpawnRarity.Common) + num2;

                    if (((double)v.points >= num3 * 1.5 || index == 0 && v.points >= num3) &&
                        __instance.currentWave >= meleeEnemy.spawnWave + waveThresholdSpawnBonus)
                    {
                        bool radiant = PatchedSpawnRadiant(meleeEnemy, indexOfEnemyType);
                        flag = true;

                        //OK.Log($"Spawning common melee enemy: { meleeEnemy.prefab.name}");
                        v.SetReflectedFields(__instance);
                        TryCallMethod(spawnOnGridMethod, __instance, new object[]{
                            meleeEnemy.prefab,
                            meleePositions[v.usedMeleePositions],
                            false, //prefab
                            true, //enemy
                            CyberPooledType.None,
                            radiant
                            });
                        
                        v.GetReflectedFields(__instance);
                        v.points -= GetEnemySpawnCost(meleeEnemy, EnemySpawnRarity.Common) * (radiant ? 3 : 1) + num2;
                        ++spawnedEnemyTypes[indexOfEnemyType].amount;
                        ++v.usedMeleePositions;
                        ++__instance.tempEnemyAmount;

                        break;
                    }
                }

                if (!flag)
                    v.usedMeleePositions = meleePositions.Count;
            }
            else if (v.usedProjectilePositions < projectilePositions.Count)
            {
                int num4 = Random.Range(0, prefabs.projectileEnemies.Length);
                var flag = false;

                for (var index = num4; index >= 0; --index)
                {
                    EndlessEnemy projectileEnemy = prefabs.projectileEnemies[index];
                    int indexOfEnemyType = (int)TryCallMethod(getIndexOfEnemyTypeMethod, __instance, new object[]{projectileEnemy.enemyType});
                    var num5 = projectileEnemy.costIncreasePerSpawn * spawnedEnemyTypes[indexOfEnemyType].amount;
                    var num6 = GetEnemySpawnCost(projectileEnemy, EnemySpawnRarity.Common) + num5;

                    if (((double)v.points >= num6 * 1.5 || index == 0 && v.points >= num6) &&
                        __instance.currentWave >= projectileEnemy.spawnWave + waveThresholdSpawnBonus)
                    {
                        bool radiant = PatchedSpawnRadiant(projectileEnemy, indexOfEnemyType);
                        flag = true;
                        
                        //OK.Log($"Spawning common projectile enemy: {projectileEnemy.prefab.name}");
                        v.SetReflectedFields(__instance);
                        TryCallMethod(spawnOnGridMethod,
                            __instance,
                            new object[]
                            {
                                projectileEnemy.prefab,
                                projectilePositions[v.usedProjectilePositions],
                                false, //prefab
                                true,  //enemy
                                CyberPooledType.None,
                                radiant
                            });
                        v.GetReflectedFields(__instance);

                        v.points -= GetEnemySpawnCost(projectileEnemy, EnemySpawnRarity.Common) * (radiant ? 3 : 1) + num5;
                        ++spawnedEnemyTypes[indexOfEnemyType].amount;
                        ++v.usedProjectilePositions;
                        ++__instance.tempEnemyAmount;

                        break;
                    }
                }

                if (!flag)
                    v.usedProjectilePositions = projectilePositions.Count;
            }

            
            v.SetReflectedFields(__instance);
            v.GetReflectedFields(__instance);
            
            //OK.Log($"Spawned #{__instance.tempEnemyAmount}, prior points: {pointsPrior}, pointsnow: {v.points}, {(int)pointsField.GetValue(__instance)}");
            __instance.StartCoroutine(SpawnNextTrashEnemyDelayedCoroutine(0.1f));

        }
        else
        {
            //OK.Log($"DONE SPAWNING! Got {__instance.enemyAmount}");
            var enemyCount = __instance.tempEnemyAmount;
            OK.Log($"NUM before: { __instance.enemyAmount}, {__instance.GetSpawnedEnemies().Length}");
            int numCustomSpawns = customSpawns.DoSpawns(__instance, meleePositions, projectilePositions, ref v, spawnedEnemyTypes);
            __instance.enemyAmount = enemyCount;
            OK.Log($"NUM after: { __instance.enemyAmount}, {__instance.GetSpawnedEnemies().Length}");

        }
    }

    private static IEnumerator SpawnNextTrashEnemyDelayedCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        PatchedGetNextEnemy();
    }

    private static bool PatchedSpawnRadiant(EndlessEnemy target, int indexOf)
    {
        float num1 = (float) (target.spawnWave * 2 + 25);
        float num2 = (float) target.spawnCost;
        if (target.spawnCost < 10)
            ++num2;
        if (target.spawnCost > 10)
            num2 = (float) ((double) num2 / 2.0 + 5.0);
        
        List <EnemyTypeTracker> spawnedEnemyTypes =
            (List <EnemyTypeTracker>)spawnedEnemyTypesField.GetValue(__instance);
        
        //OK.Log($"Call to spawnRadiant!\n\tCurrwave ({__instance.currentWave}) >= {num1} + {spawnedEnemyTypes[indexOf].type} ({spawnedEnemyTypes[indexOf].amount}) * {num2}\n\t{__instance.currentWave} >= {(double) num1 + (double) spawnedEnemyTypes[indexOf].amount * (double) num2}");
        return (double) __instance.currentWave - waveThresholdRadiantSpawnBonus >= (double) num1 + (double) spawnedEnemyTypes[indexOf].amount * (double) num2;
    }

    #endregion

    public static void DisplayInvasionText(string s)
    {
        if (invasionText == null)
        {
            Canvas canvas = CheatsController.Instance.cheatsInfo.canvas;


            var go = UnityEngine.Object.Instantiate(CheatsController.Instance.cheatsInfo.gameObject, canvas.transform);

            //var dn = go.GetComponent <DamageNumber>();
            var text = go.GetComponent <TMP_Text>();
            text.richText = true;
            text.alpha = 0f;
        
            text.horizontalAlignment = HorizontalAlignmentOptions.Center;
            text.verticalAlignment = VerticalAlignmentOptions.Middle;
            text.raycastTarget = false;

            text.rectTransform.position += Vector3.down * Screen.height * 0.33f;

            invasionText = text;

            invasionText.fontSize *= 1.25f;
            var rt = text.rectTransform;

            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = new Vector2(200f, 200f);
            rt.anchoredPosition = Vector2.zero;
        }
        
        invasionText.richText = true;
        invasionText.alpha = 1f;
        invasionText.text = s;


        NewMovement.Instance.StartCoroutine(FadeInvasionText());
    }

    private static IEnumerator FadeInvasionText()
    {
        float t1 = 0f;

        while (t1 < 5f)
        {
            invasionText.alpha = (Mathf.Cos((t1 * Mathf.PI * 2)) + 1f * 0.5f) * 0.8f + 0.2f;

            yield return new WaitForEndOfFrame();

            t1 += Time.deltaTime;
        }

        while (invasionText.alpha > 0)
        {
            invasionText.alpha -= Time.deltaTime * 2f;

            yield return new WaitForEndOfFrame();
        }

        invasionText.text = string.Empty;
        invasionText.alpha = 0f;
    }
}
