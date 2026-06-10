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

    [Export]
    public PackedScene CostRowScene { get; set; }

    [Export]
    public PackedScene ChangeRowScene { get; set; }

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
                Text = "No Recovery Bay or Training Hall changes queued.",
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

        AddSectionHeader($"Recovery Bay - {GetTreatmentFocusLabel()}");
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
        AddGladiatorChangeRow(gladiator, detail, idle);
    }

    private void AddGladiatorChangeRow(GladiatorData gladiator, string detail, bool muted)
    {
        var row = ChangeRowScene?.Instantiate<PhaseChangeRow>();
        if (row == null)
        {
            GD.PushError("Phase change row scene is missing or has the wrong root script.");
            return;
        }

        row.Configure(gladiator, detail, muted);
        _changeList.AddChild(row);
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
        var row = CostRowScene?.Instantiate<PhaseCostRow>();
        if (row == null)
        {
            GD.PushError("Phase cost row scene is missing or has the wrong root script.");
            return;
        }

        row.Configure(_goldIcon, label, value, highlightRed);
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
