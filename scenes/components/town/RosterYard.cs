using Godot;
using System.Collections.Generic;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Town;

public partial class RosterYard : Node2D
{
    private const string RosterYardGladiatorScenePath = "res://scenes/components/town/RosterYardGladiator.tscn";
    private const float DragStartDistance = 8f;
    private const float DragTokenHeight = 72f;
    private const float MinimumSpacing = 76f;
    private static readonly Rect2 SpawnArea = new(new Vector2(-220f, -104f), new Vector2(440f, 148f));

    private readonly RandomNumberGenerator _random = new();
    private SaveNode _saveNode;
    private Node2D _gladiators;
    private PackedScene _rosterYardGladiatorScene;
    private RosterYardGladiator _pendingGladiator;
    private Vector2 _pendingPressViewportPosition;
    private RosterYardGladiator _draggedGladiator;
    private Sprite2D _dragToken;
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

    public override void _Input(InputEvent inputEvent)
    {
        if (_pendingGladiator == null && _draggedGladiator == null)
            return;

        if (inputEvent is InputEventMouseMotion mouseMotion)
        {
            UpdatePointerDrag(mouseMotion.Position);
            return;
        }

        if (inputEvent is InputEventScreenDrag screenDrag)
        {
            UpdatePointerDrag(screenDrag.Position);
            return;
        }

        if (inputEvent is InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left } mouseButton)
        {
            FinishPointerInteraction(mouseButton.Position);
            return;
        }

        if (inputEvent is InputEventScreenTouch { Pressed: false } screenTouch)
            FinishPointerInteraction(screenTouch.Position);
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
                yardGladiator.PointerPressed -= OnGladiatorPointerPressed;
                if (_selectedGladiator == yardGladiator)
                    _selectedGladiator = null;

                if (_pendingGladiator == yardGladiator)
                    _pendingGladiator = null;

                if (_draggedGladiator == yardGladiator)
                    CancelDrag();

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
            yardGladiator.PointerPressed += OnGladiatorPointerPressed;
            yardGladiator.Position = PickOpenPosition(positions);
            positions.Add(yardGladiator.Position);
            _gladiators.AddChild(yardGladiator);
        }
    }

    private void OnGladiatorPointerPressed(RosterYardGladiator gladiator, Vector2 viewportPosition)
    {
        if (gladiator == null)
            return;

        _pendingGladiator = gladiator;
        _pendingPressViewportPosition = viewportPosition;
    }

    private void UpdatePointerDrag(Vector2 viewportPosition)
    {
        if (_draggedGladiator == null && _pendingGladiator != null)
        {
            if (viewportPosition.DistanceTo(_pendingPressViewportPosition) < DragStartDistance)
                return;

            StartDrag(_pendingGladiator, viewportPosition);
        }

        if (_dragToken != null)
            _dragToken.Position = ViewportToLocal(viewportPosition);
    }

    private void FinishPointerInteraction(Vector2 viewportPosition)
    {
        if (_draggedGladiator != null)
        {
            CancelDrag();
            return;
        }

        if (_pendingGladiator != null)
            OnGladiatorPressed(_pendingGladiator);

        _pendingGladiator = null;
    }

    private void StartDrag(RosterYardGladiator gladiator, Vector2 viewportPosition)
    {
        _draggedGladiator = gladiator;
        _pendingGladiator = null;
        _draggedGladiator.SetDragHidden(true);

        var texture = _draggedGladiator.GladiatorData?.GetPortraitTexture();
        _dragToken = new Sprite2D
        {
            Name = "DragToken",
            Texture = texture,
            Centered = true,
            Position = ViewportToLocal(viewportPosition),
            Modulate = new Color(1f, 1f, 1f, 0.82f)
        };

        if (texture != null && texture.GetHeight() > 0)
            _dragToken.Scale = Vector2.One * (DragTokenHeight / texture.GetHeight());

        AddChild(_dragToken);
    }

    private void CancelDrag()
    {
        _draggedGladiator?.SetDragHidden(false);
        _draggedGladiator = null;
        _pendingGladiator = null;

        if (_dragToken == null)
            return;

        _dragToken.QueueFree();
        _dragToken = null;
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

    private Vector2 ViewportToLocal(Vector2 viewportPosition)
    {
        var worldPosition = GetCanvasTransform().AffineInverse() * viewportPosition;
        return ToLocal(worldPosition);
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
