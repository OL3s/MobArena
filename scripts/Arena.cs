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

	private SaveNode _saveNode;
	private EnvironmentVisualOverlay _environmentOverlay;
	private WeatherState _weatherState;
	private CompanyRunData _runData;
	private TownPhaseState _phaseState;
	private ArenaPlayerSpawner _playerSpawner;
	private ArenaEnemySpawner _enemySpawner;
	private CombatHud _combatHud;
	private Label _statusLabel;
	private Button _debugWinButton;
	private Button _debugLoseButton;
	private bool _isResolvingContract;

	public override void _Ready()
	{
		_saveNode = SaveNode.Get();
		_environmentOverlay = GetNodeOrNull<EnvironmentVisualOverlay>("EnvironmentOverlay");
		_weatherState = _saveNode?.WeatherState;
		_runData = _saveNode?.CompanyRunData;
		_phaseState = _saveNode?.TownPhaseState;
		_playerSpawner = GetNodeOrNull<ArenaPlayerSpawner>("World/PlayerSpawner");
		_enemySpawner = GetNodeOrNull<ArenaEnemySpawner>("World/EnemySpawner");
		_combatHud = GetNodeOrNull<CombatHud>("CombatHud");

		if (_weatherState != null)
			_weatherState.WeatherChanged += RefreshWeatherVisuals;

		_statusLabel = GetNodeOrNull<Label>("ControllerUi/StatusPanel/Row/Status");
		_debugWinButton = GetNode<Button>("ControllerUi/StatusPanel/Row/DebugWinButton");
		_debugLoseButton = GetNode<Button>("ControllerUi/StatusPanel/Row/DebugLoseButton");
		_debugWinButton.Pressed += ResolveContractWin;
		_debugLoseButton.Pressed += ResolveContractLoss;

		RefreshDebugButtons();
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

		if (_debugWinButton != null)
			_debugWinButton.Pressed -= ResolveContractWin;
		if (_debugLoseButton != null)
			_debugLoseButton.Pressed -= ResolveContractLoss;
	}

	public void ResolveContractWin()
	{
		if (_isResolvingContract)
			return;

		_isResolvingContract = true;
		if (ArenaContractResultResolver.ResolveWin(_saveNode) != ArenaContractResultResolver.ContractResult.Completed)
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

	private void TryResolveAllPlayersDefeated()
	{
		if (_isResolvingContract || _playerSpawner == null || _runData?.ActiveArenaContract == null)
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

	private void RefreshDebugButtons()
	{
		var visible = _saveNode?.DebugEnabled == true;
		_debugWinButton.Visible = visible;
		_debugLoseButton.Visible = visible;
		_debugLoseButton.Text = _runData?.ActiveArenaContract?.IsChampionContract() == true
			? "Debug Lose"
			: "Debug Forfeit";
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
		_combatHud?.SetPlayers(_playerSpawner?.GetSpawnedPlayerCombatants());
		GD.Print($"Arena: setup complete; spawnedPlayers={_playerSpawner?.SpawnedPlayerCount ?? 0}/{assignedPlayers}, spawnedEnemies={_enemySpawner?.SpawnedEnemyCount ?? 0}/{expectedEnemies}.");
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
