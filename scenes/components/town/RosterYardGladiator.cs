using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Town;

public partial class RosterYardGladiator : Node2D
{
    private const float DisplayHeight = 72f;
    private const float RiskWarningThreshold = 5f;

    private bool _displayDetail;
    private bool _isHovered;
    private bool _isSelected;

	private GladiatorData _gladiatorData;
	private ColorRect _background;
	private Sprite2D _portrait;
    private Area2D _interactionArea;
    private Label _nameLabel;
    private HBoxContainer _riskWarnings;
    private TextureRect _provisionsWarningIcon;
    private TextureRect _exhaustionWarningIcon;
    private VBoxContainer _detailRows;
    private ProgressBar _healthBar;
    private ProgressBar _provisionsBar;
    private ProgressBar _exhaustionBar;

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

    public bool IsSelected => _isSelected;

	public override void _Ready()
	{
		_background = GetNode<ColorRect>("Background");
		_portrait = GetNode<Sprite2D>("Portrait");
        _interactionArea = GetNode<Area2D>("InteractionArea");
        _nameLabel = GetNode<Label>("Name");
        _riskWarnings = GetNode<HBoxContainer>("RiskWarnings");
        _provisionsWarningIcon = GetNode<TextureRect>("RiskWarnings/ProvisionsIcon");
        _exhaustionWarningIcon = GetNode<TextureRect>("RiskWarnings/ExhaustionIcon");
        _detailRows = GetNode<VBoxContainer>("Details");
		_healthBar = GetNode<ProgressBar>("Details/HealthRow/Bar");
		_provisionsBar = GetNode<ProgressBar>("Details/ProvisionsRow/Bar");
		_exhaustionBar = GetNode<ProgressBar>("Details/ExhaustionRow/Bar");

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

    public void Configure(GladiatorData gladiatorData)
    {
        GladiatorData = gladiatorData;
    }

    private void RefreshPortrait()
    {
        if (!IsNodeReady() || _portrait == null || _gladiatorData == null)
            return;

        var texture = _gladiatorData.GetPortraitTexture();
        _portrait.Texture = texture;

        if (texture != null && texture.GetHeight() > 0)
            _portrait.Scale = Vector2.One * (DisplayHeight / texture.GetHeight());
    }

    private void RefreshLabels()
    {
        if (!IsNodeReady() || _gladiatorData == null)
            return;

        _nameLabel.Text = _gladiatorData.GladiatorName;
        RefreshRiskWarnings();
        ConfigureBar(_healthBar, _gladiatorData.Health, _gladiatorData.MaxHealth);
        ConfigureBar(_provisionsBar, _gladiatorData.Provisions, 10f);
        ConfigureBar(_exhaustionBar, _gladiatorData.Exhaustion, 10f);
        RefreshDetails();
    }

    private void RefreshRiskWarnings()
    {
        if (_riskWarnings == null || _gladiatorData == null)
            return;

        _provisionsWarningIcon.Visible = _gladiatorData.Provisions < RiskWarningThreshold;
        _exhaustionWarningIcon.Visible = _gladiatorData.Exhaustion < RiskWarningThreshold;
        _riskWarnings.Visible = _provisionsWarningIcon.Visible || _exhaustionWarningIcon.Visible;
    }

    private void RefreshDetails()
    {
        DisplayDetail = _isHovered || _isSelected;
    }

    private void OnMouseEntered()
    {
        _isHovered = true;
        RefreshDetails();
    }

    private void OnMouseExited()
    {
        _isHovered = false;
        RefreshDetails();
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

    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        RefreshDetails();
    }

    public void SetDragHidden(bool dragHidden)
    {
        Visible = !dragHidden;
    }

    private void SetDisplayDetail(bool displayDetail)
    {
        _displayDetail = displayDetail;

        if (!IsNodeReady())
            return;

		if (_detailRows != null)
			_detailRows.Visible = _displayDetail;

		if (_background != null)
			_background.Visible = _displayDetail;
	}

    private static void ConfigureBar(ProgressBar bar, float value, float maxValue)
    {
        if (bar == null)
            return;

        bar.MaxValue = Mathf.Max(1f, maxValue);
        bar.Value = Mathf.Clamp(value, 0f, maxValue);
    }
}
