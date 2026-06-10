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
    private const string FameIconPath = "res://assets/ui/icons/fame.svg";
    private const float SkipContractFameMultiplier = 0.9f;

    [Export]
    public PackedScene ArenaScene { get; set; }

    [Export]
    public PackedScene ContractCardScene { get; set; }

    [Export]
    public PackedScene ControlConfigOverlayScene { get; set; }

    [Export]
    public PackedScene AssignedGladiatorButtonScene { get; set; }

    [Export]
    public Array<ArenaContractData> Contracts { get; private set; } = new();

    private ScrollContainer _contractsScroll;
    private HBoxContainer _contractsRow;
    private Control _missingGladiatorMessage;
    private HBoxContainer _assignedGladiatorsRow;
    private Button _assignedGladiatorsGrabButton;
    private HBoxContainer _assignedGladiators;
    private Button _autoAssignButton;
    private OptionButton _autoAssignCountOptions;
    private Button _startButton;
    private Button _rerollButton;
    private Button _skipButton;
    private Label _skipFameLossLabel;
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
        _assignedGladiatorsGrabButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/GrabIcon");
        _assignedGladiators = GetNode<HBoxContainer>("CenterContainer/Panel/MarginContainer/Layout/Actions/AssignedGladiatorsRow/Gladiators");
        _autoAssignButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/AutoAssignRow/AutoAssignButton");
        _autoAssignCountOptions = GetNode<OptionButton>("CenterContainer/Panel/MarginContainer/Layout/Actions/AutoAssignRow/AutoAssignCountOptions");
        _startButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/StartButton");
        _rerollButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Header/RerollButton");
        _skipButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Header/SkipButton");
        _skipFameLossLabel = GetNode<Label>("CenterContainer/Panel/MarginContainer/Layout/Header/SkipButton/CenterContainer/Row/FameLossLabel");
        _closeButton = GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/CloseButton");
        var saveNode = SaveNode.Get();
        _runData = saveNode?.CompanyRunData;
        _careerData = saveNode?.CompanyCareerData;
        _phaseState = saveNode?.TownPhaseState;

        _startButton.Pressed += OnStartPressed;
        _assignedGladiatorsGrabButton.Pressed += OnAssignedGladiatorsGrabPressed;
        _autoAssignButton.Pressed += OnAutoAssignPressed;
        _autoAssignCountOptions.ItemSelected += OnAutoAssignCountSelected;
        _rerollButton.Pressed += OnRerollPressed;
        _skipButton.Pressed += OnSkipPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Header/DonateButton").Pressed += OnDonatePressed;
        _closeButton.Pressed += QueueFree;
        if (_runData != null)
            _runData.RunChanged += RefreshUi;
        if (_careerData != null)
            _careerData.CareerChanged += RefreshUi;

        BuildContracts();
        ConfigureAutoAssignCountOptions();
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_assignedGladiatorsGrabButton != null)
            _assignedGladiatorsGrabButton.Pressed -= OnAssignedGladiatorsGrabPressed;

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
                AddAssignedGladiatorButton(gladiator);
        }
    }

    private void AddAssignedGladiatorButton(GladiatorData gladiator)
    {
        var button = AssignedGladiatorButtonScene?.Instantiate<AssignedArenaGladiatorButton>();
        if (button == null)
        {
            GD.PushError("Assigned arena gladiator button scene is missing or has the wrong root script.");
            return;
        }

        button.Configure(gladiator);
        button.DragRequested += StartGladiatorDrag;
        _assignedGladiators.AddChild(button);
    }

    private void RefreshActions()
    {
		var assignedCount = _runData?.TownAssignments?.ArenaGladiators?.Count ?? 0;
		var selectedContract = GetSelectedContractOrNull();
		var hasContract = selectedContract != null;
		var canStartArena = SaveNode.Get()?.CanStartArenaContract() == true;
		var rerollCost = GetRerollGoldCost();
		var isFirstContract = IsFirstContractOnboarding();

		_startButton.Disabled = !canStartArena || !hasContract || assignedCount <= 0;
		_rerollButton.Visible = !isFirstContract;
		_skipButton.Visible = !isFirstContract;
		_rerollButton.Disabled = _runData == null || _runData.Gold < rerollCost;
		_skipFameLossLabel.Text = GetSkipContractFameLoss().ToString();
		_skipButton.Disabled = !CanSkipDailyContract();
		RefreshAutoAssignActions();
		_rerollButton.Text = $"Reroll {rerollCost}";
		_rerollButton.TooltipText = $"Spend {rerollCost} gold to reroll all contracts.";
		_skipButton.TooltipText = GetSkipContractTooltip();

		if (!canStartArena)
		{
			_startButton.Text = "Demo Complete";
			_startButton.TooltipText = "The demo is complete for this company.";
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

    private void ConfigureAutoAssignCountOptions()
    {
        _autoAssignCountOptions.Clear();
        for (var count = 1; count <= LocalInputConfig.MaxLocalPlayers; count++)
            _autoAssignCountOptions.AddItem(count.ToString(), count);

        _autoAssignCountOptions.Select(Mathf.Clamp(GetAutoAssignCount() - 1, 0, LocalInputConfig.MaxLocalPlayers - 1));
    }

    private void RefreshAutoAssignActions()
    {
        var requestedCount = GetAutoAssignCount();
        var healthyCount = GetHealthyArenaAvailableGladiatorCount();
        _autoAssignButton.Disabled = _runData == null || healthyCount < requestedCount;
        _autoAssignButton.TooltipText = _autoAssignButton.Disabled
            ? $"Need {requestedCount} idle healthy gladiator(s). {healthyCount} idle gladiator(s) are above the low-health warning threshold."
            : $"Assign {requestedCount} idle healthy gladiator(s) to the Arena.";
    }

    private void OnAutoAssignCountSelected(long index)
    {
        var selectedId = _autoAssignCountOptions.GetItemId((int)index);
        var settings = SaveNode.Get()?.SettingsConfig;
        if (settings != null)
            settings.ArenaAutoAssignCount = Mathf.Clamp(selectedId, 1, LocalInputConfig.MaxLocalPlayers);

        RefreshActions();
    }

    private void OnAutoAssignPressed()
    {
        var requestedCount = GetAutoAssignCount();
        if (_runData == null)
            return;

        var previousArenaGladiators = new Array<GladiatorData>(_runData.TownAssignments.ArenaGladiators);
        foreach (var gladiator in previousArenaGladiators)
        {
            if (gladiator != null)
                _runData.TryMoveGladiatorToCourtyard(gladiator);
        }

        var healthyGladiators = GetHealthyArenaAvailableGladiators();
        if (healthyGladiators.Count < requestedCount)
            return;

        _runData.ClearArenaControlAssignments();
        for (var index = 0; index < requestedCount; index++)
            _runData.TryAssignGladiatorToTownLocation(healthyGladiators[index], TownAssignmentData.AssignmentLocation.Arena, LocalInputConfig.MaxLocalPlayers);

        SaveNode.Get()?.Save();
        RefreshUi();
    }

    private void OnAssignedGladiatorsGrabPressed()
    {
        var assigned = _runData?.TownAssignments?.ArenaGladiators;
        if (assigned == null || assigned.Count <= 0)
            return;

        var assignedCopy = new Array<GladiatorData>(assigned);
        foreach (var gladiator in assignedCopy)
        {
            if (gladiator != null)
                _runData.TryMoveGladiatorToCourtyard(gladiator);
        }

        _runData.ClearArenaControlAssignments();
        SaveNode.Get()?.Save();
        RefreshUi();
    }

    private int GetAutoAssignCount()
    {
        return Mathf.Clamp(SaveNode.Get()?.SettingsConfig?.ArenaAutoAssignCount ?? 1, 1, LocalInputConfig.MaxLocalPlayers);
    }

    private int GetHealthyArenaAvailableGladiatorCount()
    {
        return GetHealthyArenaAvailableGladiators().Count;
    }

    private Array<GladiatorData> GetHealthyArenaAvailableGladiators()
    {
        var healthyGladiators = new Array<GladiatorData>();
        var warningRatio = SaveNode.Get()?.SettingsConfig?.LowHealthWarningRatio ?? 0.6f;
        _runData?.EnsureResources();
        var gladiators = _runData?.TownAssignments?.CourtyardGladiators;
        if (gladiators == null)
            return healthyGladiators;

        foreach (var gladiator in gladiators)
        {
            if (gladiator == null || gladiator.Health <= 0)
                continue;

            var healthRatio = gladiator.MaxHealth <= 0 ? 0f : gladiator.Health / (float)gladiator.MaxHealth;
            if (healthRatio >= Mathf.Clamp(warningRatio, 0f, 1f))
                healthyGladiators.Add(gladiator);
        }

        return healthyGladiators;
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
		if (SaveNode.Get()?.CanStartArenaContract() != true)
			return;

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

    private void OnSkipPressed()
    {
        if (!CanSkipDailyContract())
            return;

        var fameLoss = GetSkipContractFameLoss();
        var message = $"You will lose\n[img width=30 height=30]{FameIconPath}[/img] {fameLoss}\nfame if you skip the contract.";

        GlobalOverlay.Get()?.ShowGoCancelPopup(
            "Skip Daily Contract?",
            message,
            SkipDailyContract,
            "Continue",
            "Cancel",
            pauseGameUntilClosed: true);
    }

    private void SkipDailyContract()
    {
        if (!CanSkipDailyContract())
            return;

        var previousFame = _runData.Fame;
        var fameLoss = GetSkipContractFameLoss();
        if (!PhaseTransitionController.SkipArenaContract(_phaseState, _runData, SaveNode.Get()?.WeatherState))
            return;

        if (fameLoss > 0)
            _runData.LoseFame(fameLoss);

        SaveNode.Get()?.Save();
        GD.Print($"ArenaContractsOverlay: Skipped daily contract. Fame {previousFame} -> {_runData.Fame}.");
        QueueFree();
    }

    private int GetSkipContractFameLoss()
    {
        var currentFame = Mathf.Max(0, _runData?.Fame ?? 0);
        var nextFame = Mathf.FloorToInt(currentFame * SkipContractFameMultiplier);
        return Mathf.Max(0, currentFame - nextFame);
    }

    private string GetSkipContractTooltip()
    {
        if (_phaseState?.IsChampionDay == true)
            return "Champion Day contracts cannot be skipped.";
        if (_careerData?.HasCompletedContracts != true && SaveNode.Get().SkipTutorial != true)
            return "Complete your first contract before skipping daily contracts.";
        if (_phaseState?.IsDay() != true)
            return "Daily contracts can only be skipped during the day.";

        var fameLoss = GetSkipContractFameLoss();
        return fameLoss > 0
            ? $"Skip today's arena contract and lose {fameLoss} fame."
            : "Skip today's arena contract.";
    }

    private bool CanSkipDailyContract()
    {
        return _runData != null
            && _phaseState?.IsDay() == true
            && _phaseState.IsChampionDay != true
            && (_careerData?.HasCompletedContracts == true || SaveNode.Get().SkipTutorial);
    }

    private bool IsFirstContractOnboarding()
    {
        return _careerData?.HasCompletedContracts != true && SaveNode.Get().SkipTutorial != true;
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

        return cheapestGoldReward == int.MaxValue ? 0 : Mathf.Max(1, Mathf.CeilToInt(cheapestGoldReward / 2f));
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
        if (SaveNode.Get()?.CanStartArenaContract() != true)
        {
            GD.Print("Arena launch blocked: demo is complete.");
            return;
        }

        var selectedContract = GetSelectedContractOrNull();
        if (selectedContract == null)
        {
            GD.Print("Arena launch failed: no selected contract.");
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
