using Godot;
using System;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Contracts;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaLaunchSummaryOverlay : Control
{
    private VBoxContainer _playerList;
    private Label _costLineLabel;
    private VBoxContainer _costDetails;
    private Button _startButton;
    private Button _resetButton;
    private CompanyRunData _runData;
    private TownPhaseState _phaseState;
    private LocalInputConfig _localInputConfig;
    private ArenaContractData _contract;
    private Action _startAction;
    private Action _resetAction;
    private Texture2D _keyboardDeviceIcon;
    private Texture2D _mouseDeviceIcon;
    private Texture2D _touchDeviceIcon;
    private Texture2D _gamepadDeviceIcon;
    private Texture2D _goldIcon;

    [Export]
    public PackedScene PlayerSummaryScene { get; set; }

    [Export]
    public PackedScene CostRowScene { get; set; }

    public void Configure(ArenaContractData contract, Action startAction, Action resetAction)
    {
        _contract = contract;
        _startAction = startAction;
        _resetAction = resetAction;
    }

    public override void _Ready()
    {
        _playerList = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/PlayerScroll/PlayerList");
        _costLineLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/CostPanel/MarginContainer/CostLayout/CostLine");
        _costDetails = GetNode<VBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/CostPanel/MarginContainer/CostLayout/CostDetails");
        _startButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/StartButton");
        _resetButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/ResetButton");
        var saveNode = SaveNode.Get();
        _runData = saveNode?.CompanyRunData;
        _phaseState = saveNode?.TownPhaseState;
        _localInputConfig = LocalInputConfig.Get();
        _keyboardDeviceIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/device_pc.png");
        _mouseDeviceIcon = _localInputConfig?.MouseIcon;
        _touchDeviceIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/device_phone.png");
        _gamepadDeviceIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/device_console.png");
        _goldIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/icons/gold.svg");

        _startButton.Pressed += OnStartPressed;
        _resetButton.Pressed += OnResetPressed;
        Refresh();
    }

    private void Refresh()
    {
        RefreshPlayers();
        RefreshCostSummary();
    }

    private void RefreshPlayers()
    {
        foreach (var child in _playerList.GetChildren())
            child.QueueFree();

        var arenaGladiators = _runData?.TownAssignments?.ArenaGladiators;
        if (arenaGladiators == null || arenaGladiators.Count <= 0)
        {
            _playerList.AddChild(new Label
            {
                Text = "No arena gladiators assigned.",
                HorizontalAlignment = HorizontalAlignment.Center
            });
            return;
        }

        for (var index = 0; index < arenaGladiators.Count; index++)
        {
            var gladiator = arenaGladiators[index];
            if (gladiator != null)
                AddPlayerRow(gladiator, index);
        }
    }

    private void AddPlayerRow(GladiatorData gladiator, int index)
    {
        var assignment = _runData?.GetArenaControlAssignment(gladiator);
        var row = PlayerSummaryScene?.Instantiate<ArenaLaunchPlayerSummary>();
        if (row == null)
        {
            GD.PushError("Arena launch player summary scene is missing or has the wrong root script.");
            return;
        }

        row.Configure(gladiator, index, GetControllerIcon(assignment), GetControllerLabel(assignment));
        _playerList.AddChild(row);
    }

    private Texture2D GetControllerIcon(ArenaControlAssignmentData assignment)
    {
        if (assignment == null)
            return null;

        return assignment.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => _keyboardDeviceIcon,
            LocalInputControllerConfig.ControllerKind.Mouse => _mouseDeviceIcon,
            LocalInputControllerConfig.ControllerKind.Touch => _touchDeviceIcon,
            LocalInputControllerConfig.ControllerKind.Gamepad => _gamepadDeviceIcon,
            _ => null
        };
    }

    private void RefreshCostSummary()
    {
        foreach (var child in _costDetails.GetChildren())
            child.QueueFree();

        var currentGold = _runData?.Gold ?? 0;
        var cityCost = _runData?.GetArenaReturnUpkeepGoldCost(_phaseState) ?? 0;
        var contractGold = _contract?.GoldReward ?? 0;
        var lossDelta = -cityCost;
        var winDelta = contractGold - cityCost;
		var lossGold = currentGold + lossDelta;
		var winGold = currentGold + winDelta;
		_costLineLabel.Text = "Gold change after arena";
		_startButton.Disabled = _runData == null;
		_startButton.TooltipText = lossGold < 0 ? "Start the arena contract and go into debt if needed." : "Start the arena contract.";

		if (cityCost > 0)
		{
			AddCostDetail("On loss", FormatSignedGold(lossDelta), lossGold < 0);
		}

        AddCostDetail("On win", FormatSignedGold(winDelta), winGold < 0);
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

    private static string FormatSignedGold(int amount)
    {
        return amount >= 0 ? $"+{amount} gold" : $"{amount} gold";
    }

    private void OnStartPressed()
    {
        QueueFree();
        _startAction?.Invoke();
    }

    private void OnResetPressed()
    {
        QueueFree();
        _resetAction?.Invoke();
    }

    private static string GetControllerLabel(ArenaControlAssignmentData assignment)
    {
        if (assignment == null)
            return "Unassigned";

        return assignment.DisplayName;
    }
}
