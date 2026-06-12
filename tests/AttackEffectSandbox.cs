using Godot;
using MobArena.Scenes.Components.Arena;
using MobArena.Scenes.Components.Arena.Combat.Effects;
using MobArena.Scripts.Resources.Combat.Actions;
using MobArena.Scripts.Resources.Mobs;
using System.Collections.Generic;
using System.Linq;

namespace MobArena.Tests;

public partial class AttackEffectSandbox : Node
{
    private const string DummyMobPath = "res://resources/mobs/training_dummy.tres";
    private const string DummyScenePath = "res://scenes/components/arena/EnemyCombatant.tscn";
    private const string AttackRootPath = "res://tests/attacks";

    private readonly List<AttackTestCase> _attacks = new();

    private Node2D _world;
    private Node2D _dummyRoot;
    private OptionButton _attackSelect;
    private Label _detailsLabel;
    private PackedScene _dummyScene;
    private EnemyMobData _dummyData;
    private ArenaCombatActionData _windupAction;
    private float _windupElapsed;

    public override void _Ready()
    {
        _world = GetNode<Node2D>("World");
        _dummyRoot = GetNode<Node2D>("World/Dummies");
        _attackSelect = GetNode<OptionButton>("ControllerUi/Panel/MarginContainer/Layout/AttackSelect");
        _detailsLabel = GetNode<Label>("ControllerUi/Panel/MarginContainer/Layout/DetailsLabel");
        _dummyScene = ResourceLoader.Load<PackedScene>(DummyScenePath);
        _dummyData = ResourceLoader.Load<EnemyMobData>(DummyMobPath);

        LoadAttackCases();
        PopulateAttackSelect();
        SpawnDummies();

        _attackSelect.ItemSelected += OnAttackSelected;
        GetNode<Button>("ControllerUi/Panel/MarginContainer/Layout/ResetDummiesButton").Pressed += ResetDummies;
        RefreshDetails();
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey { Pressed: true, Echo: false, Keycode: Key.F })
            return;

        HandleSpawnKeyPressed();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (_windupAction?.Windup == null)
            return;

        _windupElapsed += (float)delta;
        if (_windupAction.Windup.CanReleaseEarly != true && _windupElapsed >= Mathf.Max(0.05f, _windupAction.WindupSeconds))
        {
            SpawnAttack(_windupAction, _world.GetGlobalMousePosition(), ArenaCombatWindupData.MaxScalar);
            ClearWindup();
            return;
        }

        RefreshDetails();
    }

    private void LoadAttackCases()
    {
        _attacks.Clear();
        foreach (var path in GetAttackResourcePaths(AttackRootPath).OrderBy(path => path))
        {
            var action = ResourceLoader.Load<ArenaCombatActionData>(path);
            if (action?.Effect == null)
            {
                GameLogger.Combat($"Attack sandbox skipped '{path}' because it is not an ArenaCombatActionData with an Effect.");
                continue;
            }

            var displayName = string.IsNullOrWhiteSpace(action.DisplayName)
                ? path.GetFile().GetBaseName()
                : action.DisplayName;
            _attacks.Add(new AttackTestCase(displayName, action));
        }
    }

    private static IEnumerable<string> GetAttackResourcePaths(string directoryPath)
    {
        var directory = DirAccess.Open(directoryPath);
        if (directory == null)
        {
            GameLogger.Combat($"Attack sandbox attack directory does not exist: {directoryPath}");
            yield break;
        }

        foreach (var file in directory.GetFiles())
        {
            if (file.EndsWith(".tres", System.StringComparison.OrdinalIgnoreCase))
                yield return $"{directoryPath}/{file}";
        }

        foreach (var subdirectory in directory.GetDirectories())
        {
            if (subdirectory.StartsWith('.'))
                continue;

            foreach (var path in GetAttackResourcePaths($"{directoryPath}/{subdirectory}"))
                yield return path;
        }
    }

    private void PopulateAttackSelect()
    {
        _attackSelect.Clear();
        for (var i = 0; i < _attacks.Count; i++)
            _attackSelect.AddItem(_attacks[i].DisplayName, i);
    }

    private void OnAttackSelected(long index)
    {
        ClearWindup();
        RefreshDetails();
    }

    private void RefreshDetails()
    {
        var selected = GetSelectedAttack();
        _detailsLabel.Text = selected == null
            ? "No attack selected."
            : GetDetailsText(selected);
    }

    private string GetDetailsText(AttackTestCase selected)
    {
        if (_windupAction == selected.Action && selected.Action.Windup != null)
            return selected.Action.Windup.CanReleaseEarly
                ? $"Selected: {selected.DisplayName}\nWindup {_windupAction.Windup.GetScalar(_windupElapsed, selected.Action.WindupSeconds):0.00}. Press F again to spawn at the mouse position, facing right."
                : $"Selected: {selected.DisplayName}\nWindup {_windupAction.Windup.GetScalar(_windupElapsed, selected.Action.WindupSeconds):0.00}. Spawns automatically at full windup.";

        return selected.Action.Windup == null
            ? $"Selected: {selected.DisplayName}\nPress F to spawn at the mouse position, facing right. Reset Dummies restores all 9 targets."
            : $"Selected: {selected.DisplayName}\nPress F once to start windup, then F again to spawn at the mouse position, facing right.";
    }

    private void HandleSpawnKeyPressed()
    {
        var selected = GetSelectedAttack();
        if (selected?.Action?.Effect == null)
            return;

        if (selected.Action.Windup == null)
        {
            SpawnAttack(selected.Action, _world.GetGlobalMousePosition(), 1f);
            return;
        }

        if (_windupAction == selected.Action)
        {
            if (selected.Action.Windup.CanReleaseEarly != true)
                return;

            SpawnAttack(selected.Action, _world.GetGlobalMousePosition(), selected.Action.Windup.GetScalar(_windupElapsed, selected.Action.WindupSeconds));
            ClearWindup();
            return;
        }

        _windupAction = selected.Action;
        _windupElapsed = 0f;
        RefreshDetails();
    }

    private void SpawnAttack(ArenaCombatActionData action, Vector2 position, float windupScalar)
    {
        if (action?.Effect == null)
            return;

        var effect = action.Effect;
        ArenaCombatEffectSpawner.TrySpawn(
            _world,
            position,
            0f,
            new ArenaCombatEffectContext
            {
                Source = null,
                SourceTeam = ArenaCombatTeam.Neutral,
                SourceItem = null,
                ItemDamage = null,
                Action = action,
                Effect = effect,
                Direction = Vector2.Right,
                WindupScalar = Mathf.Clamp(windupScalar, 0.1f, 1f),
                MaxChainDepth = Mathf.Max(0, action.MaxChainDepth)
            },
            effect);
    }

    private void ClearWindup()
    {
        _windupAction = null;
        _windupElapsed = 0f;
    }

    private AttackTestCase GetSelectedAttack()
    {
        var selectedIndex = _attackSelect.Selected;
        return selectedIndex >= 0 && selectedIndex < _attacks.Count
            ? _attacks[selectedIndex]
            : null;
    }

    private void ResetDummies()
    {
        foreach (var child in _dummyRoot.GetChildren())
            child.QueueFree();

        SpawnDummies();
    }

    private void SpawnDummies()
    {
        if (_dummyScene == null || _dummyData == null)
            return;

        var positions = new[]
        {
            new Vector2(610, 205), new Vector2(760, 205), new Vector2(910, 205),
            new Vector2(610, 330), new Vector2(760, 330), new Vector2(910, 330),
            new Vector2(610, 455), new Vector2(760, 455), new Vector2(910, 455)
        };

        for (var i = 0; i < positions.Length; i++)
        {
            var dummy = _dummyScene.Instantiate<EnemyCombatant>();
            _dummyRoot.AddChild(dummy);
            dummy.GlobalPosition = positions[i];
            dummy.ConfigureEnemy(_dummyData);
            dummy.Name = $"TrainingDummy{i + 1}";
        }
    }

    private sealed record AttackTestCase(string DisplayName, ArenaCombatActionData Action);
}
