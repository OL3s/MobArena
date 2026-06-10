using Godot;
using System.Collections.Generic;
using Godot.Collections;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Arena;

public partial class ArenaPlayerSpawner : Node2D
{
    private const float SpawnSpacing = 82f;

    private readonly List<Node2D> _spawnedPlayers = new();

    [Export]
    public PackedScene PlayerScene { get; private set; }

    public void SpawnFromRunData(CompanyRunData runData)
    {
        ClearSpawned();
        runData?.EnsureResources();
        if (runData?.TownAssignments?.ArenaGladiators == null)
            return;

        var gladiators = runData.TownAssignments.ArenaGladiators;
        SpawnGladiators(gladiators, runData);
    }

    public void SpawnGladiators(Array<GladiatorData> gladiators, CompanyRunData runData = null)
    {
        ClearSpawned();
        if (gladiators == null)
            return;

        var startY = -((gladiators.Count - 1) * SpawnSpacing) * 0.5f;
        for (var index = 0; index < gladiators.Count; index++)
        {
            var gladiator = gladiators[index];
            if (gladiator == null || runData?.HasGladiator(gladiator) == false)
                continue;

            var combatant = InstantiatePlayer(gladiator, runData?.GetArenaControlAssignment(gladiator));
            if (combatant == null)
                continue;

            combatant.Position = new Vector2(0f, startY + index * SpawnSpacing);
            AddChild(combatant);
            _spawnedPlayers.Add(combatant);
        }
    }

    public IEnumerable<PlayerCombatant> GetSpawnedPlayerCombatants()
    {
        foreach (var player in _spawnedPlayers)
        {
            if (GodotObject.IsInstanceValid(player) && player is PlayerCombatant combatant)
                yield return combatant;
        }
    }

    private Node2D InstantiatePlayer(GladiatorData gladiator, ArenaControlAssignmentData assignment)
    {
        if (PlayerScene == null)
        {
            GD.PushError("Arena player spawn failed: no player scene assigned.");
            return null;
        }

        var instance = PlayerScene.Instantiate<Node2D>();
        if (instance is PlayerCombatant playerCombatant)
        {
            playerCombatant.ConfigureGladiator(gladiator, assignment);
        }
        else if (instance.HasMethod("ConfigureGladiator"))
        {
            instance.Call("ConfigureGladiator", gladiator, assignment);
        }
        else
        {
            instance.Name = string.IsNullOrWhiteSpace(gladiator?.GladiatorName)
                ? "PlayerCombatant"
                : $"{gladiator.GladiatorName}PlayerCombatant";
        }

        return instance;
    }

    private void ClearSpawned()
    {
        foreach (var player in _spawnedPlayers)
        {
            if (GodotObject.IsInstanceValid(player))
                player.QueueFree();
        }

        _spawnedPlayers.Clear();
    }
}
