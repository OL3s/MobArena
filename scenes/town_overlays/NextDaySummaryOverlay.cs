using Godot;
using System;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class NextDaySummaryOverlay : Control
{
    private VBoxContainer _changeList;
    private Label _costLineLabel;
    private VBoxContainer _costDetails;
    private Button _nextDayButton;
    private Button _cancelButton;
    private CompanyRunData _runData;
    private TownPhaseState _phaseState;
    private Action _nextDayAction;
    private Texture2D _goldIcon;

    public void Configure(Action nextDayAction)
    {
        _nextDayAction = nextDayAction;
    }

    public override void _Ready()
    {
        _changeList = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/ChangeScroll/ChangeList");
        _costLineLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/CostPanel/MarginContainer/CostLayout/CostLine");
        _costDetails = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/CostPanel/MarginContainer/CostLayout/CostDetails");
        _nextDayButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/NextDayButton");
        _cancelButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CancelButton");

        var saveNode = SaveNode.Get();
        _runData = saveNode?.CompanyRunData;
        _phaseState = saveNode?.TownPhaseState;
        _goldIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/gold.svg");

        _nextDayButton.Pressed += OnNextDayPressed;
        _cancelButton.Pressed += QueueFree;
        Refresh();
    }

    private void Refresh()
    {
        RefreshCosts();
        RefreshChanges();
    }

    private void RefreshCosts()
    {
        foreach (var child in _costDetails.GetChildren())
            child.QueueFree();

        var currentGold = _runData?.Gold ?? 0;
		var cityCost = _runData?.GetCurrentPhaseGoldCost(_phaseState) ?? 0;
		var nextGold = currentGold - cityCost;
		_costLineLabel.Text = "City & Salary cost";
		_nextDayButton.Disabled = _runData == null;
		_nextDayButton.TooltipText = nextGold < 0 ? "Apply these changes and go into debt." : "Apply these changes and advance to the next day.";

        if (cityCost > 0)
            AddCostDetail("Total", FormatSignedGold(-cityCost), nextGold < 0);
        else
            AddCostDetail("Total", "0 gold", false);
    }

    private void RefreshChanges()
    {
        foreach (var child in _changeList.GetChildren())
            child.QueueFree();

        var addedAny = false;
        addedAny |= AddTreatmentRows();
        addedAny |= AddTrainingRows();

        if (!addedAny)
        {
            _changeList.AddChild(new Label
            {
                Text = "No Thermae or Training Hall changes queued.",
                HorizontalAlignment = HorizontalAlignment.Center,
                AutowrapMode = TextServer.AutowrapMode.WordSmart
            });
        }
    }

    private bool AddTreatmentRows()
    {
        var gladiators = _runData?.TownAssignments?.HealerGladiators;
        if (gladiators == null || gladiators.Count <= 0)
            return false;

        AddSectionHeader($"Thermae - {GetTreatmentFocusLabel()}");
        foreach (var gladiator in gladiators)
            AddAssignedGladiatorRow(gladiator, TownAssignmentData.AssignmentLocation.Healer);

        return true;
    }

    private bool AddTrainingRows()
    {
        var gladiators = _runData?.TownAssignments?.TrainingHallGladiators;
        if (gladiators == null || gladiators.Count <= 0)
            return false;

        AddSectionHeader($"Training Hall - {GetTrainingFocusLabel()}");
        foreach (var gladiator in gladiators)
            AddAssignedGladiatorRow(gladiator, TownAssignmentData.AssignmentLocation.TrainingHall);

        return true;
    }

    private void AddAssignedGladiatorRow(GladiatorData gladiator, TownAssignmentData.AssignmentLocation location)
    {
        if (gladiator == null)
            return;

        var idle = _runData?.IsGladiatorIdleInTownLocation(gladiator, location) == true;
        var detail = idle
            ? location == TownAssignmentData.AssignmentLocation.Healer ? "No treatment needed" : "Too exhausted to train"
            : string.Empty;
        _changeList.AddChild(CreateGladiatorChangeRow(gladiator, detail, idle));
    }

    private Control CreateGladiatorChangeRow(GladiatorData gladiator, string detail, bool muted)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        if (muted)
            panel.Modulate = new Color(0.72f, 0.72f, 0.72f, 1f);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 10);
        margin.AddThemeConstantOverride("margin_top", 8);
        margin.AddThemeConstantOverride("margin_right", 10);
        margin.AddThemeConstantOverride("margin_bottom", 8);
        panel.AddChild(margin);

        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);
        margin.AddChild(row);

        row.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(38f, 38f),
            Texture = gladiator.GetPortraitTexture(),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });

        row.AddChild(new Label
        {
            CustomMinimumSize = new Vector2(115f, 0f),
            Text = gladiator.GladiatorName,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        row.AddChild(new Label
        {
            Text = detail,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        return panel;
    }

    private void AddSectionHeader(string title)
    {
        _changeList.AddChild(new Label
        {
            Text = title,
            ThemeTypeVariation = "HeaderSmall",
            HorizontalAlignment = HorizontalAlignment.Center
        });
    }

    private void AddCostDetail(string label, string value, bool highlightRed)
    {
        var row = new HBoxContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };
        row.AddThemeConstantOverride("separation", 8);

        row.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(24f, 24f),
            Texture = _goldIcon,
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });

        row.AddChild(new Label
        {
            Text = label,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            VerticalAlignment = VerticalAlignment.Center
        });

        var valueLabel = new Label
        {
            Text = value,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        };
        if (highlightRed)
            valueLabel.AddThemeColorOverride("font_color", new Color(1f, 0.28f, 0.22f));

        row.AddChild(valueLabel);
        _costDetails.AddChild(row);
    }

    private void OnNextDayPressed()
    {
        QueueFree();
        _nextDayAction?.Invoke();
    }

    private string GetTreatmentFocusLabel()
    {
        return _runData?.CurrentTreatmentFocus switch
        {
            CompanyRunData.TreatmentFocus.Exhaustion => "Exhaustion",
            _ => "Health"
        };
    }

    private string GetTrainingFocusLabel()
    {
        return _runData?.CurrentTrainingFocus switch
        {
            CompanyRunData.TrainingFocus.Strength => "Strength",
            CompanyRunData.TrainingFocus.Agility => "Agility",
            CompanyRunData.TrainingFocus.Vitality => "Vitality",
            CompanyRunData.TrainingFocus.Endurance => "Endurance",
            _ => "Overall"
        };
    }

    private static string FormatSignedGold(int amount)
    {
        return amount >= 0 ? $"+{amount} gold" : $"{amount} gold";
    }
}
