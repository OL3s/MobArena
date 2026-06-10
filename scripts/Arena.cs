using Godot;
using MobArena.Scenes.Components.Arena;
using MobArena.Scenes.Components.Arena.CombatUi;
using MobArena.Scenes.Components.Environment;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Contracts;

namespace MobArena.Scripts;

public partial class Arena : Node
{
	private const string TownScene = "res://scenes/town.tscn";
	private const string MainMenuScene = "res://scenes/main_menu.tscn";
	private const string DemoCompletePopupTitle = "Thanks for Playing";
	private const string DemoCompletePopupText = "Thanks for playing the demo. You defeated the first champion and reached the end of this demo build.";
	private const string VictoryPopupTitle = "Victory";
	private const string GoldIconPath = "res://assets/ui/icons/gold.svg";
	private const string FameIconPath = "res://assets/ui/icons/fame.svg";
	private const double VictoryPopupDelaySeconds = 3.0;

	private SaveNode _saveNode;
	private EnvironmentVisualOverlay _environmentOverlay;
	private WeatherState _weatherState;
	private CompanyRunData _runData;
	private TownPhaseState _phaseState;
	private ArenaPlayerSpawner _playerSpawner;
	private ArenaEnemySpawner _enemySpawner;
	private CombatHud _combatHud;
	private Control _devStatusPanel;
	private Label _statusLabel;
	private Button _devWinButton;
	private Button _devLoseButton;
	private bool _isResolvingContract;
	private bool _victoryRequested;
	private int _pendingVictoryGoldReward;
	private int _pendingVictoryFameReward;

	public override void _Ready()
	{
		_saveNode = SaveNode.Get();
		_saveNode.DevModeChanged += RefreshDevControls;
		if (_saveNode?.CanStartArenaContract() != true)
		{
			CallDeferred(MethodName.ShowDemoCompleteAndReturnToMainMenu);
			return;
		}

		_environmentOverlay = GetNodeOrNull<EnvironmentVisualOverlay>("EnvironmentOverlay");
		_weatherState = _saveNode?.WeatherState;
		_runData = _saveNode?.CompanyRunData;
		_phaseState = _saveNode?.TownPhaseState;
		_playerSpawner = GetNodeOrNull<ArenaPlayerSpawner>("World/PlayerSpawner");
		_enemySpawner = GetNodeOrNull<ArenaEnemySpawner>("World/EnemySpawner");
		_combatHud = GetNodeOrNull<CombatHud>("CombatHud");

		if (_weatherState != null)
			_weatherState.WeatherChanged += RefreshWeatherVisuals;

		_devStatusPanel = GetNodeOrNull<Control>("ControllerUi/StatusPanel");
		_statusLabel = GetNodeOrNull<Label>("ControllerUi/StatusPanel/Row/Status");
		_devWinButton = GetNode<Button>("ControllerUi/StatusPanel/Row/DevWinButton");
		_devLoseButton = GetNode<Button>("ControllerUi/StatusPanel/Row/DevLoseButton");
		_devWinButton.Pressed += RequestArenaVictory;
		_devLoseButton.Pressed += ResolveContractLoss;

		RefreshDevControls();
		RefreshWeatherVisuals();
		SpawnContractActors();
		RefreshStatus();
	}

	public override void _Process(double delta)
	{
		TryResolveAllPlayersDefeated();
	}

	public override void _ExitTree()
	{
		if (_weatherState != null)
			_weatherState.WeatherChanged -= RefreshWeatherVisuals;
		if (_saveNode != null)
			_saveNode.DevModeChanged -= RefreshDevControls;

		if (_devWinButton != null)
			_devWinButton.Pressed -= RequestArenaVictory;
		if (_devLoseButton != null)
			_devLoseButton.Pressed -= ResolveContractLoss;
	}

	public async void RequestArenaVictory()
	{
		if (_victoryRequested || _isResolvingContract)
			return;

		_victoryRequested = true;
		_isResolvingContract = true;
		StorePendingVictoryRewards();
		SetPlayerDeathPreventionEnabled(true);
		GD.Print($"Arena: victory requested; popup in {VictoryPopupDelaySeconds:0.#} seconds, gold={_pendingVictoryGoldReward}, fame={_pendingVictoryFameReward}.");

		await ToSignal(GetTree().CreateTimer(VictoryPopupDelaySeconds), Timer.SignalName.Timeout);
		if (!IsInsideTree())
			return;

		SnapshotPlayerRuntimeHealthToRunData();
		SetPlayerDamageLocked(true);
		ShowVictoryPopup();
	}

	private void ResolveVictoryAndReturn()
	{
		var result = ArenaContractResultResolver.ResolveWin(_saveNode);
		if (result == ArenaContractResultResolver.ContractResult.DemoComplete)
		{
			SaveAndReturnToMainMenuAfterDemo();
			return;
		}

		if (result != ArenaContractResultResolver.ContractResult.Completed)
		{
			_isResolvingContract = false;
			return;
		}

		SaveAndReturnToTown("arena win resolved");
	}

	public void ResolveContractLoss()
	{
		if (_isResolvingContract)
			return;

		_isResolvingContract = true;
		var result = ArenaContractResultResolver.ResolveLoss(_saveNode);
		if (result == ArenaContractResultResolver.ContractResult.ForceRetired)
		{
			SceneTransitionLogger.LogChange(GetTree(), MainMenuScene, "arena loss force retired");
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScene);
			return;
		}

		if (result == ArenaContractResultResolver.ContractResult.Completed)
		{
			SaveAndReturnToTown("arena loss resolved");
			return;
		}

		_isResolvingContract = false;
	}

	public void ResolveContractForfeit()
	{
		if (_isResolvingContract)
			return;

		_isResolvingContract = true;
		if (ArenaContractResultResolver.ResolveForfeit(_saveNode) == ArenaContractResultResolver.ContractResult.Completed)
		{
			SaveAndReturnToTown("arena forfeit resolved");
			return;
		}

		_isResolvingContract = false;
	}

	private void SaveAndReturnToTown(string reason)
	{
		var saveError = _saveNode?.Save() ?? Error.Unavailable;
		GD.Print($"Arena: Save before town transition returned {saveError}.");
		SceneTransitionLogger.LogChange(GetTree(), TownScene, reason);
		GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, TownScene);
	}

	private void SaveAndReturnToMainMenuAfterDemo()
	{
		var saveError = _saveNode?.Save() ?? Error.Unavailable;
		GD.Print($"Arena: Save before demo completion transition returned {saveError}.");
		ShowDemoCompleteAndReturnToMainMenu();
	}

	private void ShowDemoCompleteAndReturnToMainMenu()
	{
		GlobalOverlay.Get()?.ShowBlurredPopup(
			DemoCompletePopupTitle,
			DemoCompletePopupText,
			closedAction: () =>
			{
				SceneTransitionLogger.LogChange(GetTree(), MainMenuScene, "demo complete");
				GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScene);
			},
			pauseGameUntilClosed: true,
			okText: "Menu");
	}

	private void TryResolveAllPlayersDefeated()
	{
		if (_victoryRequested || _isResolvingContract || _playerSpawner == null || _runData?.ActiveArenaContract == null)
			return;

		var playerCount = 0;
		var defeatedCount = 0;
		foreach (var player in _playerSpawner.GetSpawnedPlayerCombatants())
		{
			playerCount++;
			if (player.IsDead)
				defeatedCount++;
		}

		if (playerCount <= 0 || defeatedCount < playerCount)
			return;

		_isResolvingContract = true;
		GD.Print($"Arena: all spawned players defeated ({defeatedCount}/{playerCount}).");
		var result = ArenaContractResultResolver.ResolveAllPlayersDefeated(_saveNode);
		if (result == ArenaContractResultResolver.ContractResult.ForceRetired)
		{
			SceneTransitionLogger.LogChange(GetTree(), MainMenuScene, "arena all players defeated force retired");
			GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, MainMenuScene);
			return;
		}

		if (result == ArenaContractResultResolver.ContractResult.Completed)
		{
			SaveAndReturnToTown("arena all players defeated resolved");
			return;
		}

		_isResolvingContract = false;
	}

	private void RefreshDevControls()
	{
		var visible = _saveNode?.DevEnabled == true;
		if (_devStatusPanel != null)
			_devStatusPanel.Visible = visible;
		_devWinButton.Visible = visible;
		_devLoseButton.Visible = visible;
		_devLoseButton.Text = _runData?.ActiveArenaContract?.IsChampionContract() == true
			? "Dev Lose"
			: "Dev Forfeit";
	}

	private void SpawnContractActors()
	{
		_runData?.EnsureResources();
		var contract = _runData?.ActiveArenaContract;
		var assignedPlayers = _runData?.TownAssignments?.ArenaGladiators?.Count ?? 0;
		var expectedEnemies = contract?.GetEnemyMobs()?.Count ?? 0;
		GD.Print($"Arena: setup start; contract='{contract?.DisplayName ?? "none"}', assignedPlayers={assignedPlayers}, expectedEnemies={expectedEnemies}, day={_phaseState?.CurrentDay.ToString() ?? "unknown"}, phase={_phaseState?.CurrentPhase.ToString() ?? "unknown"}.");
		_playerSpawner?.SpawnFromRunData(_runData);
		_enemySpawner?.SpawnMobs(contract?.GetEnemyMobs());
		ConnectEnemyDeathChecks();
		_combatHud?.SetPlayers(_playerSpawner?.GetSpawnedPlayerCombatants());
		GD.Print($"Arena: setup complete; spawnedPlayers={_playerSpawner?.SpawnedPlayerCount ?? 0}/{assignedPlayers}, spawnedEnemies={_enemySpawner?.SpawnedEnemyCount ?? 0}/{expectedEnemies}.");
	}

	private void ConnectEnemyDeathChecks()
	{
		if (_enemySpawner == null)
			return;

		foreach (var enemy in _enemySpawner.GetSpawnedEnemyCombatants())
			enemy.CombatantStateChanged += OnEnemyCombatantStateChanged;
	}

	private void OnEnemyCombatantStateChanged(ArenaCombatantState state)
	{
		if (state == ArenaCombatantState.Dead)
			TryRequestVictoryIfAllEnemiesDefeated();
	}

	private void TryRequestVictoryIfAllEnemiesDefeated()
	{
		if (_victoryRequested || _isResolvingContract || _enemySpawner == null)
			return;

		var enemyCount = 0;
		var deadCount = 0;
		foreach (var enemy in _enemySpawner.GetSpawnedEnemyCombatants())
		{
			enemyCount++;
			if (enemy.IsDead)
				deadCount++;
		}

		if (enemyCount <= 0 || deadCount < enemyCount)
			return;

		GD.Print($"Arena: all spawned enemies defeated ({deadCount}/{enemyCount}).");
		RequestArenaVictory();
	}

	private void StorePendingVictoryRewards()
	{
		var contract = _runData?.ActiveArenaContract;
		_pendingVictoryGoldReward = contract?.GoldReward ?? 0;
		_pendingVictoryFameReward = contract?.GetNetFameReward(_runData?.Fame ?? 0) ?? 0;
	}

	private void SetPlayerDeathPreventionEnabled(bool enabled)
	{
		if (_playerSpawner == null)
			return;

		foreach (var player in _playerSpawner.GetSpawnedPlayerCombatants())
			player.SetDeathPreventionEnabled(enabled);
	}

	private void SetPlayerDamageLocked(bool locked)
	{
		if (_playerSpawner == null)
			return;

		foreach (var player in _playerSpawner.GetSpawnedPlayerCombatants())
			player.SetDamageLocked(locked);
	}

	private void SnapshotPlayerRuntimeHealthToRunData()
	{
		if (_playerSpawner == null)
			return;

		foreach (var player in _playerSpawner.GetSpawnedPlayerCombatants())
			player.SnapshotRuntimeHealthToGladiator();
	}

	private void ShowVictoryPopup()
	{
		var globalOverlay = GlobalOverlay.Get();
		if (globalOverlay == null)
		{
			GD.PushError("Arena: victory popup could not open because GlobalOverlay is missing. Resolving victory immediately.");
			ResolveVictoryAndReturn();
			return;
		}

		globalOverlay.ShowBlurredPopup(
			VictoryPopupTitle,
			BuildVictoryPopupText(),
			closedAction: ResolveVictoryAndReturn,
			pauseGameUntilClosed: true,
			okText: "To Town");
	}

	private string BuildVictoryPopupText()
	{
		return "[center]"
			+ "[font_size=18]Your contract is complete.[/font_size]\n\n"
			+ $"[img=42x42]{GoldIconPath}[/img] {FormatRewardValue(_pendingVictoryGoldReward)}\n\n"
			+ $"[img=42x42]{FameIconPath}[/img] {FormatRewardValue(_pendingVictoryFameReward)}"
			+ "[/center]";
	}

	private static string FormatRewardValue(int value)
	{
		var text = value >= 0 ? $"+{value}" : value.ToString();
		var color = value >= 0 ? "#8EE68E" : "#FF8A80";
		return $"[font_size=34][color={color}]{text}[/color][/font_size]";
	}

	private void RefreshStatus()
	{
		if (_statusLabel == null)
			return;

		var contract = _runData?.ActiveArenaContract;
		if (contract == null)
		{
			_statusLabel.Text = "No active contract. Return to town and select a contract.";
			return;
		}

		var playerCount = _runData?.TownAssignments?.ArenaGladiators?.Count ?? 0;
		var enemyCount = contract.GetEnemyMobs().Count;
		_statusLabel.Text = $"{contract.DisplayName}: {playerCount} gladiator(s) vs {enemyCount} enemy/enemies.";
	}

	private void RefreshWeatherVisuals()
	{
		_environmentOverlay?.SetWeather(_weatherState?.CurrentWeather ?? WeatherState.WeatherVisual.Cloudy);
	}
}
