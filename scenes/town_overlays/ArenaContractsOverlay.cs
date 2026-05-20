using Godot;
using MobArena.Scenes.Components.Town;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaContractsOverlay : Control
{
    [Export]
    public PackedScene ArenaScene { get; set; }

    [Export]
    public PackedScene ContractCardScene { get; set; }

    [Export]
    public PackedScene ControlConfigOverlayScene { get; set; }

    private HBoxContainer _contractsRow;
    private HBoxContainer _assignedGladiatorsRow;
    private HBoxContainer _assignedGladiators;
    private Button _configureControlsButton;
    private Button _startButton;
    private Button _closeButton;
    private CompanyRunData _runData;
    private int _selectedContractIndex = -1;
    private bool _refreshingUi;

    public override void _Ready()
    {
        _contractsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/ContractArea/ContractsScroll/ContractsRow");
        _assignedGladiatorsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow");
        _assignedGladiators = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/Gladiators");
        _configureControlsButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/ConfigureControlsButton");
        _startButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/StartButton");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton");
        _runData = SaveNode.Get()?.CompanyRunData;

        _configureControlsButton.Pressed += OpenControlConfigOverlay;
        _startButton.Pressed += OnStartPressed;
        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshUi;

        BuildMockContracts();
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void BuildMockContracts()
    {
        AddMockContract(0, "Starter Slime Pit", "3 slimes", "35 gold, 1 fame", "Low risk");
        AddMockContract(1, "Rat Cellar Cleanup", "6 quick rats", "50 gold, 2 fame", "Medium speed");
        AddMockContract(2, "Goblin Trial", "2 goblins", "85 gold, 4 fame", "Mock locked");
    }

    private void AddMockContract(int contractIndex, string title, string enemies, string reward, string risk)
    {
        var card = ContractCardScene?.Instantiate<ArenaContractCard>();
        if (card == null)
        {
            GD.PushError("Arena contract card scene is missing or has the wrong root script.");
            return;
        }

        card.Configure(contractIndex, title, enemies, reward, risk);
        card.ContractSelected += SelectContract;
        _contractsRow.AddChild(card);
    }

    private void SelectContract(int contractIndex)
    {
        _selectedContractIndex = contractIndex;
        foreach (var child in _contractsRow.GetChildren())
        {
            if (child is ArenaContractCard card)
                card.ButtonPressed = card.ContractIndex == contractIndex;
        }

        RefreshActions();
    }

    private void RefreshUi()
    {
        if (_refreshingUi)
            return;

        _refreshingUi = true;
        var controllerSetups = LocalInputConfig.Get()?.ControllerSetups ?? new Godot.Collections.Array<LocalInputControllerConfig>();
        _runData?.SyncArenaControlAssignments(controllerSetups);
        RefreshAssignedGladiators();
        RefreshActions();
        _refreshingUi = false;
    }

    private void RefreshAssignedGladiators()
    {
        foreach (var child in _assignedGladiators.GetChildren())
            child.QueueFree();

        var assigned = _runData?.TownAssignments?.ArenaGladiators;
        var shouldShow = assigned != null && assigned.Count > 0;
        _assignedGladiatorsRow.Visible = shouldShow;
        if (!shouldShow)
            return;

        foreach (var gladiator in assigned)
        {
            if (gladiator != null)
                _assignedGladiators.AddChild(CreateAssignedGladiatorButton(gladiator));
        }
    }

    private Button CreateAssignedGladiatorButton(GladiatorData gladiator)
    {
        var button = new Button
        {
            CustomMinimumSize = new Vector2(48, 48),
            Icon = gladiator.GetPortraitTexture(),
            TooltipText = $"Drag {gladiator.GladiatorName}",
            ExpandIcon = true
        };

        button.ButtonDown += () => StartGladiatorDrag(gladiator);
        return button;
    }

    private void RefreshActions()
    {
        var controllerSetups = LocalInputConfig.Get()?.ControllerSetups ?? new Godot.Collections.Array<LocalInputControllerConfig>();
        var assignedCount = _runData?.TownAssignments?.ArenaGladiators?.Count ?? 0;
        var hasContract = _selectedContractIndex >= 0;
        var ready = _runData?.AreArenaGladiatorsReadyForLaunch(controllerSetups) == true;
        var missingControls = assignedCount > 0 && !ready;

        _configureControlsButton.Disabled = assignedCount <= 0;
        _configureControlsButton.Text = "Controller Config";
        _startButton.Disabled = !hasContract || !ready;

        if (missingControls)
        {
            _startButton.Text = "Missing Controls";
            _startButton.TooltipText = "Assign one unique control setup to each Arena gladiator before starting.";
        }
        else if (assignedCount <= 0)
        {
            _startButton.Text = "Missing Gladiator";
            _startButton.TooltipText = "Assign at least one gladiator to the Arena building before starting.";
        }
        else if (!hasContract)
        {
            _startButton.Text = "Choose Contract";
            _startButton.TooltipText = "Choose a contract first.";
        }
        else
        {
            _startButton.Text = "Start";
            _startButton.TooltipText = "Start the selected mock contract.";
        }
    }

    private void OpenControlConfigOverlay()
    {
        if (ControlConfigOverlayScene == null)
        {
            GD.PushError("Arena control config overlay scene is missing.");
            return;
        }

        GlobalOverlay.Get()?.AddOverlay(ControlConfigOverlayScene);
    }

    private void OnStartPressed()
    {
        var controllerSetups = LocalInputConfig.Get()?.ControllerSetups ?? new Godot.Collections.Array<LocalInputControllerConfig>();
        if (_selectedContractIndex < 0 || _runData?.AreArenaGladiatorsReadyForLaunch(controllerSetups) != true)
            return;

        GlobalOverlay.Get()?.ShowGoCancelPopup(
            "Start Arena?",
            "This will use the selected mock contract and the current gladiator control assignments.",
            StartArenaScene,
            "Start",
            "Cancel");
    }

    private void StartArenaScene()
    {
        var scene = ArenaScene ?? ResourceLoader.Load<PackedScene>("res://scenes/arena.tscn");
        if (scene == null)
        {
            GD.PushError("Arena launch failed: scenes/arena.tscn could not be loaded.");
            return;
        }

        GetTree().ChangeSceneToPacked(scene);
        GlobalOverlay.Get()?.CloseAllOverlaysImmediate();
    }

    private void StartGladiatorDrag(GladiatorData gladiator)
    {
        foreach (var node in GetTree().GetNodesInGroup("roster_yard"))
        {
            if (node is not RosterYard rosterYard)
                continue;

            rosterYard.StartGladiatorDrag(gladiator, GetViewport().GetMousePosition());
            QueueFree();
            return;
        }

        GD.PushError($"Arena overlay drag failed: roster yard missing for gladiator '{gladiator.GladiatorName}'.");
    }
}
