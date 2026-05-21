using Godot;
using Godot.Collections;
using MobArena.Scenes.Components.Town;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources.Contracts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class ArenaContractsOverlay : Control
{
    private const string TownScenePath = "res://scenes/town.tscn";
    private const string ArenaDonationOverlayScenePath = "res://scenes/town_overlays/arena_donation_overlay.tscn";

    [Export]
    public PackedScene ArenaScene { get; set; }

    [Export]
    public PackedScene ContractCardScene { get; set; }

    [Export]
    public PackedScene ControlConfigOverlayScene { get; set; }

    [Export]
    public Array<ArenaContractData> Contracts { get; private set; } = new();

    private HBoxContainer _contractsRow;
    private HBoxContainer _assignedGladiatorsRow;
    private HBoxContainer _assignedGladiators;
    private Button _startButton;
    private Button _closeButton;
    private TownPhaseState _phaseState;
    private CompanyRunData _runData;
    private int _selectedContractIndex = -1;
    private bool _refreshingUi;

    public override void _Ready()
    {
        _contractsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/ContractArea/ContractsScroll/ContractsRow");
        _assignedGladiatorsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow");
        _assignedGladiators = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/Gladiators");
        _startButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/StartButton");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton");
        var saveNode = SaveNode.Get();
        _runData = saveNode?.CompanyRunData;
        _phaseState = saveNode?.TownPhaseState;

        _startButton.Pressed += OnStartPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Header/DonateButton").Pressed += OnDonatePressed;
        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshUi;

        BuildContracts();
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
    }

    private void BuildContracts()
    {
        for (var index = 0; index < Contracts.Count; index++)
            AddContract(index, Contracts[index]);
    }

    private void AddContract(int contractIndex, ArenaContractData contractData)
    {
        if (contractData == null)
            return;

        var card = ContractCardScene?.Instantiate<ArenaContractCard>();
        if (card == null)
        {
            GD.PushError("Arena contract card scene is missing or has the wrong root script.");
            return;
        }

        card.Configure(contractIndex, contractData, _runData?.Fame ?? 0);
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
        RefreshContractCards();
        RefreshAssignedGladiators();
        RefreshActions();
        _refreshingUi = false;
    }

    private void RefreshContractCards()
    {
        foreach (var child in _contractsRow.GetChildren())
        {
            if (child is ArenaContractCard card)
                card.SetCurrentCompanyFame(_runData?.Fame ?? 0);
        }
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
        var assignedCount = _runData?.TownAssignments?.ArenaGladiators?.Count ?? 0;
        var selectedContract = GetSelectedContractOrNull();
        var hasContract = selectedContract != null;
        var canPayReturnUpkeep = _runData?.CanPayArenaReturnUpkeep(_phaseState) != false;

        _startButton.Disabled = !canPayReturnUpkeep || !hasContract || assignedCount <= 0;

        if (!canPayReturnUpkeep)
        {
            var cost = _runData?.GetArenaReturnUpkeepGoldCost(_phaseState) ?? 0;
            _startButton.Text = "Upkeep Short";
            _startButton.TooltipText = $"Need {cost} gold for upkeep when the arena day ends.";
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
            _startButton.TooltipText = "Set arena controls for this contract.";
        }
    }

    private void OpenControlConfigOverlay()
    {
        if (ControlConfigOverlayScene == null)
        {
            GD.PushError("Arena control config overlay scene is missing.");
            return;
        }

        var overlay = ControlConfigOverlayScene.Instantiate<ArenaControlConfigOverlay>();
        overlay.Configure(StartArenaScene, MockCompleteContract);
        GlobalOverlay.Get()?.AddOverlay(overlay);
    }

    private void OnStartPressed()
    {
        if (_runData?.CanPayArenaReturnUpkeep(_phaseState) == false
            || GetSelectedContractOrNull() == null
            || (_runData?.TownAssignments?.ArenaGladiators?.Count ?? 0) <= 0)
            return;

        OpenControlConfigOverlay();
    }

    private static void OnDonatePressed()
    {
        var overlayScene = ResourceLoader.Load<PackedScene>(ArenaDonationOverlayScenePath);
        if (overlayScene == null)
        {
            GD.PushError("Arena donation overlay scene is missing.");
            return;
        }

        GlobalOverlay.Get()?.AddOverlay(overlayScene.Instantiate<ArenaDonationOverlay>());
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

    private void MockCompleteContract()
    {
        var saveNode = SaveNode.Get();
        var selectedContract = GetSelectedContractOrNull();
        if (saveNode == null || _runData == null || _phaseState == null || selectedContract == null)
            return;

        _runData.AddGold(selectedContract.GoldReward, saveNode.CompanyCareerData);
        var netFameReward = selectedContract.GetNetFameReward(_runData.Fame);
        if (netFameReward >= 0)
            _runData.AddFame(netFameReward);
        else
            _runData.LoseFame(-netFameReward);
        saveNode.CompanyCareerData?.AddContractCompleted();
        foreach (var gladiator in _runData.TownAssignments.ArenaGladiators)
        {
            gladiator?.GladiatorCareer?.AddContractCompleted();
            gladiator?.GladiatorCareer?.AddWin();
        }

        PhaseTransitionController.CompleteArenaContract(_phaseState, _runData);
        saveNode.Save();
        GlobalOverlay.Get()?.CloseAllOverlaysImmediate();
        GetTree().ChangeSceneToFile(TownScenePath);
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

    private ArenaContractData GetSelectedContractOrNull()
    {
        return _selectedContractIndex >= 0 && _selectedContractIndex < Contracts.Count
            ? Contracts[_selectedContractIndex]
            : null;
    }
}
