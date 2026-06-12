using System;
using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class SaveNode : Node
{
	public enum SaveDataDeleteScope
	{
		RetireCompany,
		Records,
		Settings,
		All
	}

	private const int SaveVersion = 1;
	private const string SaveDirectory = "user://save";
	private const string ManifestPath = SaveDirectory + "/save.cfg";
	private const string CompanyLogoPath = SaveDirectory + "/company_logo.tres";
	private const string CompanyCareerPath = SaveDirectory + "/company_career.tres";
	private const string CompletedCompanyHistoryPath = SaveDirectory + "/completed_company_history.tres";
	private const string CompanyRunPath = SaveDirectory + "/company_run.tres";
	private const string TownPhasePath = SaveDirectory + "/town_phase.tres";
	private const string WeatherPath = SaveDirectory + "/weather.tres";
	private const string SettingsPath = SaveDirectory + "/settings.tres";

	private string _pendingCompanyLossTitle;
	private string _pendingCompanyLossText;
	private bool _suppressExitSave;

	public event Action RuntimeStateResetting;
	public event Action DevModeChanged;
	public event Action RuntimeTagsChanged;
	public event Action TutorialModeChanged;

    [Export]
    public bool HasCompany { get; set; }

    [Export]
    public CompanyLogoData CompanyLogoData { get; private set; } = CompanyLogoData.CreateDefault();

    [Export]
    public CompanyCareerData CompanyCareerData { get; private set; } = new();

    [Export]
    public CompletedCompanyHistory CompletedCompanyHistory { get; private set; } = new();

    [Export]
    public CompanyRunData CompanyRunData { get; private set; } = new();

	[Export]
	public TownPhaseState TownPhaseState { get; private set; } = new();

	[Export]
	public WeatherState WeatherState { get; private set; } = new();

	[Export]
	public SettingsConfig SettingsConfig { get; private set; } = new();

	public bool DevEnabled => SettingsConfig?.DevEnabled == true;
	public bool IsDemo => SettingsConfig?.IsDemo == true;
	public bool IsDemoComplete => IsDemo && (CompanyCareerData?.ChampionsDefeated ?? 0) > 0;
	public bool SkipTutorial => SettingsConfig?.SkipTutorial == true;
	public bool HasCompletedContractsForProgression => SkipTutorial || CompanyCareerData?.HasCompletedContracts == true;
	public bool HasUnlockedRecoveryBayForProgression => SkipTutorial || (CompanyCareerData?.ContractsCompleted ?? 0) >= 2;
	public bool HasUnlockedTrainingHallForProgression => SkipTutorial || (CompanyCareerData?.ContractsCompleted ?? 0) >= 3;
	public bool HasReachedSpecialtyBuildingsForProgression => SkipTutorial || CompanyCareerData?.HasReachedSpecialtyBuildings == true;

	public bool CanStartArenaContract()
	{
		return CanStartArenaContract(out _);
	}

	public bool CanStartArenaContract(out string blockReason)
	{
		if (IsDemoComplete)
		{
			blockReason = "demo is complete";
			return false;
		}

		if (TownPhaseState?.IsDay() != true)
		{
			var phase = TownPhaseState?.GetPhaseLabel() ?? "unknown";
			blockReason = $"arena contracts can only start during Day; current phase is {phase}";
			return false;
		}

		blockReason = string.Empty;
		return true;
	}

	public void SetDevEnabled(bool devEnabled)
	{
		SettingsConfig ??= new SettingsConfig();
		if (SettingsConfig.DevEnabled == devEnabled)
			return;

		SettingsConfig.DevEnabled = devEnabled;
		GameLogger.Save($"Dev mode {(devEnabled ? "enabled" : "disabled")}.");
		DevModeChanged?.Invoke();
		RuntimeTagsChanged?.Invoke();
	}

	public void SetIsDemo(bool isDemo)
	{
		SettingsConfig ??= new SettingsConfig();
		if (SettingsConfig.IsDemo == isDemo)
			return;

		SettingsConfig.IsDemo = isDemo;
		GameLogger.Save($"Demo mode {(isDemo ? "enabled" : "disabled")}.");
		RuntimeTagsChanged?.Invoke();
	}

	public void SetShowRuntimeTags(bool showRuntimeTags)
	{
		SettingsConfig ??= new SettingsConfig();
		if (SettingsConfig.ShowRuntimeTags == showRuntimeTags)
			return;

		SettingsConfig.ShowRuntimeTags = showRuntimeTags;
		GameLogger.Save($"Runtime tags {(showRuntimeTags ? "shown" : "hidden")}.");
		RuntimeTagsChanged?.Invoke();
	}

	public void SetSkipTutorial(bool skipTutorial)
	{
		SettingsConfig ??= new SettingsConfig();
		if (SettingsConfig.SkipTutorial == skipTutorial)
			return;

		SettingsConfig.SkipTutorial = skipTutorial;
		GameLogger.Save($"Tutorial mode {(skipTutorial ? "disabled" : "enabled")}.");
		TutorialModeChanged?.Invoke();
	}

	public void QueueCompanyLossNotification(string title, string text)
	{
		_pendingCompanyLossTitle = string.IsNullOrWhiteSpace(title) ? "Company Retired" : title;
		_pendingCompanyLossText = string.IsNullOrWhiteSpace(text)
			? "The company has been retired and the run has ended."
			: text;
	}

	public bool TryConsumeCompanyLossNotification(out string title, out string text)
	{
		title = _pendingCompanyLossTitle;
		text = _pendingCompanyLossText;
		_pendingCompanyLossTitle = null;
		_pendingCompanyLossText = null;
		return !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(text);
	}

	public void StartNewCompanyRun()
	{
		CompanyCareerData = new CompanyCareerData();
		CompanyRunData = new CompanyRunData();
		ApplyDebugStartingCondition();
		TownPhaseState = new TownPhaseState();
		WeatherState = new WeatherState();
	}

	private void ApplyDebugStartingCondition()
	{
		if (!DevEnabled || CompanyRunData?.Gladiators == null || CompanyRunData.Gladiators.Count <= 0)
			return;

		var gladiator = CompanyRunData.Gladiators[0];
		if (gladiator == null)
			return;

		gladiator.SetExhaustion(2f);
		gladiator.SetHealth(Mathf.Max(1, Mathf.RoundToInt(gladiator.MaxHealth * 0.35f)));
	}

    public static SaveNode Get()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        var saveNode = sceneTree?.Root?.GetNodeOrNull<SaveNode>("/root/SaveNode");
        if (saveNode != null)
            return saveNode;

        const string message = "SaveNode autoload is missing. Check project.godot autoload registration before loading save-dependent scenes.";
        GD.PushError(message);
        throw new InvalidOperationException(message);
    }

	public override void _Ready()
	{
		SaveCommandLineController.TryHandle(this);
	}

	public override void _ExitTree()
	{
		if (_suppressExitSave)
			return;

		Save();
	}

	public void SuppressExitSaveForCommandLine()
	{
		_suppressExitSave = true;
	}

    public bool HasSave()
    {
		return FileAccess.FileExists(ManifestPath);
    }

	public Error Save()
	{
		GameLogger.Save($"Saving data.");
		var error = EnsureSaveDirectory();
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed while creating save directory. Error: {error}.");
			return error;
		}

		if (!HasCompany)
		{
			error = DeleteActiveCompanyFiles();
			if (error != Error.Ok)
				return error;

			error = SaveResource(CompletedCompanyHistory, CompletedCompanyHistoryPath);
			if (error != Error.Ok)
			{
				GameLogger.Save($"Save failed for completed company history. Error: {error}.");
				return error;
			}

			error = SaveResource(SettingsConfig, SettingsPath);
			if (error != Error.Ok)
			{
				GameLogger.Save($"Save failed for settings. Error: {error}.");
				return error;
			}

			error = SaveManifest(CreateManifest());
			if (error != Error.Ok)
				GameLogger.Error(GameLogCategory.Save, $"Save failed for manifest. Error: {error}.");

			return error;
		}

		error = SaveResource(CompanyLogoData, CompanyLogoPath);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed for company logo. Error: {error}.");
			return error;
		}

		error = SaveResource(CompanyCareerData, CompanyCareerPath);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed for company career. Error: {error}.");
			return error;
		}

		error = SaveResource(CompletedCompanyHistory, CompletedCompanyHistoryPath);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed for completed company history. Error: {error}.");
			return error;
		}

		error = SaveResource(CompanyRunData, CompanyRunPath);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed for company run. Error: {error}.");
			return error;
		}

		error = SaveResource(TownPhaseState, TownPhasePath);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed for town phase. Error: {error}.");
			return error;
		}

		error = SaveResource(WeatherState, WeatherPath);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed for weather. Error: {error}.");
			return error;
		}

		error = SaveResource(SettingsConfig, SettingsPath);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Save failed for settings. Error: {error}.");
			return error;
		}

		error = SaveManifest(CreateManifest());
		if (error != Error.Ok)
			GameLogger.Error(GameLogCategory.Save, $"Save failed for manifest. Error: {error}.");

		return error;
    }

    public Error Load()
    {
		GameLogger.Save($"Loading data.");
		var manifest = new ConfigFile();
		var error = LoadManifest(manifest);
		if (error == Error.FileNotFound)
		{
			GameLogger.Save($"No save manifest found. Using defaults.");
			return error;
		}

		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for manifest. Error: {error}.");
			return error;
		}

		HasCompany = (bool)manifest.GetValue("company", "has_company", false);

		error = LoadResource(GetResourcePath(manifest, "completed_company_history", CompletedCompanyHistoryPath), CompletedCompanyHistory, out var completedCompanyHistory);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for completed company history. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "settings", SettingsPath), SettingsConfig, out var settingsConfig);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for settings. Error: {error}.");
			return error;
		}

		CompletedCompanyHistory = completedCompanyHistory;
		SettingsConfig = settingsConfig;
		if (!HasCompany)
		{
			CompanyLogoData = CompanyLogoData.CreateDefault();
			CompanyCareerData = new CompanyCareerData();
			CompanyRunData = new CompanyRunData();
			TownPhaseState = new TownPhaseState();
			WeatherState = new WeatherState();
			return Error.Ok;
		}

		error = LoadResource(GetResourcePath(manifest, "company_logo", CompanyLogoPath), CompanyLogoData, out var companyLogoData);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for company logo. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "company_career", CompanyCareerPath), CompanyCareerData, out var companyCareerData);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for company career. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "company_run", CompanyRunPath), CompanyRunData, out var companyRunData);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for company run. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "town_phase", TownPhasePath), TownPhaseState, out var townPhaseState);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for town phase. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "weather", WeatherPath), WeatherState, out var weatherState);
		if (error != Error.Ok)
		{
			GameLogger.Save($"Load failed for weather. Error: {error}.");
			return error;
		}

		CompanyLogoData = companyLogoData;
		CompanyCareerData = companyCareerData;
		CompanyRunData = companyRunData;
		CompanyRunData.ApplyGladiatorRecoverableCaps();
		TownPhaseState = townPhaseState;
		WeatherState = weatherState;
		return Error.Ok;
    }

	public Error DeleteSave()
	{
		return DeleteSaveData(SaveDataDeleteScope.All);
	}

	public Error DeleteSettingsData()
	{
		return DeleteSaveData(SaveDataDeleteScope.Settings);
	}

	public Error DeleteCompletedCompanyHistoryData()
	{
		return DeleteSaveData(SaveDataDeleteScope.Records);
	}

	public Error DeleteSaveData(SaveDataDeleteScope scope)
	{
		PrepareRuntimeStateReset();
		LocalInputConfig.Get()?.ClearControllerSetups();
		return scope switch
		{
			SaveDataDeleteScope.RetireCompany => RetireCompanyCore(),
			SaveDataDeleteScope.Settings => DeleteSettingsDataCore(),
			SaveDataDeleteScope.Records => DeleteCompletedCompanyHistoryDataCore(),
			_ => DeleteSaveCore()
		};
	}

	private void PrepareRuntimeStateReset()
	{
		RuntimeStateResetting?.Invoke();
	}

	private Error DeleteSaveCore()
	{
		GameLogger.Save($"Deleting all save data.");
		var error = DeleteSaveDirectoryContents();
		if (error != Error.Ok)
			return error;

		ResetRuntimeState();
		GameLogger.Save($"All save data deleted.");
		return Error.Ok;
	}

	private static Error DeleteSaveDirectoryContents()
	{
		var directoryPath = ProjectSettings.GlobalizePath(SaveDirectory);
		if (!DirAccess.DirExistsAbsolute(directoryPath))
			return Error.Ok;

		return DeleteDirectoryContents(directoryPath);
	}

	private static Error DeleteDirectoryContents(string directoryPath)
	{
		var directory = DirAccess.Open(directoryPath);
		if (directory == null)
			return Error.CantOpen;

		directory.ListDirBegin();
		while (true)
		{
			var entry = directory.GetNext();
			if (string.IsNullOrEmpty(entry))
				break;

			if (entry.StartsWith(".", StringComparison.Ordinal))
				continue;

			var entryPath = $"{directoryPath}/{entry}";
			var error = directory.CurrentIsDir()
				? DeleteDirectoryTree(entryPath)
				: DirAccess.RemoveAbsolute(entryPath);

			if (error != Error.Ok)
			{
				directory.ListDirEnd();
				GD.PushError($"Failed to delete save path '{entryPath}': {error}");
				return error;
			}

			GameLogger.Save($"Deleted save path: {entryPath}");
		}

		directory.ListDirEnd();
		return Error.Ok;
	}

	private static Error DeleteDirectoryTree(string directoryPath)
	{
		var error = DeleteDirectoryContents(directoryPath);
		return error == Error.Ok ? DirAccess.RemoveAbsolute(directoryPath) : error;
	}

	private static Error DeleteActiveCompanyFiles()
	{
		var error = DeleteFileIfExists(CompanyLogoPath, false);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompanyCareerPath, false);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompanyRunPath, false);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(TownPhasePath, false);
		if (error != Error.Ok)
			return error;

		return DeleteFileIfExists(WeatherPath, false);
	}

	private Error RetireCompanyCore()
	{
		if (HasCompany)
		{
			GameLogger.Save($"Retiring current company.");
			TryAddCurrentCompanyToCompletedHistory();
		}
		else
		{
			GameLogger.Save($"Retire company requested, but no active company exists in save data.");
		}

		var error = DeleteFileIfExists(CompanyLogoPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompanyCareerPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompanyRunPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(TownPhasePath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(WeatherPath);
		if (error != Error.Ok)
			return error;

		HasCompany = false;
		CompanyLogoData = CompanyLogoData.CreateDefault();
		CompanyCareerData = new CompanyCareerData();
		CompanyRunData = new CompanyRunData();
		TownPhaseState = new TownPhaseState();
		WeatherState = new WeatherState();
		error = SaveCurrentManifest();
		GameLogger.Save(error == Error.Ok ? "Company retired." : $"Company retirement failed while saving manifest. Error: {error}.");
		return error;
	}

	public bool TryAddCurrentCompanyToCompletedHistory()
	{
		CompletedCompanyHistory ??= new CompletedCompanyHistory();
		var added = CompletedCompanyHistory.TryAddCompletedRun(CompanyLogoData, CompanyCareerData, CompanyRunData?.Fame ?? 0);
		if (added)
			Save();

		return added;
	}

	public Error ForceRetireCurrentCompany()
	{
		if (HasCompany)
		{
			GameLogger.Save($"Force-retiring current company.");
			TryAddCurrentCompanyToCompletedHistory();
		}
		else
		{
			GameLogger.Save($"Force-retire requested, but no active company exists in save data.");
		}

		PrepareRuntimeStateReset();
		LocalInputConfig.Get()?.ClearControllerSetups();
		HasCompany = false;
		CompanyLogoData = CompanyLogoData.CreateDefault();
		CompanyCareerData = new CompanyCareerData();
		CompanyRunData = new CompanyRunData();
		TownPhaseState = new TownPhaseState();
		WeatherState = new WeatherState();
		return Save();
	}

	private Error DeleteSettingsDataCore()
	{
		GameLogger.Save($"Deleting settings data.");
		var error = DeleteFileIfExists(SettingsPath);
		if (error != Error.Ok)
			return error;

		SettingsConfig = new SettingsConfig();
		error = SaveCurrentManifest();
		GameLogger.Save(error == Error.Ok ? "Settings data deleted." : $"Settings data delete failed while saving manifest. Error: {error}.");
		return error;
	}

	private Error DeleteCompletedCompanyHistoryDataCore()
	{
		GameLogger.Save($"Deleting completed company history data.");
		var error = DeleteFileIfExists(CompletedCompanyHistoryPath);
		if (error != Error.Ok)
			return error;

		CompletedCompanyHistory = new CompletedCompanyHistory();
		error = SaveCurrentManifest();
		GameLogger.Save(error == Error.Ok ? "Completed company history data deleted." : $"Completed company history data delete failed while saving manifest. Error: {error}.");
		return error;
	}

	public void ResetRuntimeState()
	{
		HasCompany = false;
		CompanyLogoData = CompanyLogoData.CreateDefault();
		CompanyCareerData = new CompanyCareerData();
		CompletedCompanyHistory = new CompletedCompanyHistory();
		CompanyRunData = new CompanyRunData();
		TownPhaseState = new TownPhaseState();
		WeatherState = new WeatherState();
		SettingsConfig = new SettingsConfig();
	}

	private static Error EnsureSaveDirectory()
	{
		var error = DirAccess.MakeDirRecursiveAbsolute(ProjectSettings.GlobalizePath(SaveDirectory));
		if (error != Error.Ok)
			GD.PushError($"Failed to create save directory '{SaveDirectory}': {error}");

		return error;
	}

	private static Error SaveResource(Resource resource, string path)
	{
		if (resource == null)
			return Error.InvalidData;

		var error = ResourceSaver.Save(resource, path);
		if (error != Error.Ok)
			GD.PushError($"Failed to save resource '{path}': {error}");

		return error;
	}

	private static Error SaveManifest(ConfigFile configFile)
	{
		var error = configFile.Save(ManifestPath);
		if (error != Error.Ok)
			GD.PushError($"Failed to save manifest '{ManifestPath}': {error}");

		return error;
	}

	private Error SaveCurrentManifest()
	{
		var error = EnsureSaveDirectory();
		return error == Error.Ok ? SaveManifest(CreateManifest()) : error;
	}

	private ConfigFile CreateManifest()
	{
		var manifest = new ConfigFile();
		manifest.SetValue("save", "version", SaveVersion);
		manifest.SetValue("company", "has_company", HasCompany);
		manifest.SetValue("resources", "company_logo", CompanyLogoPath);
		manifest.SetValue("resources", "company_career", CompanyCareerPath);
		manifest.SetValue("resources", "completed_company_history", CompletedCompanyHistoryPath);
		manifest.SetValue("resources", "company_run", CompanyRunPath);
		manifest.SetValue("resources", "town_phase", TownPhasePath);
		manifest.SetValue("resources", "weather", WeatherPath);
		manifest.SetValue("resources", "settings", SettingsPath);
		return manifest;
	}

	private static Error LoadManifest(ConfigFile configFile)
	{
		var error = configFile.Load(ManifestPath);
		if (error != Error.Ok && error != Error.FileNotFound)
			GD.PushError($"Failed to load save manifest '{ManifestPath}': {error}");

		return error;
	}

	private static Error LoadResource<T>(string path, T fallback, out T result) where T : Resource
	{
		if (!ResourceLoader.Exists(path))
		{
			result = fallback;
			return Error.Ok;
		}

		var resource = ResourceLoader.Load<T>(path);
		if (resource == null)
		{
			GD.PushError($"Failed to load save resource '{path}'.");
			result = fallback;
			return Error.CantOpen;
		}

		result = resource;
		return Error.Ok;
	}

	private static string GetResourcePath(ConfigFile manifest, string key, string fallback)
	{
		return (string)manifest.GetValue("resources", key, fallback);
	}

	private static Error DeleteFileIfExists(string path, bool printSkipped = true)
	{
		if (!FileAccess.FileExists(path))
		{
			if (printSkipped)
				GameLogger.Save($"Delete skipped; file does not exist: {path}");
			return Error.Ok;
		}

		var error = DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
		if (error != Error.Ok)
			GD.PushError($"Failed to delete save file '{path}': {error}");
		else
			GameLogger.Save($"Deleted file: {path}");

		return error;
	}
}
