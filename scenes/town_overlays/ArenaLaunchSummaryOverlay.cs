using Godot;
using System;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Contracts;
using MobArena.Scripts.Resources.Items;

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
    private Texture2D _touchDeviceIcon;
    private Texture2D _gamepadDeviceIcon;
    private Texture2D _goldIcon;

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
                _playerList.AddChild(CreatePlayerRow(gladiator, index));
        }
    }

    private Control CreatePlayerRow(GladiatorData gladiator, int index)
    {
        var panel = new PanelContainer
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        };

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
        row.AddThemeConstantOverride("separation", 6);
        margin.AddChild(row);

        row.AddChild(CreateIcon(gladiator.GetPortraitTexture(), 38f, gladiator.GladiatorName));
        row.AddChild(new Label
        {
            CustomMinimumSize = new Vector2(110f, 0f),
            Text = $"P{index + 1} {gladiator.GladiatorName}",
            VerticalAlignment = VerticalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.WordSmart
        });

        var equipment = gladiator.Equipment;
        row.AddChild(CreateItemSlot(equipment?.MainHand, "Main hand"));
        row.AddChild(CreateItemSlot(equipment?.Armor, "Armor"));
        row.AddChild(CreateItemSlot(equipment?.OffHand, "Off hand"));

        var assignment = _runData?.GetArenaControlAssignment(gladiator);
        var controllerIcon = GetControllerIcon(assignment);
        row.AddChild(CreateIcon(controllerIcon, 34f, GetControllerLabel(assignment)));

        return panel;
    }

    private static Control CreateItemSlot(ItemData item, string slotName)
    {
        var slot = new PanelContainer
        {
            CustomMinimumSize = new Vector2(36f, 36f),
            TooltipText = item == null ? $"{slotName}: Empty" : $"{slotName}: {item.DisplayName}"
        };

        if (item?.Icon != null)
        {
            slot.AddChild(CreateIcon(item.Icon, 30f, slot.TooltipText));
        }
        else
        {
            slot.Modulate = new Color(0.65f, 0.65f, 0.65f, 0.85f);
        }

        return slot;
    }

    private static TextureRect CreateIcon(Texture2D texture, float size, string tooltip)
    {
        return new TextureRect
        {
            CustomMinimumSize = new Vector2(size, size),
            Texture = texture,
            TooltipText = tooltip,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        };
    }

    private Texture2D GetControllerIcon(ArenaControlAssignmentData assignment)
    {
        if (assignment == null)
            return null;

        return assignment.ControllerKind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => _keyboardDeviceIcon,
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
