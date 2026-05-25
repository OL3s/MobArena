using Godot;
using System.Collections.Generic;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Town;

public partial class RosterYard : Node2D, IPhaseGoldCostSource
{
    public const string DragDropTargetGroup = "town_drag_drop_targets";
    public const string PhaseGoldCostSourceGroup = "phase_gold_cost_sources";

    private const string RosterYardGladiatorScenePath = "res://scenes/components/town/RosterYardGladiator.tscn";
    private const string GoldCostOverlayScenePath = "res://scenes/ui/GoldCostOverlay.tscn";
    private const string DragIconPath = "res://assets/ui/items/drag_hand.svg";
    private const string DragTutorialPopupTitle = "Drag to Manage";
    private const string DragTutorialPopupText = "Gladiators in the yard can be dragged onto town buildings to assign them. The hand icon marks things you can drag.";
    private const float DragStartDistance = 8f;
    private const float DragTokenHeight = 72f;
    private const float DragTokenPointerOffsetY = 32f;
    private const float DragTiltPerPixel = 0.03f;
    private const float MaxDragTiltRadians = 0.28f;
    private const float MinimumSpacing = 76f;
    private static readonly Rect2 SpawnArea = new(new Vector2(-220f, -104f), new Vector2(440f, 148f));

    private readonly RandomNumberGenerator _random = new();
    private SaveNode _saveNode;
    private Node2D _gladiators;
    private Button _gladiatorsButton;
    private Button _equipmentButton;
    private Button _goldButton;
    private RichTextLabel _emptyCourtyardLabel;
    private PanelContainer _goldTotalPreview;
    private Label _goldTotalPreviewLabel;
    private PackedScene _rosterYardGladiatorScene;
    private RosterYardGladiator _pendingGladiator;
    private Vector2 _pendingPressViewportPosition;
    private Vector2 _lastDragViewportPosition;
    private RosterYardGladiator _draggedGladiator;
    private GladiatorData _draggedGladiatorData;
    private ItemData _draggedItem;
    private Sprite2D _dragToken;
    private ITownDragDropTarget _previewedDropTarget;
    private bool _showingGladiatorDragCapacityHints;
    private bool _gladiatorsButtonHovered;
    private bool _equipmentButtonHovered;
    private bool _goldButtonHovered;
    private CompanyRunData _subscribedRunData;
    private TownPhaseState _subscribedPhaseState;

    public int PhaseGoldCostDisplayOrder => 0;

    public string PhaseGoldCostSection => "Gladiators";

    public override void _Ready()
    {
        AddToGroup("roster_yard");
        AddToGroup(PhaseGoldCostSourceGroup);
        _saveNode = SaveNode.Get();
        _saveNode.RuntimeStateResetting += OnRuntimeStateResetting;
        _gladiators = GetNode<Node2D>("Gladiators");
        _gladiatorsButton = GetNodeOrNull<Button>("ButtonRow/GladiatorsButton");
        _equipmentButton = GetNodeOrNull<Button>("ButtonRow/EquipmentButton");
        _goldButton = GetNodeOrNull<Button>("ButtonRow/GoldButton");
        _emptyCourtyardLabel = GetNodeOrNull<RichTextLabel>("EmptyCourtyardLabel");
        _goldTotalPreview = GetNodeOrNull<PanelContainer>("ButtonRow/GoldButton/GoldTotalPreview");
        _goldTotalPreviewLabel = GetNodeOrNull<Label>("ButtonRow/GoldButton/GoldTotalPreview/Row/TotalLabel");
        _rosterYardGladiatorScene = ResourceLoader.Load<PackedScene>(RosterYardGladiatorScenePath);
        _random.Randomize();

        if (_gladiatorsButton != null)
        {
            _gladiatorsButton.MouseEntered += OnGladiatorsButtonMouseEntered;
            _gladiatorsButton.MouseExited += OnGladiatorsButtonMouseExited;
        }

        if (_equipmentButton != null)
        {
            _equipmentButton.MouseEntered += OnEquipmentButtonMouseEntered;
            _equipmentButton.MouseExited += OnEquipmentButtonMouseExited;
        }

        if (_goldButton != null)
        {
            _goldButton.MouseEntered += OnGoldButtonMouseEntered;
            _goldButton.MouseExited += OnGoldButtonMouseExited;
            _goldButton.Pressed += OnGoldButtonPressed;
        }

        _subscribedRunData = _saveNode?.CompanyRunData;
        if (_subscribedRunData != null)
        {
            _subscribedRunData.RunChanged += RefreshGladiators;
            _subscribedRunData.RunChanged += RefreshGoldCostPreview;
        }

        _subscribedPhaseState = _saveNode?.TownPhaseState;
        if (_subscribedPhaseState != null)
            _subscribedPhaseState.PhaseChanged += RefreshGoldCostPreview;

        RefreshGladiators();
        RefreshGoldCostPreview();
    }

    public override void _ExitTree()
    {
        if (_saveNode != null)
            _saveNode.RuntimeStateResetting -= OnRuntimeStateResetting;

        UnsubscribeResourceSignals();

        if (_gladiatorsButton != null)
        {
            _gladiatorsButton.MouseEntered -= OnGladiatorsButtonMouseEntered;
            _gladiatorsButton.MouseExited -= OnGladiatorsButtonMouseExited;
        }

        if (_equipmentButton != null)
        {
            _equipmentButton.MouseEntered -= OnEquipmentButtonMouseEntered;
            _equipmentButton.MouseExited -= OnEquipmentButtonMouseExited;
        }

        if (_goldButton != null)
        {
            _goldButton.MouseEntered -= OnGoldButtonMouseEntered;
            _goldButton.MouseExited -= OnGoldButtonMouseExited;
            _goldButton.Pressed -= OnGoldButtonPressed;
        }
    }

    private void OnRuntimeStateResetting()
    {
        UnsubscribeResourceSignals();
    }

    private void UnsubscribeResourceSignals()
    {
        if (_subscribedRunData != null)
        {
            _subscribedRunData.RunChanged -= RefreshGladiators;
            _subscribedRunData.RunChanged -= RefreshGoldCostPreview;
            _subscribedRunData = null;
        }

        if (_subscribedPhaseState != null)
        {
            _subscribedPhaseState.PhaseChanged -= RefreshGoldCostPreview;
            _subscribedPhaseState = null;
        }
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (_pendingGladiator == null && _draggedGladiator == null && _draggedGladiatorData == null && _draggedItem == null)
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

        runData.EnsureResources();
        var courtyardGladiators = new HashSet<GladiatorData>();
        foreach (var gladiator in runData.TownAssignments.CourtyardGladiators)
        {
            if (gladiator != null)
                courtyardGladiators.Add(gladiator);
        }

        var existingGladiators = new Dictionary<GladiatorData, RosterYardGladiator>();
        foreach (var child in _gladiators.GetChildren())
        {
            if (child is not RosterYardGladiator yardGladiator)
                continue;

            if (yardGladiator.GladiatorData == null
                || !courtyardGladiators.Contains(yardGladiator.GladiatorData))
            {
                yardGladiator.PointerPressed -= OnGladiatorPointerPressed;
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

        if (_emptyCourtyardLabel != null)
            _emptyCourtyardLabel.Visible = runData.Gladiators.Count <= 0;

        RefreshShowcaseButtons(runData);

        foreach (var gladiator in runData.TownAssignments.CourtyardGladiators)
        {
            if (gladiator == null)
                continue;

            if (existingGladiators.TryGetValue(gladiator, out var existingGladiator))
            {
                existingGladiator.Configure(gladiator);
                ApplyGladiatorStatusContext(existingGladiator);
                continue;
            }

            var yardGladiator = _rosterYardGladiatorScene.Instantiate<RosterYardGladiator>();
            yardGladiator.Configure(gladiator);
            yardGladiator.PointerPressed += OnGladiatorPointerPressed;
            yardGladiator.Position = PickOpenPosition(positions);
            ApplyGladiatorStatusContext(yardGladiator);
            positions.Add(yardGladiator.Position);
            _gladiators.AddChild(yardGladiator);
        }

        if (runData.TownAssignments.CourtyardGladiators.Count > 0)
            CallDeferred(MethodName.ShowDragTutorialPopupIfNeeded);
    }

    private void ShowDragTutorialPopupIfNeeded()
    {
        var runData = _saveNode?.CompanyRunData;
        if (runData == null || runData.HasShownDragTutorialPopup || runData.TownAssignments.CourtyardGladiators.Count <= 0)
            return;

        runData.MarkDragTutorialPopupShown();
        _saveNode.Save();
        GlobalOverlay.Get()?.ShowBlurredPopup(
            DragTutorialPopupTitle,
            DragTutorialPopupText,
            ResourceLoader.Load<Texture2D>(DragIconPath));
    }

    private void RefreshShowcaseButtons(CompanyRunData runData)
    {
        if (runData == null)
            return;

        if (_gladiatorsButton != null)
            _gladiatorsButton.Visible = runData.Gladiators.Count > 0;

        if (_equipmentButton != null)
            _equipmentButton.Visible = runData.Inventory.Count > 0;

        RefreshGoldButtonVisibility(runData);
    }

    private void RefreshGoldButtonVisibility(CompanyRunData runData = null)
    {
        if (_goldButton == null)
            return;

        var phaseState = _saveNode?.TownPhaseState;
        var phaseGoldCost = (runData ?? _saveNode?.CompanyRunData)?.GetCurrentPhaseGoldCost(phaseState) ?? 0;
        _goldButton.Visible = phaseGoldCost > 0;

        if (_goldButton.Visible)
            return;

        _goldButtonHovered = false;
        if (_goldTotalPreview != null)
            _goldTotalPreview.Visible = false;
    }

    private void OnGladiatorsButtonMouseEntered()
    {
        _gladiatorsButtonHovered = true;
        RefreshGladiatorStatusContexts();
        SetGladiatorDragCapacityHintsVisible(true);
    }

    private void OnGladiatorsButtonMouseExited()
    {
        _gladiatorsButtonHovered = false;
        RefreshGladiatorStatusContexts();
        if (_draggedGladiator == null && _draggedGladiatorData == null)
            SetGladiatorDragCapacityHintsVisible(false);
    }

    private void OnEquipmentButtonMouseEntered()
    {
        _equipmentButtonHovered = true;
        RefreshGladiatorStatusContexts();
    }

    private void OnEquipmentButtonMouseExited()
    {
        _equipmentButtonHovered = false;
        RefreshGladiatorStatusContexts();
    }

    private void OnGoldButtonMouseEntered()
    {
        _goldButtonHovered = true;
        RefreshGoldCostPreview();
    }

    private void OnGoldButtonMouseExited()
    {
        _goldButtonHovered = false;
        RefreshGoldCostPreview();
    }

    private static void OnGoldButtonPressed()
    {
        var overlayScene = ResourceLoader.Load<PackedScene>(GoldCostOverlayScenePath);
        if (overlayScene == null)
            return;

        GlobalOverlay.Get()?.AddOverlay(overlayScene);
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
        {
            UpdateDragToken(viewportPosition);
            UpdateDropPreview(viewportPosition);
        }
    }

    private void FinishPointerInteraction(Vector2 viewportPosition)
    {
        if (_draggedGladiator != null || _draggedGladiatorData != null || _draggedItem != null)
        {
            TryDropPayload(viewportPosition);
            CancelDrag();
            return;
        }

        _pendingGladiator = null;
    }

    private void StartDrag(RosterYardGladiator gladiator, Vector2 viewportPosition)
    {
        _draggedGladiator = gladiator;
        _pendingGladiator = null;
        _draggedGladiator.SetDragHidden(true);

        var texture = _draggedGladiator.GladiatorData?.GetBodyForwardTexture();
        StartDragToken(texture, viewportPosition);
        SetGladiatorDragCapacityHintsVisible(true);
    }

    public void StartGladiatorDrag(GladiatorData gladiatorData, Vector2 viewportPosition)
    {
        if (gladiatorData == null)
            return;

        CancelDrag();
        _draggedGladiatorData = gladiatorData;
        StartDragToken(gladiatorData.GetBodyForwardTexture(), viewportPosition);
        SetGladiatorDragCapacityHintsVisible(true);
    }

    public void StartItemDrag(ItemData item, Vector2 viewportPosition)
    {
        if (item == null)
            return;

        CancelDrag();
        _draggedItem = item;
        StartDragToken(item.UiIcon, viewportPosition);
        SetGladiatorDragCapacityHintsVisible(false);
        RefreshGladiatorStatusContexts();
    }

    private void StartDragToken(Texture2D texture, Vector2 viewportPosition)
    {
        _dragToken = new Sprite2D
        {
            Name = "DragToken",
            Texture = texture,
            Centered = true,
            Position = GetDragTokenPosition(viewportPosition),
            Modulate = new Color(1f, 1f, 1f, 0.82f)
        };

        _lastDragViewportPosition = viewportPosition;

        if (texture != null && texture.GetHeight() > 0)
            _dragToken.Scale = Vector2.One * (DragTokenHeight / texture.GetHeight());

        AddChild(_dragToken);
    }

    private void UpdateDragToken(Vector2 viewportPosition)
    {
        var horizontalDelta = viewportPosition.X - _lastDragViewportPosition.X;
        _lastDragViewportPosition = viewportPosition;

        _dragToken.Position = GetDragTokenPosition(viewportPosition);
        _dragToken.Rotation = Mathf.Clamp(horizontalDelta * DragTiltPerPixel, -MaxDragTiltRadians, MaxDragTiltRadians);
    }

    private Vector2 GetDragTokenPosition(Vector2 viewportPosition)
    {
        return ViewportToLocal(viewportPosition) + new Vector2(0f, DragTokenPointerOffsetY);
    }

    private void CancelDrag()
    {
        _draggedGladiator?.SetDragHidden(false);
        _draggedGladiator = null;
        _draggedGladiatorData = null;
        _draggedItem = null;
        _pendingGladiator = null;
        _previewedDropTarget?.SetTownDragDropPreview(null, Vector2.Zero);
        _previewedDropTarget = null;
        SetGladiatorDragCapacityHintsVisible(false);
        RefreshGladiatorStatusContexts();

        if (_dragToken == null)
            return;

        _dragToken.QueueFree();
        _dragToken = null;
    }

    private void TryDropPayload(Vector2 viewportPosition)
    {
        var payload = GetCurrentDragPayload();
        if (payload == null)
            return;

        var target = GetBestDropTarget(payload.Value, viewportPosition);
        if (target != null)
        {
            target.ReceiveTownDragDrop(payload.Value, viewportPosition);
            return;
        }

        if (payload.Value.Kind == TownDragPayloadKind.Gladiator)
            _saveNode?.CompanyRunData?.TryMoveGladiatorToCourtyard(payload.Value.Gladiator);
    }

    private void UpdateDropPreview(Vector2 viewportPosition)
    {
        var payload = GetCurrentDragPayload();
        var target = payload == null ? null : GetBestPreviewTarget(payload.Value);
        if (_previewedDropTarget != target)
        {
            _previewedDropTarget?.SetTownDragDropPreview(null, viewportPosition);
            _previewedDropTarget = target;
        }

        _previewedDropTarget?.SetTownDragDropPreview(payload, viewportPosition);
    }

    private ITownDragDropTarget GetBestDropTarget(TownDragPayload payload, Vector2 viewportPosition)
    {
        ITownDragDropTarget bestTarget = null;
        foreach (var node in GetTree().GetNodesInGroup(DragDropTargetGroup))
        {
            if (node is not ITownDragDropTarget target || !target.CanReceiveTownDragDrop(payload, viewportPosition))
                continue;

            if (bestTarget == null || target.TownDragDropPriority > bestTarget.TownDragDropPriority)
                bestTarget = target;
        }

        return bestTarget;
    }

    private ITownDragDropTarget GetBestPreviewTarget(TownDragPayload payload)
    {
        ITownDragDropTarget bestTarget = null;
        foreach (var node in GetTree().GetNodesInGroup(DragDropTargetGroup))
        {
            if (node is not ITownDragDropTarget target || !target.CanPreviewTownDragDrop(payload))
                continue;

            if (bestTarget == null || target.TownDragDropPriority > bestTarget.TownDragDropPriority)
                bestTarget = target;
        }

        return bestTarget;
    }

    private TownDragPayload? GetCurrentDragPayload()
    {
        if (_draggedGladiator?.GladiatorData != null)
            return new TownDragPayload(_draggedGladiator.GladiatorData);

        if (_draggedGladiatorData != null)
            return new TownDragPayload(_draggedGladiatorData);

        if (_draggedItem != null)
            return new TownDragPayload(_draggedItem);

        return null;
    }

    private void SetGladiatorDragCapacityHintsVisible(bool visible)
    {
        if (_showingGladiatorDragCapacityHints == visible)
            return;

        _showingGladiatorDragCapacityHints = visible;
        foreach (var node in GetTree().GetNodesInGroup(DragDropTargetGroup))
        {
            if (node is TownBuilding townBuilding)
                townBuilding.SetGladiatorDragCapacityPreview(visible);
        }
    }

    private void RefreshGoldCostPreview()
    {
        RefreshGoldButtonVisibility();

        if (_goldTotalPreview != null)
            _goldTotalPreview.Visible = _goldButton?.Visible == true && _goldButtonHovered;

        if (_goldTotalPreviewLabel != null)
            _goldTotalPreviewLabel.Text = (_saveNode?.CompanyRunData?.GetCurrentPhaseGoldCost(_saveNode.TownPhaseState) ?? 0).ToString();

        foreach (var child in _gladiators?.GetChildren() ?? new Godot.Collections.Array<Node>())
        {
            if (child is RosterYardGladiator yardGladiator)
                yardGladiator.SetSalaryPreviewVisible(_goldButtonHovered, _saveNode?.TownPhaseState?.IsNight() == true);
        }

        foreach (var node in GetTree().GetNodesInGroup(DragDropTargetGroup))
        {
            if (node is TownBuilding townBuilding)
                townBuilding.SetGoldCostPreviewVisible(_goldButtonHovered);
        }
    }

    public IEnumerable<PhaseGoldCostLine> GetPhaseGoldCostLines(CompanyRunData runData, TownPhaseState phaseState)
    {
        var sourceRunData = runData ?? _saveNode?.CompanyRunData;
        if (sourceRunData?.Gladiators == null)
            yield break;

        foreach (var gladiator in sourceRunData.Gladiators)
        {
            if (gladiator != null)
                yield return new PhaseGoldCostLine(gladiator.GladiatorName, CompanyRunData.GetGladiatorSalaryGoldCost(gladiator), PhaseGoldCostTiming.NightToDay);
        }
    }

    private void RefreshGladiatorStatusContexts()
    {
        if (_gladiators == null)
            return;

        foreach (var child in _gladiators.GetChildren())
        {
            if (child is RosterYardGladiator yardGladiator)
                ApplyGladiatorStatusContext(yardGladiator);
        }
    }

    private void ApplyGladiatorStatusContext(RosterYardGladiator yardGladiator)
    {
        yardGladiator?.SetCompactStatusContext(_equipmentButtonHovered || _draggedItem != null, _gladiatorsButtonHovered);
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
