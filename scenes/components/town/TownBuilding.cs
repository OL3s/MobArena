using Godot;
using System.Collections.Generic;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Town;

[Tool]
public partial class TownBuilding : Node2D, ITownDragDropTarget, ITownHoverInfoProvider, IPhaseGoldCostSource
{
    public enum GladiatorCapacityMode
    {
        Fixed,
        LocalInputSetups
    }

    private static readonly Rect2 InteractionBounds = new(new Vector2(-75.0f, -75.0f), new Vector2(150.0f, 150.0f));
    private const ulong InputActivationDebounceMsec = 250;
    private const float ExhaustionWarningThreshold = 5f;
    private const string IdleIconPath = "res://assets/ui/gladiator_icons/idle.svg";
    private const string ExhaustionIconPath = "res://assets/ui/gladiator_icons/exhaustion.svg";
    private const string HealthIconPath = "res://assets/ui/gladiator_icons/health.svg";
    private const string CriticalRiskIconPath = "res://assets/ui/gladiator_icons/critical_risk.svg";

    private string _buildingName = "Town Building";
    private Texture2D _buildingTexture;
    private Texture2D _closedBuildingTexture;
    private Texture2D _iconTexture;
    private bool _disabled;

    [Export]
    public string BuildingName
    {
        get => _buildingName;
        set
        {
            _buildingName = value;
            RefreshVisuals();
        }
    }

    [Export]
    public Texture2D BuildingTexture
    {
        get => _buildingTexture;
        set
        {
            _buildingTexture = value;
            RefreshVisuals();
        }
    }

    [Export]
    public Texture2D ClosedBuildingTexture
    {
        get => _closedBuildingTexture;
        set
        {
            _closedBuildingTexture = value;
            RefreshVisuals();
        }
    }

    [Export]
    public Texture2D IconTexture
    {
        get => _iconTexture;
        set
        {
            _iconTexture = value;
            RefreshVisuals();
        }
    }

    [Export]
    public PackedScene SceneToOpen { get; set; }

    [Export(PropertyHint.MultilineText)]
    public string HoverDescription { get; set; }

    [Export]
    public PackedScene OverlayToOpen { get; set; }

    [Export]
    public bool Disabled
    {
        get => _disabled || IsDisabledByEmptyRoster() || IsDisabledByNight();
        set
        {
            _disabled = value;
            RefreshVisuals();
        }
    }

    [Export]
    public bool DisableWhenRosterEmpty { get; set; } = true;

    [Export]
    public bool DisableAtNight { get; set; }

    [Export]
    public string ClosedTitle { get; set; } = "Closed";

    [Export(PropertyHint.MultilineText)]
    public string ClosedMessage { get; set; }

    [Export]
    public bool HideUntilSpecialtyBuildingsUnlocked { get; set; }

    [Export]
    public bool HideWhenNoContractsCompletedAndRosterEmpty { get; set; }

    [Export]
    public string ConfirmationTitle { get; set; } = "Open Building?";

    [Export(PropertyHint.MultilineText)]
    public string ConfirmationMessage { get; set; } = "Go inside this building?";

    [Export]
    public string GoText { get; set; } = "Go";

    [Export]
    public string CancelText { get; set; } = "Cancel";

    [Export]
    public bool RequireConfirmation { get; set; } = true;

    [Export]
    public bool DebugInteraction { get; set; }

    [Export]
    public int TownDragDropPriority { get; set; }

    [Export]
    public bool SellDroppedPayloads { get; set; }

    [Export]
    public bool AssignDroppedGladiators { get; set; } = true;

    [Export]
    public GladiatorCapacityMode AssignmentCapacityMode { get; set; } = GladiatorCapacityMode.Fixed;

    [Export]
    public int MaxAssignedGladiators { get; set; } = 1;

    [Export]
    public TownAssignmentData.AssignmentLocation AssignmentLocation { get; set; } = TownAssignmentData.AssignmentLocation.Courtyard;

    [Export]
    public Godot.Collections.Array<TownDragPayloadKind> AcceptedTownDragDropKinds { get; set; } = TownDragDropRules.GetAllAcceptedKinds();

    private Area2D _interactionArea;
    private Label _nameLabel;
    private Node2D _visuals;
    private Sprite2D _buildingSprite;
    private Sprite2D _iconSprite;
    private PanelContainer _sellPreview;
    private Label _sellPreviewValueLabel;
    private PanelContainer _occupancyBadge;
    private Label _occupancyCountLabel;
    private HBoxContainer _statusWarnings;
    private CompanyRunData _runData;
    private CompanyCareerData _careerData;
    private TownPhaseState _phaseState;
    private ulong _lastInputActivationMsec;
    private bool _showCapacityDuringGladiatorDrag;
    private bool _showGoldCostPreview;
    private bool _showSalePreview;

    public string DropTargetName => string.IsNullOrWhiteSpace(BuildingName) ? "Town Building" : BuildingName;

    public int PhaseGoldCostDisplayOrder => 10;

    public string PhaseGoldCostSection => "Buildings";

    public Godot.Collections.Array<GladiatorData> AssignedGladiators => SaveNode.Get()?.CompanyRunData?.TownAssignments?.GetGladiators(AssignmentLocation) ?? new Godot.Collections.Array<GladiatorData>();

    public override void _Ready()
    {
        _interactionArea = GetNode<Area2D>("InteractionArea");
        _nameLabel = GetNode<Label>("Visuals/NamePlate/NameLabel");
        _visuals = GetNode<Node2D>("Visuals");
        _buildingSprite = GetNode<Sprite2D>("Visuals/BuildingSprite");
        _iconSprite = GetNode<Sprite2D>("Visuals/IconSprite");
        _sellPreview = GetNode<PanelContainer>("Visuals/SellPreview");
        _sellPreviewValueLabel = GetNode<Label>("Visuals/SellPreview/Row/ValueLabel");
        _occupancyBadge = GetNode<PanelContainer>("Visuals/OccupancyBadge");
        _occupancyCountLabel = GetNode<Label>("Visuals/OccupancyBadge/Row/CountLabel");
        _statusWarnings = GetNode<HBoxContainer>("Visuals/StatusWarnings");

        RefreshVisuals();

        if (Engine.IsEditorHint())
            return;

        AddToGroup(RosterYard.DragDropTargetGroup);
        AddToGroup(RosterYard.PhaseGoldCostSourceGroup);
        var saveNode = SaveNode.Get();
        _runData = saveNode?.CompanyRunData;
        _careerData = saveNode?.CompanyCareerData;
        _phaseState = saveNode?.TownPhaseState;
        if (_runData != null)
            _runData.RunChanged += RefreshBadges;
        if (_careerData != null)
            _careerData.CareerChanged += RefreshBadges;
        if (_phaseState != null)
            _phaseState.PhaseChanged += RefreshBadges;

        _interactionArea.InputPickable = true;
        _interactionArea.InputEvent += OnInteractionInputEvent;
        _interactionArea.MouseEntered += OnMouseEntered;
        _interactionArea.MouseExited += OnMouseExited;
        RefreshVisuals();
        RefreshOccupancyBadge();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshBadges;
        if (_careerData != null)
            _careerData.CareerChanged -= RefreshBadges;
        if (_phaseState != null)
            _phaseState.PhaseChanged -= RefreshBadges;
    }

    public override void _UnhandledInput(InputEvent inputEvent)
    {
        if (Disabled || !IsVisibleInTree())
            return;

        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton)
        {
            ActivateIfInside(mouseButton.Position, true);
            return;
        }

        if (inputEvent is InputEventScreenTouch { Pressed: true } screenTouch)
            ActivateIfInside(screenTouch.Position, true);
    }

    public void Activate()
    {
        if (DebugInteraction)
            GD.Print($"TownBuilding Activate: {BuildingName}, disabled={Disabled}, overlay={OverlayToOpen != null}, scene={SceneToOpen != null}");

        if (Disabled)
        {
            ShowClosedPopupIfConfigured();
            return;
        }

        if (SceneToOpen == null && OverlayToOpen == null)
            return;

        if (OverlayToOpen != null)
        {
            OpenOverlay();
            return;
        }

        if (!RequireConfirmation)
        {
            OpenScene();
            return;
        }

        var globalOverlay = GlobalOverlay.Get();
        if (globalOverlay == null)
        {
            GD.PushWarning("Could not load global overlay. Opening scene directly.");
            OpenScene();
            return;
        }

        globalOverlay.ShowGoCancelPopup(ConfirmationTitle, ConfirmationMessage, OpenScene, GoText, CancelText);
    }

    public bool CanReceiveTownDragDrop(TownDragPayload payload, Vector2 viewportPosition)
    {
        if (!this.AcceptsTownDragPayloadKind(payload))
            return false;

        if (Disabled || !IsVisibleInTree())
            return false;

        if (SellDroppedPayloads && GetSaleValue(payload) <= 0)
            return false;

        if (SellDroppedPayloads && !PayloadExistsInRunState(payload))
            return false;

        if (!SellDroppedPayloads && payload.Kind == TownDragPayloadKind.Gladiator && !CanTakeGladiator(payload.Gladiator))
            return false;

        return TownDragDropRules.IsViewportPositionInside(this, InteractionBounds, viewportPosition);
    }

    public bool CanPreviewTownDragDrop(TownDragPayload payload)
    {
        return SellDroppedPayloads
            && !Disabled
            && IsVisibleInTree()
            && this.AcceptsTownDragPayloadKind(payload)
            && GetSaleValue(payload) > 0
            && PayloadExistsInRunState(payload);
    }

    public void ReceiveTownDragDrop(TownDragPayload payload, Vector2 viewportPosition)
    {
        if (SellDroppedPayloads)
        {
            TrySellDroppedPayload(payload);
            SetTownDragDropPreview(null, viewportPosition);
            return;
        }

        if (payload.Kind == TownDragPayloadKind.Gladiator)
        {
            TryTakeGladiator(payload.Gladiator);
            return;
        }

        GD.Print(TownDragDropRules.FormatDropMessage(payload, "building", DropTargetName));
    }

    public int GetAssignedGladiatorCapacity()
    {
        return AssignmentCapacityMode switch
        {
            GladiatorCapacityMode.LocalInputSetups => Mathf.Max(0, LocalInputConfig.Get()?.ControllerSetups.Count ?? 0),
            _ => Mathf.Max(0, MaxAssignedGladiators)
        };
    }

    public bool CanTakeGladiator(GladiatorData gladiatorData)
    {
        if (!AssignDroppedGladiators || gladiatorData == null)
            return false;

        var runData = SaveNode.Get()?.CompanyRunData;
        if (runData == null || !runData.HasGladiator(gladiatorData))
            return false;

        var assignedGladiators = AssignedGladiators;
        if (assignedGladiators.Contains(gladiatorData))
            return true;

        return assignedGladiators.Count < GetAssignedGladiatorCapacity();
    }

    public bool TryTakeGladiator(GladiatorData gladiatorData)
    {
        if (Disabled)
        {
            GD.PushError($"Building assignment failed: '{DropTargetName}' is disabled.");
            return false;
        }

        if (!AssignDroppedGladiators)
        {
            GD.PushError($"Building assignment failed: '{DropTargetName}' does not assign dropped gladiators.");
            return false;
        }

        if (gladiatorData == null)
        {
            GD.PushError($"Building assignment failed: null gladiator dropped on '{DropTargetName}'.");
            return false;
        }

        var runData = SaveNode.Get()?.CompanyRunData;
        if (runData == null || !runData.HasGladiator(gladiatorData))
        {
            GD.PushError($"Building assignment failed: gladiator '{gladiatorData.GladiatorName}' is not in the active roster.");
            return false;
        }

        var assignedGladiators = AssignedGladiators;
        if (assignedGladiators.Contains(gladiatorData))
        {
            GD.Print($"Building assignment: gladiator '{gladiatorData.GladiatorName}' is already assigned to '{DropTargetName}'.");
            return true;
        }

        var capacity = GetAssignedGladiatorCapacity();
        if (assignedGladiators.Count >= capacity)
        {
            GD.PushError($"Building assignment failed: '{DropTargetName}' is full ({assignedGladiators.Count}/{capacity}).");
            return false;
        }

        if (!runData.TryAssignGladiatorToTownLocation(gladiatorData, AssignmentLocation, capacity))
            return false;

        GD.Print($"Building assignment: gladiator '{gladiatorData.GladiatorName}' assigned to '{DropTargetName}' ({AssignedGladiators.Count}/{capacity}).");
        return true;
    }

    public bool RemoveAssignedGladiator(GladiatorData gladiatorData)
    {
        if (gladiatorData == null || !HasAssignedGladiator(gladiatorData))
            return false;

        var runData = SaveNode.Get()?.CompanyRunData;
        return runData?.TryMoveGladiatorToCourtyard(gladiatorData) == true;
    }

    public void ClearAssignedGladiators()
    {
        var runData = SaveNode.Get()?.CompanyRunData;
        if (runData?.TownAssignments == null)
            return;

        var assignedGladiators = AssignedGladiators;
        while (assignedGladiators.Count > 0)
            runData.TryMoveGladiatorToCourtyard(assignedGladiators[0]);
    }

    public bool HasAssignedGladiator(GladiatorData gladiatorData)
    {
        return gladiatorData != null && AssignedGladiators?.Contains(gladiatorData) == true;
    }

    public void SetGladiatorDragCapacityPreview(bool showCapacityDuringGladiatorDrag)
    {
        if (_showCapacityDuringGladiatorDrag == showCapacityDuringGladiatorDrag)
            return;

        _showCapacityDuringGladiatorDrag = showCapacityDuringGladiatorDrag;
        RefreshOccupancyBadge();
    }

    public void SetGoldCostPreviewVisible(bool visible)
    {
        if (_showGoldCostPreview == visible)
            return;

        _showGoldCostPreview = visible;
        RefreshGoldPreview();
        RefreshOccupancyBadge();
    }

    public void ShowTownHoverInfo(TownHud hud)
    {
        hud?.ShowBuildingHoverInfo(this, IconTexture, DropTargetName, GetHoverDescription());
    }

    public void SetTownDragDropPreview(TownDragPayload? payload, Vector2 viewportPosition)
    {
        if (_sellPreview == null || _sellPreviewValueLabel == null)
            return;

        if (!SellDroppedPayloads || payload == null || !CanPreviewTownDragDrop(payload.Value))
        {
            _showSalePreview = false;
            RefreshGoldPreview();
            RefreshOccupancyBadge();
            return;
        }

        var saleValue = GetSaleValue(payload.Value);
        _sellPreviewValueLabel.Text = saleValue.ToString();
        _showSalePreview = saleValue > 0;
        RefreshGoldPreview();
        RefreshOccupancyBadge();
    }

    private bool TrySellDroppedPayload(TownDragPayload payload)
    {
        var saleValue = GetSaleValue(payload);
        var globalOverlay = GlobalOverlay.Get();
        if (globalOverlay == null)
        {
            GD.PushError($"Market sale failed: GlobalOverlay missing while trying to confirm sale of {payload.Kind} '{payload.GetDebugName()}'.");
            return false;
        }

        globalOverlay.ShowGoCancelPopup(
            "Confirm Sale",
            $"Are you sure you want to sell {payload.GetDebugName()} for {saleValue} gold?",
            () => ExecuteConfirmedSale(payload, saleValue),
            "Sell",
            "Cancel",
            pauseGameUntilClosed: true);

        return true;
    }

    private static void ExecuteConfirmedSale(TownDragPayload payload, int expectedSaleValue)
    {
        var saveNode = SaveNode.Get();
        var runData = saveNode?.CompanyRunData;
        if (runData == null)
        {
            GD.PushError($"Market sale failed: company run data missing for {payload.Kind} '{payload.GetDebugName()}'.");
            return;
        }

        var currentSaleValue = GetSaleValue(payload);
        if (currentSaleValue != expectedSaleValue)
            GD.PushError($"Market sale warning: {payload.Kind} '{payload.GetDebugName()}' sale value changed from {expectedSaleValue} to {currentSaleValue} before confirmation.");

        var sold = payload.Kind switch
        {
            TownDragPayloadKind.Item => runData.TrySellItem(payload.Item, saveNode.CompanyCareerData),
            TownDragPayloadKind.Gladiator => runData.TrySellGladiator(payload.Gladiator, saveNode.CompanyCareerData),
            _ => false
        };

        if (sold)
            GD.Print($"Market sale: sold {payload.Kind} '{payload.GetDebugName()}' for {currentSaleValue} gold.");
    }

    private static int GetSaleValue(TownDragPayload payload)
    {
        var runData = SaveNode.Get()?.CompanyRunData;
        if (runData == null)
            return 0;

        return payload.Kind switch
        {
            TownDragPayloadKind.Item => runData.GetSaleValue(payload.Item),
            TownDragPayloadKind.Gladiator => runData.GetSaleValue(payload.Gladiator),
            _ => 0
        };
    }

    private static bool PayloadExistsInRunState(TownDragPayload payload)
    {
        var runData = SaveNode.Get()?.CompanyRunData;
        if (runData == null)
            return false;

        return payload.Kind switch
        {
            TownDragPayloadKind.Item => runData.HasItem(payload.Item),
            TownDragPayloadKind.Gladiator => runData.HasGladiator(payload.Gladiator),
            _ => false
        };
    }

    private void OpenOverlay()
    {
        var globalOverlay = GlobalOverlay.Get();
        if (globalOverlay == null)
        {
            GD.PushWarning($"TownBuilding overlay failed: GlobalOverlay missing for {BuildingName}.");
            return;
        }

        if (DebugInteraction)
            GD.Print($"TownBuilding opening overlay: {BuildingName}");

        globalOverlay.AddOverlay(OverlayToOpen);
    }

    private void ShowClosedPopupIfConfigured()
    {
        if (string.IsNullOrWhiteSpace(ClosedMessage))
            return;

        GlobalOverlay.Get()?.ShowBlurredPopup(
            string.IsNullOrWhiteSpace(ClosedTitle) ? "Closed" : ClosedTitle,
            ClosedMessage,
            IconTexture);
    }

    private void OnInteractionInputEvent(Node viewport, InputEvent inputEvent, long shapeIdx)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
        {
            if (DebugInteraction)
                GD.Print($"TownBuilding Area2D click: {BuildingName}");

            GetViewport()?.SetInputAsHandled();
            ActivateFromInput();
            return;
        }

        if (inputEvent is InputEventScreenTouch { Pressed: true })
        {
            if (DebugInteraction)
                GD.Print($"TownBuilding Area2D touch: {BuildingName}");

            GetViewport()?.SetInputAsHandled();
            ActivateFromInput();
        }
    }

    private void OnMouseEntered()
    {
        if (Disabled)
            return;

        _visuals.Scale = new Vector2(1.04f, 1.04f);
        ShowTownHoverInfo(GetTownHud());
    }

    private void OnMouseExited()
    {
        _visuals.Scale = Vector2.One;
        GetTownHud()?.HideHoverInfo(this);
    }

    private void OpenScene()
    {
        GlobalOverlay.Get()?.CloseAllOverlaysImmediate();
        GetTree().ChangeSceneToPacked(SceneToOpen);
    }

    private void ActivateIfInside(Vector2 viewportPosition, bool fromInput = false)
    {
        var worldPosition = GetCanvasTransform().AffineInverse() * viewportPosition;
        var localPosition = ToLocal(worldPosition);
        if (!InteractionBounds.HasPoint(localPosition))
            return;

        if (DebugInteraction)
            GD.Print($"TownBuilding fallback hit: {BuildingName}, viewport={viewportPosition}, local={localPosition}");

        GetViewport()?.SetInputAsHandled();
        if (fromInput)
            ActivateFromInput();
        else
            Activate();
    }

    private void ActivateFromInput()
    {
        var nowMsec = Time.GetTicksMsec();
        if (nowMsec - _lastInputActivationMsec < InputActivationDebounceMsec)
            return;

        _lastInputActivationMsec = nowMsec;
        Activate();
    }

    private void RefreshVisuals()
    {
        if (!IsInsideTree())
            return;

        _nameLabel ??= GetNodeOrNull<Label>("Visuals/NamePlate/NameLabel");
        _buildingSprite ??= GetNodeOrNull<Sprite2D>("Visuals/BuildingSprite");
        _iconSprite ??= GetNodeOrNull<Sprite2D>("Visuals/IconSprite");

        if (_nameLabel != null)
            _nameLabel.Text = string.IsNullOrWhiteSpace(BuildingName) ? "Town Building" : BuildingName;

        var disabled = Disabled;
        Visible = !ShouldHideBuilding();
        if (!Visible)
            GetTownHud()?.HideHoverInfo(this);

        var visibleBuildingTexture = disabled && ClosedBuildingTexture != null
            ? ClosedBuildingTexture
            : BuildingTexture;
        if (_buildingSprite != null && visibleBuildingTexture != null)
            _buildingSprite.Texture = visibleBuildingTexture;

        if (_iconSprite != null && IconTexture != null)
            _iconSprite.Texture = IconTexture;

        if (disabled)
            GetTownHud()?.HideHoverInfo(this);

        Modulate = disabled ? new Color(0.55f, 0.55f, 0.55f, 1.0f) : Colors.White;
        RefreshOccupancyBadge();
    }

    private void RefreshOccupancyBadge()
    {
        if (_occupancyBadge == null || _occupancyCountLabel == null || Engine.IsEditorHint())
            return;

        var capacity = GetAssignedGladiatorCapacity();
        var count = AssignedGladiators.Count;
        var shouldShowCapacityHint = _showCapacityDuringGladiatorDrag && count <= 0;
        _occupancyBadge.Visible = !Disabled && !_showSalePreview && !IsGoldCostPreviewVisible() && AssignDroppedGladiators && capacity > 0 && !SellDroppedPayloads && (count > 0 || shouldShowCapacityHint);
        _occupancyCountLabel.Text = $"{count}/{capacity}";
        RefreshStatusWarnings();
    }

    private void RefreshBadges()
    {
        RefreshVisuals();
        RefreshGoldPreview();
        RefreshOccupancyBadge();
    }

    private bool IsDisabledByEmptyRoster()
    {
        if (!DisableWhenRosterEmpty || SellDroppedPayloads || Engine.IsEditorHint())
            return false;

        var runData = _runData ?? SaveNode.Get()?.CompanyRunData;
        return runData?.Gladiators == null || runData.Gladiators.Count <= 0;
    }

    private bool IsDisabledByNight()
    {
        if (!DisableAtNight || Engine.IsEditorHint())
            return false;

        return (_phaseState ?? SaveNode.Get()?.TownPhaseState)?.IsNight() == true;
    }

    private bool ShouldHideBuilding()
    {
        if (Engine.IsEditorHint())
            return false;

        var saveNode = SaveNode.Get();
        var runData = _runData ?? saveNode?.CompanyRunData;
        if (HideUntilSpecialtyBuildingsUnlocked)
            return !saveNode.HasReachedSpecialtyBuildingsForProgression && runData?.HasUnlockedSpecialtyBuildings != true;

        if (!HideWhenNoContractsCompletedAndRosterEmpty)
            return false;

        if (saveNode.HasCompletedContractsForProgression)
            return false;

        return runData?.Gladiators == null || runData.Gladiators.Count <= 0;
    }

    private void RefreshGoldPreview()
    {
        if (_sellPreview == null || _sellPreviewValueLabel == null || Engine.IsEditorHint())
            return;

        if (_showSalePreview)
        {
            _sellPreview.Visible = true;
            return;
        }

        var cost = GetPhaseGoldCost();
        _sellPreviewValueLabel.Text = cost.ToString();
        _sellPreview.Visible = _showGoldCostPreview && cost > 0;
    }

    private bool IsGoldCostPreviewVisible()
    {
        return _showGoldCostPreview && GetPhaseGoldCost() > 0;
    }

    private int GetPhaseGoldCost()
    {
        if (SellDroppedPayloads)
            return 0;

        var saveNode = SaveNode.Get();
        var sourceRunData = _runData ?? saveNode?.CompanyRunData;
        var phaseState = saveNode?.TownPhaseState;
        if (sourceRunData == null)
            return 0;

        return sourceRunData.GetPhaseBuildingGoldCost(AssignmentLocation)
            + sourceRunData.GetAssignedGladiatorsSalaryGoldCost(AssignedGladiators, phaseState);
    }

    public IEnumerable<PhaseGoldCostLine> GetPhaseGoldCostLines(CompanyRunData runData, TownPhaseState phaseState)
    {
        if (SellDroppedPayloads)
            yield break;

        var sourceRunData = runData ?? _runData ?? SaveNode.Get()?.CompanyRunData;
        yield return new PhaseGoldCostLine(DropTargetName, sourceRunData?.GetPhaseBuildingGoldCost(AssignmentLocation) ?? 0, PhaseGoldCostTiming.Both);
    }

    private void RefreshStatusWarnings()
    {
        if (_statusWarnings == null || Engine.IsEditorHint())
            return;

        foreach (var child in _statusWarnings.GetChildren())
            child.QueueFree();

        if (!AssignDroppedGladiators || SellDroppedPayloads)
        {
            _statusWarnings.Visible = false;
            return;
        }

        var addedWarning = false;
        foreach (var gladiator in AssignedGladiators)
        {
            if (gladiator == null)
                continue;

            if (_runData?.IsGladiatorIdleInTownLocation(gladiator, AssignmentLocation) == true)
            {
                AddStatusWarningIcon(IdleIconPath, $"{gladiator.GladiatorName} is assigned here but has no work this phase");
                addedWarning = true;
                continue;
            }

            var riskStatus = GetRiskStatus(gladiator);
            if (riskStatus == GladiatorRiskStatus.None)
                continue;

            var (iconPath, tooltipText) = riskStatus switch
            {
                GladiatorRiskStatus.Critical => (CriticalRiskIconPath, $"{gladiator.GladiatorName} is exhausted and low health"),
                GladiatorRiskStatus.Exhausted => (ExhaustionIconPath, $"{gladiator.GladiatorName} is exhausted"),
                GladiatorRiskStatus.LowHealth => (HealthIconPath, $"{gladiator.GladiatorName} is low health"),
                _ => (string.Empty, string.Empty)
            };
            AddStatusWarningIcon(iconPath, tooltipText);
            addedWarning = true;
        }

        _statusWarnings.Visible = addedWarning;
    }

    private void AddStatusWarningIcon(string texturePath, string tooltipText)
    {
        _statusWarnings.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(20, 20),
            Texture = ResourceLoader.Load<Texture2D>(texturePath),
            TooltipText = tooltipText,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });
    }

    private static GladiatorRiskStatus GetRiskStatus(GladiatorData gladiator)
    {
        if (gladiator == null)
            return GladiatorRiskStatus.None;

        var warningRatio = SaveNode.Get()?.SettingsConfig?.LowHealthWarningRatio ?? 0.6f;
        return gladiator.GetRiskStatus(ExhaustionWarningThreshold, warningRatio);
    }

    private string GetHoverDescription()
    {
        if (!string.IsNullOrWhiteSpace(HoverDescription))
            return HoverDescription;

        if (SellDroppedPayloads)
            return "Sell unwanted gladiators and equipment for gold.";

        if (AssignDroppedGladiators)
            return $"Drag gladiators here to assign them. Capacity: {AssignedGladiators.Count}/{GetAssignedGladiatorCapacity()}.";

        return ConfirmationMessage;
    }

    private TownHud GetTownHud()
    {
        return GetTree()?.GetFirstNodeInGroup("town_hover_info") as TownHud;
    }
}
