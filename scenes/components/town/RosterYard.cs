using Godot;
using System.Collections.Generic;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Town;

public partial class RosterYard : Node2D
{
    private const string RosterYardGladiatorScenePath = "res://scenes/components/town/RosterYardGladiator.tscn";
    private const float MinimumSpacing = 76f;
    private static readonly Rect2 SpawnArea = new(new Vector2(-220f, -104f), new Vector2(440f, 148f));

    private readonly RandomNumberGenerator _random = new();
    private SaveNode _saveNode;
    private Node2D _gladiators;
    private PackedScene _rosterYardGladiatorScene;
    private RosterYardGladiator _selectedGladiator;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _gladiators = GetNode<Node2D>("Gladiators");
        _rosterYardGladiatorScene = ResourceLoader.Load<PackedScene>(RosterYardGladiatorScenePath);
        _random.Randomize();

        if (_saveNode?.CompanyRunData != null)
            _saveNode.CompanyRunData.RunChanged += RefreshGladiators;

        RefreshGladiators();
    }

    public override void _ExitTree()
    {
        if (_saveNode?.CompanyRunData != null)
            _saveNode.CompanyRunData.RunChanged -= RefreshGladiators;
    }

    private void RefreshGladiators()
    {
        if (_gladiators == null)
            return;

        var runData = _saveNode?.CompanyRunData;
        if (runData == null || _rosterYardGladiatorScene == null)
            return;

        var activeGladiators = new HashSet<GladiatorData>();
        foreach (var gladiator in runData.Gladiators)
        {
            if (gladiator != null)
                activeGladiators.Add(gladiator);
        }

        var existingGladiators = new Dictionary<GladiatorData, RosterYardGladiator>();
        foreach (var child in _gladiators.GetChildren())
        {
            if (child is not RosterYardGladiator yardGladiator)
                continue;

            if (yardGladiator.GladiatorData == null || !activeGladiators.Contains(yardGladiator.GladiatorData))
            {
                yardGladiator.Pressed -= OnGladiatorPressed;
                if (_selectedGladiator == yardGladiator)
                    _selectedGladiator = null;

                yardGladiator.QueueFree();
                continue;
            }

            existingGladiators[yardGladiator.GladiatorData] = yardGladiator;
        }

        var positions = new Godot.Collections.Array<Vector2>();
        foreach (var yardGladiator in existingGladiators.Values)
        {
            positions.Add(yardGladiator.Position);
        }

        foreach (var gladiator in runData.Gladiators)
        {
            if (gladiator == null)
                continue;

            if (existingGladiators.TryGetValue(gladiator, out var existingGladiator))
            {
                existingGladiator.Configure(gladiator);
                continue;
            }

            var yardGladiator = _rosterYardGladiatorScene.Instantiate<RosterYardGladiator>();
            yardGladiator.Configure(gladiator);
            yardGladiator.Pressed += OnGladiatorPressed;
            yardGladiator.Position = PickOpenPosition(positions);
            positions.Add(yardGladiator.Position);
            _gladiators.AddChild(yardGladiator);
        }
    }

    private void OnGladiatorPressed(RosterYardGladiator gladiator)
    {
        if (gladiator == null)
            return;

        if (_selectedGladiator == gladiator)
        {
            _selectedGladiator.SetSelected(false);
            _selectedGladiator = null;
            return;
        }

        _selectedGladiator?.SetSelected(false);
        _selectedGladiator = gladiator;
        _selectedGladiator.SetSelected(true);
    }

    private Vector2 PickOpenPosition(Godot.Collections.Array<Vector2> existingPositions)
    {
        for (var attempt = 0; attempt < 32; attempt++)
        {
            var position = new Vector2(
                _random.RandfRange(SpawnArea.Position.X, SpawnArea.End.X),
                _random.RandfRange(SpawnArea.Position.Y, SpawnArea.End.Y));

            if (IsFarEnoughFromExisting(position, existingPositions))
                return position;
        }

        return new Vector2(
            _random.RandfRange(SpawnArea.Position.X, SpawnArea.End.X),
            _random.RandfRange(SpawnArea.Position.Y, SpawnArea.End.Y));
    }

    private static bool IsFarEnoughFromExisting(Vector2 position, Godot.Collections.Array<Vector2> existingPositions)
    {
        foreach (var existingPosition in existingPositions)
        {
            if (position.DistanceTo(existingPosition) < MinimumSpacing)
                return false;
        }

        return true;
    }
}
