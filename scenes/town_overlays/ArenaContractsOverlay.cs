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
    private const string ArenaDonationOverlayScenePath = "res://scenes/town_overlays/arena_donation_overlay.tscn";
    [Export]
    public PackedScene ArenaScene { get; set; }

    [Export]
    public PackedScene ContractCardScene { get; set; }

    [Export]
    public PackedScene ControlConfigOverlayScene { get; set; }

    [Export]
    public Array<ArenaContractData> Contracts { get; private set; } = new();

    private ScrollContainer _contractsScroll;
    private HBoxContainer _contractsRow;
    private Control _missingGladiatorMessage;
    private HBoxContainer _assignedGladiatorsRow;
    private HBoxContainer _assignedGladiators;
    private Button _startButton;
    private Button _rerollButton;
    private Button _closeButton;
    private CompanyCareerData _careerData;
    private TownPhaseState _phaseState;
    private CompanyRunData _runData;
    private Array<ArenaContractData> _activeContracts = new();
    private int _selectedContractIndex = -1;
    private int _generatedForFame = -1;
    private bool _generatedForChampionDay;
    private bool _generatedForCompletedContracts;
    private bool _generatedForSkipTutorial;
    private bool _refreshingUi;

    public override void _Ready()
    {
        _contractsScroll = GetNode<ScrollContainer>("CenterContainer/Panel/MarginContainer/Layout/ContractArea/ContractsScroll");
        _contractsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/ContractArea/ContractsScroll/ContractsRow");
        _missingGladiatorMessage = GetNode<Control>("CenterContainer/Panel/MarginContainer/Layout/ContractArea/MissingGladiatorMessage");
        _assignedGladiatorsRow = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow");
        _assignedGladiators = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/Gladiators");
        _startButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/StartButton");
        _rerollButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Header/RerollButton");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton");
        var saveNode = SaveNode.Get();
        _runData = saveNode?.CompanyRunData;
        _careerData = saveNode?.CompanyCareerData;
        _phaseState = saveNode?.TownPhaseState;

        _startButton.Pressed += OnStartPressed;
        _rerollButton.Pressed += OnRerollPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Header/DonateButton").Pressed += OnDonatePressed;
        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshUi;
        if (_careerData != null)
            _careerData.CareerChanged += RefreshUi;

        BuildContracts();
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_runData != null)
            _runData.RunChanged -= RefreshUi;
        if (_careerData != null)
            _careerData.CareerChanged -= RefreshUi;
    }

    private void BuildContracts()
    {
        foreach (var child in _contractsRow.GetChildren())
            child.QueueFree();

        _selectedContractIndex = -1;
        _generatedForFame = _runData?.Fame ?? 0;
        _generatedForChampionDay = _phaseState?.IsChampionDay == true;
        _generatedForSkipTutorial = SaveNode.Get().SkipTutorial;
        _generatedForCompletedContracts = _careerData?.HasCompletedContracts == true || _generatedForSkipTutorial;
		_activeContracts = ArenaContractSelection.GetVisibleContracts(_generatedForFame, _generatedForChampionDay, _generatedForCompletedContracts);

		if (_activeContracts.Count <= 0 && !_generatedForSkipTutorial)
        {
            foreach (var contract in Contracts)
            {
                if (contract?.IsChampionContract() == (_phaseState?.IsChampionDay == true))
                    _activeContracts.Add(contract);
            }
        }

        for (var index = 0; index < _activeContracts.Count; index++)
            AddContract(index, _activeContracts[index]);
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
        if ((_runData?.Fame ?? 0) != _generatedForFame
            || (_phaseState?.IsChampionDay == true) != _generatedForChampionDay
            || SaveNode.Get().SkipTutorial != _generatedForSkipTutorial
            || ((_careerData?.HasCompletedContracts == true) || SaveNode.Get().SkipTutorial) != _generatedForCompletedContracts)
        {
            BuildContracts();
        }

        RefreshContractCards();
        RefreshAssignedGladiators();
        RefreshContractVisibility();
        RefreshActions();
        _refreshingUi = false;
    }

    private void RefreshContractVisibility()
    {
        var hasAssignedGladiator = (_runData?.TownAssignments?.ArenaGladiators?.Count ?? 0) > 0;
        _contractsScroll.Visible = hasAssignedGladiator;
        _missingGladiatorMessage.Visible = !hasAssignedGladiator;

        if (!hasAssignedGladiator)
            SelectContract(-1);
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
            Icon = gladiator.GetUiIconTexture(),
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
		var rerollCost = GetRerollGoldCost();

		_startButton.Disabled = !hasContract || assignedCount <= 0;
		_rerollButton.Disabled = assignedCount <= 0 || _runData == null || _runData.Gold < rerollCost;
		_rerollButton.Text = $"Reroll {rerollCost}";
		_rerollButton.TooltipText = $"Spend {rerollCost} gold to reroll all contracts.";

		if (assignedCount <= 0)
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
        overlay.Configure(GetSelectedContractOrNull(), StartArenaScene);
        GlobalOverlay.Get()?.AddOverlay(overlay);
    }

	private void OnStartPressed()
	{
		if (GetSelectedContractOrNull() == null
			|| (_runData?.TownAssignments?.ArenaGladiators?.Count ?? 0) <= 0)
			return;

        OpenControlConfigOverlay();
    }

    private void OnRerollPressed()
    {
        var cost = GetRerollGoldCost();
        if (_runData == null || !_runData.TrySpendGold(cost))
            return;

        BuildContracts();
        RefreshUi();
    }

    private int GetRerollGoldCost()
    {
        var cheapestGoldReward = int.MaxValue;
        foreach (var contract in _activeContracts)
        {
            if (contract == null)
                continue;

            cheapestGoldReward = Mathf.Min(cheapestGoldReward, contract.GoldReward);
        }

        return cheapestGoldReward == int.MaxValue ? 0 : cheapestGoldReward;
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
        var selectedContract = GetSelectedContractOrNull();
        if (selectedContract == null)
        {
            GD.PushError("Arena launch failed: no selected contract.");
            return;
        }

        var scene = ArenaScene ?? ResourceLoader.Load<PackedScene>("res://scenes/arena.tscn");
        if (scene == null)
        {
            GD.PushError("Arena launch failed: scenes/arena.tscn could not be loaded.");
            return;
        }

        _runData?.SetActiveArenaContract(selectedContract);
        SceneTransitionLogger.LogChange(GetTree(), scene, $"contract selected: {selectedContract.DisplayName}");
        GlobalOverlay.Get()?.CloseAllOverlaysImmediate();
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToPacked, scene);
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
        return _selectedContractIndex >= 0 && _selectedContractIndex < _activeContracts.Count
            ? _activeContracts[_selectedContractIndex]
            : null;
    }
}
