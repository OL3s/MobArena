using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scenes.Components.Town;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Panels;

public partial class BuildingOverlayPanel : Control, IUpgradeable
{
    private const float PanelWidthRatio = 0.68f;
    private const int PanelMinWidth = 420;
    private const int PanelMaxWidth = 820;
    private const float ExhaustionWarningThreshold = 5f;
    private const string IdleIconPath = "res://assets/ui/gladiator_icons/idle.svg";
    private const string ExhaustionIconPath = "res://assets/ui/gladiator_icons/exhaustion.svg";
    private const string HealthIconPath = "res://assets/ui/gladiator_icons/health.svg";
    private const string CriticalRiskIconPath = "res://assets/ui/gladiator_icons/critical_risk.svg";
    private const string AttributeProgressScenePath = "res://scenes/components/ui/AttributeProgressDisplay.tscn";
    private static readonly Vector2 AttributeProgressMinimumSize = new(120f, 32f);

    [Export]
    public string Title { get; set; } = "Building";

    [Export(PropertyHint.MultilineText)]
    public string Body { get; set; } = "This building is not implemented yet.";

    [Export]
    public Texture2D IconTexture { get; set; }

    [Export]
    public bool ShowAssignedGladiators { get; set; }

    [Export]
    public TownAssignmentData.AssignmentLocation AssignmentLocation { get; set; } = TownAssignmentData.AssignmentLocation.Courtyard;

    [Export]
    public bool IsUpgradeableBuilding { get; set; }

    [Export]
    public int MaxUpgradeLevel { get; set; } = 3;

    [Export]
    public PackedScene GladiatorRowScene { get; set; }

    [Export]
    public PackedScene AttributeBarScene { get; set; }

    [Export]
    public PackedScene RiskIconScene { get; set; }

    public int UpgradeLevel => _runData?.GetBuildingUpgradeLevel(AssignmentLocation) ?? 0;

    private PanelContainer _panel;
    private TextureRect _icon;
    private Label _title;
    private Button _upgradeButton;
    private RichTextLabel _body;
    private HBoxContainer _workRow;
    private VBoxContainer _gladiatorDetails;
    private VBoxContainer _modeButtons;
    private HBoxContainer _assignedGladiatorsRow;
    private Button _assignedGladiatorsGrabButton;
    private HBoxContainer _assignedGladiators;
    private Button _closeButton;
    private CompanyRunData _runData;
    private bool _refreshingModeButtons;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _icon = GetNode<TextureRect>("CenterContainer/Panel/MarginContainer/Layout/Header/Icon");
        _title = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Header/Title");
        _upgradeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Header/UpgradeButton");
        _body = GetNode<RichTextLabel>("CenterContainer/Panel/MarginContainer/Layout/Body");
        _workRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/WorkRow");
        _gladiatorDetails = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/WorkRow/GladiatorDetails");
        _modeButtons = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/WorkRow/ModeButtons");
        _assignedGladiatorsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow");
        _assignedGladiatorsGrabButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/GrabIcon");
        _assignedGladiators = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/Gladiators");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton");
        _runData = SaveNode.Get()?.CompanyRunData;

        _title.Text = Title;
        _body.Text = Body;
        _icon.Texture = IconTexture;
        _icon.Visible = IconTexture != null;
        _upgradeButton.Pressed += OnUpgradePressed;
        _assignedGladiatorsGrabButton.Pressed += OnAssignedGladiatorsGrabPressed;
        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshOverlayState;

        UpdatePanelWidth();
        RefreshWorkControls();
        RefreshUpgradeButton();
        RefreshAssignedGladiators();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshOverlayState;

        if (_upgradeButton != null)
            _upgradeButton.Pressed -= OnUpgradePressed;

        if (_assignedGladiatorsGrabButton != null)
            _assignedGladiatorsGrabButton.Pressed -= OnAssignedGladiatorsGrabPressed;
    }

    public override void _Notification(int what)
    {
        if (what == NotificationResized)
            UpdatePanelWidth();
    }

    private void UpdatePanelWidth()
    {
        if (_panel == null)
            return;

        var viewportWidth = GetViewportRect().Size.X;
        var width = Mathf.Clamp(Mathf.RoundToInt(viewportWidth * PanelWidthRatio), PanelMinWidth, PanelMaxWidth);
        _panel.CustomMinimumSize = new Vector2(width, 0.0f);
    }

    private void RefreshOverlayState()
    {
        RefreshWorkControls();
        RefreshUpgradeButton();
        RefreshAssignedGladiators();
    }

    public int GetUpgradeGoldCost()
    {
        return _runData?.GetBuildingUpgradeGoldCost(AssignmentLocation) ?? 0;
    }

    public bool CanUpgrade()
    {
        return IsUpgradeableBuilding && _runData?.CanUpgradeBuilding(AssignmentLocation, MaxUpgradeLevel) == true;
    }

    public bool TryUpgrade()
    {
        return IsUpgradeableBuilding && _runData?.TryUpgradeBuilding(AssignmentLocation, MaxUpgradeLevel) == true;
    }

    private void RefreshUpgradeButton()
    {
        if (_upgradeButton == null)
            return;

        _upgradeButton.Visible = IsUpgradeableBuilding;
        if (!IsUpgradeableBuilding)
            return;

        var isMaxed = UpgradeLevel >= MaxUpgradeLevel;
        _upgradeButton.Text = isMaxed ? "Max" : "Upgrade";
        _upgradeButton.Disabled = isMaxed || !CanUpgrade();
        _upgradeButton.TooltipText = isMaxed
            ? $"{Title} is fully upgraded."
            : $"Upgrade {Title} to level {UpgradeLevel + 1}/{MaxUpgradeLevel} for {GetUpgradeGoldCost()} gold.";
    }

    private void OnUpgradePressed()
    {
        if (TryUpgrade())
        {
            SaveNode.Get()?.Save();
            RefreshUpgradeButton();
            return;
        }

        GlobalOverlay.Get()?.ShowBlurredPopup("Upgrade", $"Not enough gold to upgrade {Title}. Need {GetUpgradeGoldCost()} gold.");
    }

    private void RefreshWorkControls()
    {
        if (_workRow == null || _gladiatorDetails == null || _modeButtons == null)
            return;

        var shouldShow = IsWorkOverlay();
        _workRow.Visible = shouldShow;
        if (!shouldShow)
            return;

        RefreshGladiatorDetails();
        RefreshModeButtons();
    }

    private bool IsWorkOverlay()
    {
        return AssignmentLocation is TownAssignmentData.AssignmentLocation.Healer or TownAssignmentData.AssignmentLocation.TrainingHall;
    }

    private void RefreshModeButtons()
    {
        _refreshingModeButtons = true;
        foreach (var child in _modeButtons.GetChildren())
            child.QueueFree();

        if (AssignmentLocation == TownAssignmentData.AssignmentLocation.Healer)
        {
            _modeButtons.AddChild(CreateModeHeader("Treatment"));
            AddTreatmentModeButton("Health", CompanyRunData.TreatmentFocus.Health);
            AddTreatmentModeButton("Exhaustion", CompanyRunData.TreatmentFocus.Exhaustion);
        }
        else
        {
            _modeButtons.AddChild(CreateModeHeader("Training Focus"));
            AddTrainingFocusDropdown();
        }

        _refreshingModeButtons = false;
    }

    private Label CreateModeHeader(string text)
    {
        return new Label
        {
            Text = text,
            ThemeTypeVariation = "HeaderSmall",
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private void AddTreatmentModeButton(string label, CompanyRunData.TreatmentFocus focus)
    {
        var isSelected = (_runData?.CurrentTreatmentFocus ?? CompanyRunData.TreatmentFocus.Health) == focus;
        var button = CreateModeButton(label, isSelected);
        button.Pressed += () =>
        {
            if (!_refreshingModeButtons)
                _runData?.SetTreatmentFocus(focus);
        };
        _modeButtons.AddChild(button);
    }

    private void AddTrainingFocusDropdown()
    {
        var dropdown = new OptionButton
        {
            CustomMinimumSize = new Vector2(0f, 44f),
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            FocusMode = FocusModeEnum.None
        };

        AddTrainingFocusOption(dropdown, "Overall", CompanyRunData.TrainingFocus.Overall);
        AddTrainingFocusOption(dropdown, "Strength", CompanyRunData.TrainingFocus.Strength);
        AddTrainingFocusOption(dropdown, "Agility", CompanyRunData.TrainingFocus.Agility);
        AddTrainingFocusOption(dropdown, "Vitality", CompanyRunData.TrainingFocus.Vitality);
        AddTrainingFocusOption(dropdown, "Endurance", CompanyRunData.TrainingFocus.Endurance);

        var selectedFocus = _runData?.CurrentTrainingFocus ?? CompanyRunData.TrainingFocus.Overall;
        for (var index = 0; index < dropdown.ItemCount; index++)
        {
            if (dropdown.GetItemId(index) == (int)selectedFocus)
            {
                dropdown.Select(index);
                break;
            }
        }

        dropdown.ItemSelected += index =>
        {
            if (_refreshingModeButtons)
                return;

            _runData?.SetTrainingFocus((CompanyRunData.TrainingFocus)dropdown.GetItemId((int)index));
        };
        _modeButtons.AddChild(dropdown);
    }

    private static void AddTrainingFocusOption(OptionButton dropdown, string label, CompanyRunData.TrainingFocus focus)
    {
        dropdown.AddItem(label, (int)focus);
    }

    private static Button CreateModeButton(string label, bool selected)
    {
        var button = new Button
        {
            Text = label,
            ToggleMode = true,
            ButtonPressed = selected,
            CustomMinimumSize = new Vector2(0f, 40f),
            FocusMode = FocusModeEnum.None
        };
        if (selected)
            button.AddThemeColorOverride("font_color", new Color(1f, 0.86f, 0.35f));

        return button;
    }

    private void RefreshGladiatorDetails()
    {
        foreach (var child in _gladiatorDetails.GetChildren())
            child.QueueFree();

        var assigned = _runData?.TownAssignments?.GetGladiators(AssignmentLocation);
        if (!ShowAssignedGladiators || assigned == null || assigned.Count <= 0)
        {
            _gladiatorDetails.AddChild(new Label
            {
                Text = "Assign gladiators here to preview work.",
                VerticalAlignment = VerticalAlignment.Center
            });
            return;
        }

        foreach (var gladiator in assigned)
        {
            if (gladiator != null)
                _gladiatorDetails.AddChild(CreateGladiatorDetailCard(gladiator));
        }
    }

    private Control CreateGladiatorDetailCard(GladiatorData gladiator)
    {
        var row = GladiatorRowScene?.Instantiate<BuildingGladiatorRow>();
        if (row == null)
        {
            GD.PushError("Building gladiator row scene is missing or has the wrong root script.");
            return new Control();
        }

        row.Configure(gladiator, true);
        row.DragRequested += OnAssignedGladiatorDragRequested;

        if (IsFocusedTrainingOverlay())
        {
            AddAttributeRow(row.Details, gladiator, GetTrainingAttribute(_runData?.CurrentTrainingFocus ?? CompanyRunData.TrainingFocus.Strength), useDefaultFontSize: true);
            return row;
        }

        if (AssignmentLocation == TownAssignmentData.AssignmentLocation.Healer)
            AddTreatmentDetailRows(row.Details, gladiator);
        else
            AddTrainingDetailRows(row.Details, gladiator);

        return row;
    }

    private bool IsFocusedTrainingOverlay()
    {
        return AssignmentLocation == TownAssignmentData.AssignmentLocation.TrainingHall
            && (_runData?.CurrentTrainingFocus ?? CompanyRunData.TrainingFocus.Overall) != CompanyRunData.TrainingFocus.Overall;
    }

    private void AddTreatmentDetailRows(VBoxContainer details, GladiatorData gladiator)
    {
        var focus = _runData?.CurrentTreatmentFocus ?? CompanyRunData.TreatmentFocus.Health;
        if (focus == CompanyRunData.TreatmentFocus.Exhaustion)
        {
            var gain = _runData?.IsGladiatorIdleInTownLocation(gladiator, AssignmentLocation) == true
                ? 0f
                : _runData?.GetTreatmentExhaustionRecoveryPreview(gladiator) ?? 0f;
            AddValueRow(details, "Exhaustion", gladiator.Exhaustion, GladiatorData.MaxConditionValue, $"{gladiator.Exhaustion:0.#}/{GladiatorData.MaxConditionValue:0.#}", gain);
            return;
        }

        var healthGain = _runData?.IsGladiatorIdleInTownLocation(gladiator, AssignmentLocation) == true
            ? 0f
            : _runData?.GetTreatmentHealthRecoveryPreview(gladiator) ?? 0f;
        AddValueRow(details, "Health", gladiator.Health, gladiator.MaxHealth, $"{gladiator.Health}/{gladiator.MaxHealth} (recoverable {gladiator.RecoverableMaxHealth})", healthGain);
    }

    private void AddTrainingDetailRows(VBoxContainer details, GladiatorData gladiator)
    {
        var focus = _runData?.CurrentTrainingFocus ?? CompanyRunData.TrainingFocus.Overall;
        if (focus == CompanyRunData.TrainingFocus.Overall)
        {
            var grid = new GridContainer
            {
                Columns = 2,
                SizeFlagsHorizontal = SizeFlags.ExpandFill
            };
            grid.AddThemeConstantOverride("h_separation", 8);
            grid.AddThemeConstantOverride("v_separation", 4);
            details.AddChild(grid);
            AddCenteredAttributeRow(grid, gladiator, GladiatorLevelData.AttributeKind.Strength, useDefaultFontSize: true);
            AddCenteredAttributeRow(grid, gladiator, GladiatorLevelData.AttributeKind.Agility, useDefaultFontSize: true);
            AddCenteredAttributeRow(grid, gladiator, GladiatorLevelData.AttributeKind.Vitality, useDefaultFontSize: true);
            AddCenteredAttributeRow(grid, gladiator, GladiatorLevelData.AttributeKind.Endurance, useDefaultFontSize: true);
            return;
        }

        AddCenteredAttributeRow(details, gladiator, GetTrainingAttribute(focus));
    }

    private static GladiatorLevelData.AttributeKind GetTrainingAttribute(CompanyRunData.TrainingFocus focus)
    {
        return focus switch
        {
            CompanyRunData.TrainingFocus.Agility => GladiatorLevelData.AttributeKind.Agility,
            CompanyRunData.TrainingFocus.Vitality => GladiatorLevelData.AttributeKind.Vitality,
            CompanyRunData.TrainingFocus.Endurance => GladiatorLevelData.AttributeKind.Endurance,
            _ => GladiatorLevelData.AttributeKind.Strength
        };
    }

    private void AddCenteredAttributeRow(Container parent, GladiatorData gladiator, GladiatorLevelData.AttributeKind attribute, bool useDefaultFontSize = false)
    {
        var centeredAttribute = new CenterContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        parent.AddChild(centeredAttribute);
        AddAttributeRow(centeredAttribute, gladiator, attribute, useDefaultFontSize);
    }

    private void AddAttributeRow(Container parent, GladiatorData gladiator, GladiatorLevelData.AttributeKind attribute, bool useDefaultFontSize = false)
    {
        if (parent == null)
        {
            GD.PushError("Building overlay attribute row parent is missing.");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>(AttributeProgressScenePath);
        if (scene?.Instantiate() is not AttributeProgressDisplay display)
            return;

        display.CustomMinimumSize = AttributeProgressMinimumSize;
        display.SizeFlagsHorizontal = SizeFlags.Fill;
        parent.AddChild(display);
        if (useDefaultFontSize)
            display.UseDefaultFontSize();

        var level = gladiator?.Level;
        var attributeLevel = level?.GetAttributeLevel(attribute) ?? 1;
        var progress = level?.GetAttributeLevelProgress(attribute) ?? 0f;
        var gainProgress = GetTrainingGainProgress(gladiator, attribute);
        display.Configure(GetAttributeAbbreviation(attribute), attributeLevel, progress, gainProgress);
    }

    private float GetTrainingGainProgress(GladiatorData gladiator, GladiatorLevelData.AttributeKind attribute)
    {
        if (gladiator?.Level == null || _runData?.IsGladiatorIdleInTownLocation(gladiator, AssignmentLocation) == true)
            return 0f;

        var expGain = _runData.GetTrainingAttributeExpPreview(_runData.CurrentTrainingFocus, attribute);
        if (expGain <= 0f)
            return 0f;

        var currentExp = gladiator.Level.GetAttributeExp(attribute);
        var currentLevel = gladiator.Level.GetAttributeLevel(attribute);
        var nextLevelExp = GladiatorLevelData.GetAttributeExpForLevel(currentLevel + 1);
        var currentLevelExp = GladiatorLevelData.GetAttributeExpForLevel(currentLevel);
        var levelExpRange = nextLevelExp - currentLevelExp;
        if (levelExpRange <= 0f)
            return 0f;

        var currentProgress = gladiator.Level.GetAttributeLevelProgress(attribute);
        var previewProgress = Mathf.Clamp((currentExp + expGain - currentLevelExp) / levelExpRange, 0f, 1f);
        return Mathf.Max(0f, previewProgress - currentProgress);
    }

    private static void AddValueRow(Container parent, string label, float value, float maxValue, string text, float gainValue = 0f)
    {
        if (parent == null)
        {
            GD.PushError("Building overlay value row parent is missing.");
            return;
        }

        var scene = ResourceLoader.Load<PackedScene>("res://scenes/components/panels/BuildingAttributeBar.tscn");
        var row = scene?.Instantiate<BuildingAttributeBar>();
        if (row == null)
        {
            GD.PushError("Building attribute bar scene is missing or has the wrong root script.");
            return;
        }

        row.Configure(label, value, maxValue, text, gainValue);
        parent.AddChild(row);
    }

    private static string GetAttributeAbbreviation(GladiatorLevelData.AttributeKind attribute)
    {
        return attribute switch
        {
            GladiatorLevelData.AttributeKind.Agility => "AGI",
            GladiatorLevelData.AttributeKind.Vitality => "VIT",
            GladiatorLevelData.AttributeKind.Endurance => "END",
            _ => "STR"
        };
    }

    private void RefreshAssignedGladiators()
    {
        if (_assignedGladiatorsRow == null || _assignedGladiators == null)
            return;

        foreach (var child in _assignedGladiators.GetChildren())
            child.QueueFree();

        var assigned = _runData?.TownAssignments?.GetGladiators(AssignmentLocation);
        var shouldShow = ShowAssignedGladiators && assigned != null && assigned.Count > 0;
        _assignedGladiatorsRow.Visible = shouldShow;
        if (!shouldShow)
            return;

        foreach (var gladiator in assigned)
        {
            if (gladiator != null)
                _assignedGladiators.AddChild(CreateAssignedGladiatorButton(gladiator));
        }
    }

    private Control CreateAssignedGladiatorButton(GladiatorData gladiator)
    {
        var container = new Control
        {
            CustomMinimumSize = new Vector2(48, 48),
            TooltipText = $"Drag {gladiator.GladiatorName}"
        };

        var button = new Button
        {
            AnchorsPreset = (int)LayoutPreset.FullRect,
            Icon = gladiator.GetUiIconTexture(),
            TooltipText = $"Drag {gladiator.GladiatorName}",
            ExpandIcon = true
        };
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        container.AddChild(button);

        var riskIconPath = GetRiskIconPath(gladiator);
        if (!string.IsNullOrEmpty(riskIconPath))
        {
            var riskIcon = RiskIconScene?.Instantiate<RiskIcon>();
            if (riskIcon == null)
            {
                GD.PushError("Risk icon scene is missing or has the wrong root script.");
                return container;
            }

            riskIcon.Configure(ResourceLoader.Load<Texture2D>(riskIconPath));
            riskIcon.SetAnchorsPreset(LayoutPreset.FullRect);
            container.AddChild(riskIcon);
        }

        button.ButtonDown += () => OnAssignedGladiatorDragRequested(gladiator);
        return container;
    }

    private string GetRiskIconPath(GladiatorData gladiator)
    {
        if (gladiator == null)
            return string.Empty;

        if (_runData?.IsGladiatorIdleInTownLocation(gladiator, AssignmentLocation) == true)
            return IdleIconPath;

        var warningRatio = SaveNode.Get()?.SettingsConfig?.LowHealthWarningRatio ?? 0.6f;
        return gladiator.GetRiskStatus(ExhaustionWarningThreshold, warningRatio) switch
        {
            GladiatorRiskStatus.Critical => CriticalRiskIconPath,
            GladiatorRiskStatus.Exhausted => ExhaustionIconPath,
            GladiatorRiskStatus.LowHealth => HealthIconPath,
            _ => string.Empty
        };
    }

    private void OnAssignedGladiatorDragRequested(GladiatorData gladiator)
    {
        if (gladiator == null)
            return;

        foreach (var node in GetTree().GetNodesInGroup("roster_yard"))
        {
            if (node is not RosterYard rosterYard)
                continue;

            rosterYard.StartGladiatorDrag(gladiator, GetViewport().GetMousePosition());
            QueueFree();
            return;
        }

        GD.PushError($"Building overlay drag failed: roster yard missing for gladiator '{gladiator.GladiatorName}'.");
    }

    private void OnAssignedGladiatorsGrabPressed()
    {
        var assigned = _runData?.TownAssignments?.GetGladiators(AssignmentLocation);
        if (assigned == null || assigned.Count <= 0)
            return;

        var assignedCopy = new Godot.Collections.Array<GladiatorData>(assigned);
        foreach (var gladiator in assignedCopy)
        {
            if (gladiator != null)
                _runData.TryMoveGladiatorToCourtyard(gladiator);
        }

        SaveNode.Get()?.Save();
    }
}
