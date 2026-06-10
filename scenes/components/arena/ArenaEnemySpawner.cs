using Godot;
using System.Collections.Generic;
using Godot.Collections;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scenes.Components.Arena;

public partial class ArenaEnemySpawner : Node2D
{
    private const int MaxRows = 4;
    private const float ColumnSpacing = 90f;
    private const float RowSpacing = 82f;

    private readonly List<Node2D> _spawnedEnemies = new();

    [Export]
    public PackedScene FallbackEnemyScene { get; private set; }

    public int SpawnedEnemyCount => _spawnedEnemies.Count;

    public IEnumerable<EnemyCombatant> GetSpawnedEnemyCombatants()
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (GodotObject.IsInstanceValid(enemy) && enemy is EnemyCombatant combatant)
                yield return combatant;
        }
    }

    public void SpawnMobs(Array<EnemyMobData> mobs)
    {
        ClearSpawned();
        if (mobs == null)
            return;

        var enemyIndex = 0;
        foreach (var enemyMob in mobs)
        {
            var enemy = InstantiateEnemy(enemyMob);
            if (enemy == null)
                continue;

            enemy.Position = GetGridPosition(enemyIndex);
            AddChild(enemy);
            _spawnedEnemies.Add(enemy);
            enemyIndex++;
        }
    }

    private Node2D InstantiateEnemy(EnemyMobData mobData)
    {
        var scene = mobData.Scene ?? FallbackEnemyScene;
        if (scene == null)
        {
            GD.PushError($"Arena enemy spawn failed: mob '{mobData.DisplayName}' has no scene and no fallback enemy scene is assigned.");
            return null;
        }

        var instance = scene.Instantiate<Node2D>();

        if (instance is EnemyCombatant enemyCombatant)
        {
            enemyCombatant.ConfigureEnemy(mobData);
        }
        else if (instance.HasMethod("ConfigureEnemy"))
        {
            instance.Call("ConfigureEnemy", mobData);
        }
        else
        {
            instance.Name = string.IsNullOrWhiteSpace(mobData.DisplayName)
                ? "EnemyCombatant"
                : $"{mobData.DisplayName}EnemyCombatant";
        }

        return instance;
    }

    private static Vector2 GetGridPosition(int index)
    {
        var row = index % MaxRows;
        var column = index / MaxRows;
        var y = (row - (MaxRows - 1) * 0.5f) * RowSpacing;
        return new Vector2(column * ColumnSpacing, y);
    }

    private void ClearSpawned()
    {
        foreach (var enemy in _spawnedEnemies)
        {
            if (GodotObject.IsInstanceValid(enemy))
                enemy.QueueFree();
        }

        _spawnedEnemies.Clear();
    }
}
