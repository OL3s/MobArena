using Godot;
using MobArena.Scenes.Components.Town;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Panels;

public partial class BuildingOverlayPanel : Control
{
    private const float PanelWidthRatio = 0.68f;
    private const int PanelMinWidth = 420;
    private const int PanelMaxWidth = 820;
    private const float ExhaustionWarningThreshold = 5f;
    private const string ExhaustionIconPath = "res://assets/ui/gladiator_icons/exhaustion.svg";
    private const string HealthIconPath = "res://assets/ui/gladiator_icons/health.svg";
    private const string CriticalRiskIconPath = "res://assets/ui/gladiator_icons/critical_risk.svg";

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

    private PanelContainer _panel;
    private TextureRect _icon;
    private Label _title;
    private RichTextLabel _body;
    private HBoxContainer _assignedGladiatorsRow;
    private HBoxContainer _assignedGladiators;
    private Button _closeButton;
    private CompanyRunData _runData;

    public override void _Ready()
    {
        _panel = GetNode<PanelContainer>("CenterContainer/Panel");
        _icon = GetNode<TextureRect>("CenterContainer/Panel/MarginContainer/Layout/Header/Icon");
        _title = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Header/Title");
        _body = GetNode<RichTextLabel>("CenterContainer/Panel/MarginContainer/Layout/Body");
        _assignedGladiatorsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow");
        _assignedGladiators = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/Gladiators");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton");
        _runData = SaveNode.Get()?.CompanyRunData;

        _title.Text = Title;
        _body.Text = Body;
        _icon.Texture = IconTexture;
        _icon.Visible = IconTexture != null;
        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshAssignedGladiators;

        UpdatePanelWidth();
        RefreshAssignedGladiators();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshAssignedGladiators;
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
            Icon = gladiator.GetPortraitTexture(),
            TooltipText = $"Drag {gladiator.GladiatorName}",
            ExpandIcon = true
        };
        button.SetAnchorsPreset(LayoutPreset.FullRect);
        container.AddChild(button);

        var riskIconPath = GetRiskIconPath(gladiator);
        if (!string.IsNullOrEmpty(riskIconPath))
        {
            var riskIcon = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>(riskIconPath),
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = new Color(1f, 1f, 1f, 0.8f),
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
            };
            riskIcon.SetAnchorsPreset(LayoutPreset.FullRect);
            container.AddChild(riskIcon);
        }

        button.ButtonDown += () => OnAssignedGladiatorDragRequested(gladiator);
        return container;
    }

    private static string GetRiskIconPath(GladiatorData gladiator)
    {
        if (gladiator == null)
            return string.Empty;

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
}
