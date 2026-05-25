using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Town;

public partial class RosterYardGladiator : Node2D, ITownDragDropTarget, ITownHoverInfoProvider
{
    private const float DisplayHeight = 72f;
    private const float HandDisplayHeight = 14f;
    private const float TownHeldDisplayScale = DisplayHeight / 96f;
    private const float DefaultHeldItemDisplayHeight = 48f;
    private const float RiskWarningThreshold = 5f;
    private static readonly Vector2 BodyLocalPosition = new(0f, -21f);
    private static readonly Vector2 LeftHandLocalPosition = new(-20f, -15f);
    private static readonly Vector2 RightHandLocalPosition = new(20f, -15f);
    private static readonly Vector2 MainHandItemLocalPosition = new(9f, -1f);
    private static readonly Vector2 OffHandItemLocalPosition = new(-9f, -1f);
    private static readonly Rect2 DropBounds = new(new Vector2(-56f, -68f), new Vector2(112f, 136f));

    private readonly Godot.Collections.Array<TownDragPayloadKind> _acceptedTownDragDropKinds = TownDragDropRules.GetAllAcceptedKinds();

    private bool _displayDetail;
    private bool _isHovered;
    private bool _isDragPreview;
    private bool _showCompactEquipment;
    private bool _showCompactHealthBar;
    private Vector2 _lookDirection = Vector2.Right;

	private GladiatorData _gladiatorData;
	private ColorRect _background;
	private Sprite2D _portrait;
    private Sprite2D _armor;
    private Sprite2D _leftHand;
    private Sprite2D _rightHand;
    private Sprite2D _mainHandItem;
    private Sprite2D _offHandItem;
    private Area2D _interactionArea;
    private Label _nameLabel;
    private HBoxContainer _riskWarnings;
    private TextureRect _exhaustionWarningIcon;
    private VBoxContainer _detailRows;
    private VBoxContainer _compactStatus;
    private TextureRect _compactMainItemIcon;
    private TextureRect _compactArmorIcon;
    private TextureRect _compactOffItemIcon;
    private TextureRect _compactExhaustionIcon;
    private TextureRect _compactLowHealthIcon;
    private TextureRect _compactCriticalRiskIcon;
    private HBoxContainer _compactHealthRow;
    private VitalProgressBar _compactHealthBar;
    private HBoxContainer _compactExhaustionRow;
    private VitalProgressBar _compactExhaustionBar;
    private PanelContainer _salaryPreview;
    private Label _salaryPreviewLabel;

    [Signal]
    public delegate void PressedEventHandler(RosterYardGladiator gladiator);

    [Signal]
    public delegate void PointerPressedEventHandler(RosterYardGladiator gladiator, Vector2 viewportPosition);

    public GladiatorData GladiatorData
    {
        get => _gladiatorData;
        private set
        {
            _gladiatorData = value;
            Name = string.IsNullOrWhiteSpace(_gladiatorData?.GladiatorName)
                ? "RosterYardGladiator"
                : $"{_gladiatorData.GladiatorName}YardGladiator";

            RefreshPortrait();
            RefreshLabels();
        }
    }

    [Export]
    public bool DisplayDetail
    {
        get => _displayDetail;
        private set => SetDisplayDetail(value);
    }

    public string DropTargetName => _gladiatorData?.GladiatorName ?? "Town Gladiator";

    public int TownDragDropPriority => 10;

    public Godot.Collections.Array<TownDragPayloadKind> AcceptedTownDragDropKinds => _acceptedTownDragDropKinds;

	public override void _Ready()
	{
		AddToGroup(RosterYard.DragDropTargetGroup);
		AddToGroup("town_roster_gladiators");
		_background = GetNode<ColorRect>("Background");
		_portrait = GetNode<Sprite2D>("Portrait");
        _armor = GetNode<Sprite2D>("Armor");
        _leftHand = GetNode<Sprite2D>("LeftHand");
        _rightHand = GetNode<Sprite2D>("RightHand");
        _mainHandItem = GetNode<Sprite2D>("RightHand/MainHandItem");
        _offHandItem = GetNode<Sprite2D>("LeftHand/OffHandItem");
        _interactionArea = GetNode<Area2D>("InteractionArea");
        _nameLabel = GetNode<Label>("Name");
        _riskWarnings = GetNode<HBoxContainer>("RiskWarnings");
        _exhaustionWarningIcon = GetNode<TextureRect>("RiskWarnings/ExhaustionIcon");
        _detailRows = GetNode<VBoxContainer>("Details");
        _compactStatus = GetNode<VBoxContainer>("CompactStatus");
        _compactMainItemIcon = GetNode<TextureRect>("CompactStatus/StatusRow/MainItemIcon");
        _compactArmorIcon = GetNode<TextureRect>("CompactStatus/StatusRow/ArmorIcon");
        _compactOffItemIcon = GetNode<TextureRect>("CompactStatus/StatusRow/OffItemIcon");
        _compactExhaustionIcon = GetNode<TextureRect>("CompactStatus/StatusRow/ExhaustionIcon");
        _compactLowHealthIcon = GetNode<TextureRect>("CompactStatus/StatusRow/LowHealthIcon");
        _compactCriticalRiskIcon = GetNode<TextureRect>("CompactStatus/StatusRow/CriticalRiskIcon");
        _compactHealthRow = GetNode<HBoxContainer>("CompactStatus/CompactHealthRow");
        _compactHealthBar = GetNode<VitalProgressBar>("CompactStatus/CompactHealthRow/Bar");
        _compactExhaustionRow = GetNode<HBoxContainer>("CompactStatus/CompactExhaustionRow");
        _compactExhaustionBar = GetNode<VitalProgressBar>("CompactStatus/CompactExhaustionRow/Bar");
        _salaryPreview = GetNode<PanelContainer>("SalaryPreview");
        _salaryPreviewLabel = GetNode<Label>("SalaryPreview/Row/SalaryLabel");
        GetNodeOrNull<Control>("Details/EquipmentRow")?.Hide();
        GetNodeOrNull<Control>("Details/HealthRow")?.Hide();
        GetNodeOrNull<Control>("Details/ExhaustionRow")?.Hide();

        _interactionArea.MouseEntered += OnMouseEntered;
        _interactionArea.MouseExited += OnMouseExited;
        _interactionArea.InputEvent += OnInputEvent;

        RefreshPortrait();
        RefreshLabels();
    }

    public override void _ExitTree()
    {
        if (_interactionArea == null)
            return;

        _interactionArea.MouseEntered -= OnMouseEntered;
        _interactionArea.MouseExited -= OnMouseExited;
        _interactionArea.InputEvent -= OnInputEvent;
    }

    public override void _Process(double delta)
    {
        ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
    }

    public void Configure(GladiatorData gladiatorData)
    {
        GladiatorData = gladiatorData;
    }

    public void SetDragPreviewMode(bool isDragPreview)
    {
        _isDragPreview = isDragPreview;

        if (!IsNodeReady())
            return;

        if (_interactionArea != null)
        {
            _interactionArea.InputPickable = !isDragPreview;
            _interactionArea.Monitoring = !isDragPreview;
        }

        if (_nameLabel != null)
            _nameLabel.Visible = !isDragPreview;
        if (_riskWarnings != null)
            _riskWarnings.Visible = false;
        if (_detailRows != null)
            _detailRows.Visible = false;
        if (_compactStatus != null)
            _compactStatus.Visible = false;
        if (_salaryPreview != null)
            _salaryPreview.Visible = false;
    }

    private void RefreshPortrait()
    {
        if (!IsNodeReady() || _portrait == null || _gladiatorData == null)
            return;

        ApplyLookVisual(_portrait, _gladiatorData.GetBodyForwardTexture(), _gladiatorData.GetBodyBackTexture(), DisplayHeight, BodyLocalPosition);
        ApplyArmorVisual();
        RefreshHandVisuals();
    }

    private void ApplyArmorVisual()
    {
        var armor = _gladiatorData?.Equipment?.Armor;
        ApplyLookVisual(_armor, armor?.ArmorForwardTexture, armor?.ArmorBackTexture, (armor?.GetArmorDisplayHeight(DisplayHeight) ?? DisplayHeight) * (DisplayHeight / 96f), BodyLocalPosition);
        if (_armor != null && armor != null)
            _armor.Offset = armor.GetArmorTextureOffset();
    }

    private void ApplyLookVisual(Sprite2D sprite, Texture2D frontTexture, Texture2D backTexture, float displayHeight, Vector2 localPosition)
    {
        if (sprite == null)
            return;

        var texture = _lookDirection.Y < 0f
            ? backTexture ?? frontTexture
            : frontTexture ?? backTexture;

        if (texture == null)
        {
            sprite.Hide();
            return;
        }

        var xSign = GetVisualXSign();
        sprite.Show();
        sprite.Texture = texture;
        sprite.Position = new Vector2(localPosition.X * xSign, localPosition.Y);

        if (texture.GetHeight() > 0)
        {
            var scale = displayHeight / texture.GetHeight();
            sprite.Scale = new Vector2(scale * xSign, scale);
        }
    }

    private void RefreshHandVisuals()
    {
        if (_gladiatorData?.UsesSeparatedHands() != true)
        {
            _leftHand?.Hide();
            _rightHand?.Hide();
            return;
        }

        var handTexture = _gladiatorData.GetHandTexture();
        ApplyDirectionalVisual(_leftHand, handTexture, HandDisplayHeight, LeftHandLocalPosition);
        ApplyDirectionalVisual(_rightHand, handTexture, HandDisplayHeight, RightHandLocalPosition);
        ApplyHandDrawOrder();

        var equipment = _gladiatorData.Equipment;
        ApplyHeldVisual(_mainHandItem, equipment?.MainHand, MainHandItemLocalPosition);
        ApplyHeldVisual(_offHandItem, equipment?.OffHand, OffHandItemLocalPosition);
        if (_mainHandItem != null && equipment?.MainHand != null)
            _mainHandItem.RotationDegrees = equipment.MainHand.GetHeldRotationDegrees();
        if (_offHandItem != null && equipment?.OffHand != null)
            _offHandItem.RotationDegrees = equipment.OffHand.GetHeldRotationDegrees();
    }

    private static void ApplyHeldVisual(Sprite2D sprite, ItemData item, Vector2 localPosition)
    {
        ApplyLocalVisual(
            sprite,
            item?.GetHeldTexture(),
            (item?.GetHeldDisplayHeight(DefaultHeldItemDisplayHeight) ?? DefaultHeldItemDisplayHeight) * TownHeldDisplayScale,
            localPosition,
            item?.GetHeldTextureOffset() ?? Vector2.Zero);
    }

    private static void ApplyLocalVisual(Sprite2D sprite, Texture2D texture, float displayHeight, Vector2 localPosition, Vector2 textureOffset)
    {
        if (sprite == null)
            return;

        if (texture == null)
        {
            sprite.Hide();
            return;
        }

        sprite.Show();
        sprite.Centered = false;
        sprite.Texture = texture;
        sprite.Position = localPosition;
        sprite.Offset = textureOffset;
        sprite.RotationDegrees = 0f;

        if (texture.GetHeight() > 0)
            sprite.Scale = Vector2.One * (displayHeight / texture.GetHeight());
    }

    private void ApplyDirectionalVisual(Sprite2D sprite, Texture2D texture, float displayHeight, Vector2 localPosition)
    {
        if (sprite == null)
            return;

        if (texture == null)
        {
            sprite.Hide();
            return;
        }

        var xSign = GetVisualXSign();
        sprite.Show();
        sprite.Texture = texture;
        sprite.Position = new Vector2(localPosition.X * xSign, localPosition.Y);

        if (texture.GetHeight() > 0)
        {
            var scale = displayHeight / texture.GetHeight();
            sprite.Scale = new Vector2(scale * xSign, scale);
        }
    }

    private float GetVisualXSign()
    {
        if (_lookDirection.X > 0f)
            return 1f;
        if (_lookDirection.X < 0f)
            return -1f;

        return 1f;
    }

    private void ApplyHandDrawOrder()
    {
        var handZIndex = _lookDirection.Y < 0f ? -2 : 1;
        if (_leftHand != null)
            _leftHand.ZIndex = handZIndex;
        if (_rightHand != null)
            _rightHand.ZIndex = handZIndex;
        if (_mainHandItem != null)
            _mainHandItem.ZIndex = 1;
        if (_offHandItem != null)
            _offHandItem.ZIndex = 1;
    }

    private void RefreshLabels()
    {
        if (!IsNodeReady() || _gladiatorData == null)
            return;

        _nameLabel.Text = _gladiatorData.GladiatorName;
        RefreshRiskWarnings();
        RefreshDetails();
        RefreshCompactStatus();
    }

    private void RefreshRiskWarnings()
    {
        if (_riskWarnings == null || _gladiatorData == null)
            return;

        _exhaustionWarningIcon.Visible = _gladiatorData.Exhaustion < RiskWarningThreshold;
        _riskWarnings.Visible = false;
    }

    private void RefreshDetails()
    {
        DisplayDetail = _isHovered;
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        RefreshDetails();
        ShowTownHoverInfo(GetTownHud());
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        RefreshDetails();
        GetTownHud()?.HideHoverInfo(this);
    }

    private void OnInputEvent(Node viewport, InputEvent inputEvent, long shapeIdx)
    {
        Vector2 viewportPosition;
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } mouseButton)
            viewportPosition = mouseButton.Position;
        else if (inputEvent is InputEventScreenTouch { Pressed: true } screenTouch)
            viewportPosition = screenTouch.Position;
        else
            return;

        GetViewport()?.SetInputAsHandled();
        EmitSignal(SignalName.PointerPressed, this, viewportPosition);
        EmitSignal(SignalName.Pressed, this);
    }

    public void SetDragHidden(bool dragHidden)
    {
        Visible = !dragHidden;
    }

    public void SetCompactStatusContext(bool showEquipment, bool showHealthBar)
    {
        if (_showCompactEquipment == showEquipment && _showCompactHealthBar == showHealthBar)
            return;

        _showCompactEquipment = showEquipment;
        _showCompactHealthBar = showHealthBar;
        RefreshCompactStatus();
    }

    public void SetSalaryPreviewVisible(bool visible, bool salaryDue)
    {
        if (_salaryPreview == null || _salaryPreviewLabel == null)
            return;

        var salary = salaryDue ? CompanyRunData.GetGladiatorSalaryGoldCost(_gladiatorData) : 0;
        _salaryPreviewLabel.Text = salary.ToString();
        _salaryPreview.Visible = visible && salary > 0;
    }

    public void ShowTownHoverInfo(TownHud hud)
    {
        hud?.ShowGladiatorHoverInfo(this, _gladiatorData);
    }

    public bool CanReceiveTownDragDrop(TownDragPayload payload, Vector2 viewportPosition)
    {
        if (_isDragPreview)
            return false;

        if (!this.AcceptsTownDragPayloadKind(payload))
            return false;

        if (!TownDragDropRules.IsViewportPositionInside(this, DropBounds, viewportPosition))
            return false;

        return payload.Kind != TownDragPayloadKind.Gladiator || payload.Gladiator != _gladiatorData;
    }

    public bool CanPreviewTownDragDrop(TownDragPayload payload)
    {
        return false;
    }

    public void ReceiveTownDragDrop(TownDragPayload payload, Vector2 viewportPosition)
    {
        if (payload.Kind == TownDragPayloadKind.Item)
        {
            TryEquipDroppedItem(payload);
            return;
        }

        GD.Print(TownDragDropRules.FormatDropMessage(payload, "gladiator", DropTargetName));
    }

    public void SetTownDragDropPreview(TownDragPayload? payload, Vector2 viewportPosition)
    {
    }

    private void SetDisplayDetail(bool displayDetail)
    {
        _displayDetail = displayDetail;

        if (!IsNodeReady())
            return;

		if (_detailRows != null)
			_detailRows.Visible = _displayDetail;

		if (_background != null)
			_background.Visible = false;
	}

    private void RefreshCompactStatus()
    {
        if (!IsNodeReady() || _compactStatus == null || _gladiatorData == null)
            return;

        var equipment = _gladiatorData.Equipment;
        SetEquipmentIcon(_compactMainItemIcon, equipment?.MainHand, _showCompactEquipment);
        SetEquipmentIcon(_compactArmorIcon, equipment?.Armor, _showCompactEquipment);
        SetEquipmentIcon(_compactOffItemIcon, equipment?.OffHand, _showCompactEquipment);
        RefreshHandVisuals();

        var riskStatus = GetRiskStatus(_gladiatorData);
        _compactExhaustionIcon.Visible = riskStatus == GladiatorRiskStatus.Exhausted;
        _compactLowHealthIcon.Visible = riskStatus == GladiatorRiskStatus.LowHealth;
        _compactCriticalRiskIcon.Visible = riskStatus == GladiatorRiskStatus.Critical;

        _compactHealthRow.Visible = _showCompactHealthBar;
        _compactHealthBar.ShowHealth(_gladiatorData);
        _compactExhaustionRow.Visible = _showCompactHealthBar;
        _compactExhaustionBar.ShowExhaustion(_gladiatorData.Exhaustion);
        _compactStatus.Visible = _showCompactEquipment || _showCompactHealthBar || riskStatus != GladiatorRiskStatus.None;
    }

    private static void SetEquipmentIcon(TextureRect icon, ItemData item, bool showEquipment)
    {
        if (icon == null)
            return;

        icon.Texture = item?.UiIcon;
        icon.TooltipText = item?.DisplayName ?? "Empty equipment slot";
        icon.Visible = showEquipment;
        icon.Modulate = item == null ? new Color(1f, 1f, 1f, 0.28f) : Colors.White;
    }

    private static GladiatorRiskStatus GetRiskStatus(GladiatorData gladiatorData)
    {
        if (gladiatorData == null)
            return GladiatorRiskStatus.None;

        var warningRatio = SaveNode.Get()?.SettingsConfig?.LowHealthWarningRatio ?? 0.6f;
        return gladiatorData.GetRiskStatus(RiskWarningThreshold, warningRatio);
    }

    private void TryEquipDroppedItem(TownDragPayload payload)
    {
        var runData = SaveNode.Get()?.CompanyRunData;
        if (runData == null)
        {
            GD.PushError($"Drop item failed: company run data missing for item '{payload.Item?.DisplayName ?? "null"}'.");
            return;
        }

        if (runData.TryEquipItemOnGladiator(_gladiatorData, payload.Item))
            GD.Print($"Drop equip: equipped item '{payload.Item?.DisplayName ?? "null"}' on gladiator '{DropTargetName}'.");
    }

    private TownHud GetTownHud()
    {
        return GetTree()?.GetFirstNodeInGroup("town_hover_info") as TownHud;
    }
}
