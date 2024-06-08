using System;
using BepInEx.Logging;
using Newtonsoft.Json;
using OVERKILL.Upgrades.Cybergrind;

namespace OVERKILL.JSON;

public class EndlessEnemyConverter : JsonConverter <EndlessEnemy>
{
    public override void WriteJson(JsonWriter writer, EndlessEnemy? value, JsonSerializer serializer)
    {
        writer.WriteValue(value.enemyType.ToString());
    }

    public override EndlessEnemy? ReadJson(
        JsonReader reader,
        Type objectType,
        EndlessEnemy? existingValue,
        bool hasExistingValue,
        JsonSerializer serializer)
    {
        var enemyType = (EnemyType)Enum.Parse(typeof(EnemyType), reader.ReadAsString());
        var prefabs = (PrefabDatabase)PatchCybergrindEnemySpawning.prefabsField.GetValue(EndlessGrid.Instance);
        
        foreach (var enemy in prefabs.meleeEnemies)
        {
            if (enemy.enemyType == enemyType)
                return enemy;
        }
        
        foreach (var enemy in prefabs.projectileEnemies)
        {
            if (enemy.enemyType == enemyType)
                return enemy;
        }
        
        foreach (var enemy in prefabs.uncommonEnemies)
        {
            if (enemy.enemyType == enemyType)
                return enemy;
        }
        
        foreach (var enemy in prefabs.specialEnemies)
        {
            if (enemy.enemyType == enemyType)
                return enemy;
        }

        OK.Log($"Unable to find prefab for deserialized enemytype {enemyType}!", LogLevel.Error);
        return null;
    }
}
