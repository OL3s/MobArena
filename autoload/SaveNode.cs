using System;
using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class SaveNode : Node
{
	private const int SaveVersion = 1;
	private const string DeleteFlag = "--delete";
	private const string DeleteSaveDataFlag = "--delete-savedata";
	private const string DeleteStorageFlag = "--del-storage";
	private const string DeleteUserDataFlag = "--delete-user-data";
	private const string SaveDirectory = "user://save";
	private const string ManifestPath = SaveDirectory + "/save.cfg";
	private const string CompanyLogoPath = SaveDirectory + "/company_logo.tres";
	private const string CompanyCareerPath = SaveDirectory + "/company_career.tres";
	private const string CompletedCompanyHistoryPath = SaveDirectory + "/completed_company_history.tres";
	private const string CompanyRunPath = SaveDirectory + "/company_run.tres";
	private const string TownPhasePath = SaveDirectory + "/town_phase.tres";
	private const string SettingsPath = SaveDirectory + "/settings.tres";

	private bool _skipExitSave;

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

	public bool DebugEnabled => SettingsConfig?.DebugEnabled == true;

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
		if (!DebugEnabled || CompanyRunData?.Gladiators == null || CompanyRunData.Gladiators.Count <= 0)
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
		if (TryHandleCommandLineSaveOperation())
			return;

		Load();
	}

	public override void _ExitTree()
	{
		if (_skipExitSave)
			return;

		Save();
	}

	private bool TryHandleCommandLineSaveOperation()
	{
		if (!HasCommandLineFlag(DeleteFlag, DeleteSaveDataFlag, DeleteStorageFlag, DeleteUserDataFlag))
			return false;

		_skipExitSave = true;
		var error = DeleteSave();
		var exitCode = error == Error.Ok ? 0 : 1;
		GD.Print($"SaveNode: Command-line save data delete completed with exit code {exitCode}.");
		GetTree().Quit(exitCode);
		return true;
	}

	private static bool HasCommandLineFlag(params string[] acceptedFlags)
	{
		return ContainsAnyFlag(OS.GetCmdlineUserArgs(), acceptedFlags) || ContainsAnyFlag(OS.GetCmdlineArgs(), acceptedFlags);
	}

	private static bool ContainsAnyFlag(string[] args, string[] acceptedFlags)
	{
		foreach (var arg in args)
		{
			foreach (var acceptedFlag in acceptedFlags)
			{
				if (string.Equals(arg, acceptedFlag, StringComparison.OrdinalIgnoreCase))
					return true;
			}
		}

		return false;
	}

    public bool HasSave()
    {
		return FileAccess.FileExists(ManifestPath);
    }

	public Error Save()
	{
		GD.Print("SaveNode: Saving data.");
		var error = EnsureSaveDirectory();
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Save failed while creating save directory. Error: {error}.");
			return error;
		}

		error = SaveResource(CompanyLogoData, CompanyLogoPath);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Save failed for company logo. Error: {error}.");
			return error;
		}

		error = SaveResource(CompanyCareerData, CompanyCareerPath);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Save failed for company career. Error: {error}.");
			return error;
		}

		error = SaveResource(CompletedCompanyHistory, CompletedCompanyHistoryPath);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Save failed for completed company history. Error: {error}.");
			return error;
		}

		error = SaveResource(CompanyRunData, CompanyRunPath);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Save failed for company run. Error: {error}.");
			return error;
		}

		error = SaveResource(TownPhaseState, TownPhasePath);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Save failed for town phase. Error: {error}.");
			return error;
		}

		error = SaveResource(SettingsConfig, SettingsPath);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Save failed for settings. Error: {error}.");
			return error;
		}

		error = SaveManifest(CreateManifest());
		if (error != Error.Ok)
			GD.PushError($"SaveNode: Save failed for manifest. Error: {error}.");

		return error;
    }

    public Error Load()
    {
		GD.Print("SaveNode: Loading data.");
		var manifest = new ConfigFile();
		var error = LoadManifest(manifest);
		if (error == Error.FileNotFound)
		{
			GD.Print("SaveNode: No save manifest found. Using defaults.");
			return error;
		}

		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Load failed for manifest. Error: {error}.");
			return error;
		}

		HasCompany = (bool)manifest.GetValue("company", "has_company", false);

		error = LoadResource(GetResourcePath(manifest, "company_logo", CompanyLogoPath), CompanyLogoData, out var companyLogoData);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Load failed for company logo. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "company_career", CompanyCareerPath), CompanyCareerData, out var companyCareerData);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Load failed for company career. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "completed_company_history", CompletedCompanyHistoryPath), CompletedCompanyHistory, out var completedCompanyHistory);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Load failed for completed company history. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "company_run", CompanyRunPath), CompanyRunData, out var companyRunData);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Load failed for company run. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "town_phase", TownPhasePath), TownPhaseState, out var townPhaseState);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Load failed for town phase. Error: {error}.");
			return error;
		}

		error = LoadResource(GetResourcePath(manifest, "settings", SettingsPath), SettingsConfig, out var settingsConfig);
		if (error != Error.Ok)
		{
			GD.Print($"SaveNode: Load failed for settings. Error: {error}.");
			return error;
		}

		CompanyLogoData = companyLogoData;
		CompanyCareerData = companyCareerData;
		CompletedCompanyHistory = completedCompanyHistory;
		CompanyRunData = companyRunData;
		CompanyRunData.ApplyGladiatorRecoverableCaps();
		TownPhaseState = townPhaseState;
		WeatherState ??= new WeatherState();
		SettingsConfig = settingsConfig;
		return Error.Ok;
    }

	public Error DeleteSave()
	{
		GD.Print("SaveNode: Deleting all save data.");
		var error = DeleteFileIfExists(ManifestPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompanyLogoPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompanyCareerPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompletedCompanyHistoryPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(CompanyRunPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(TownPhasePath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(SettingsPath);
		if (error != Error.Ok)
			return error;

		ResetRuntimeState();
		GD.Print("SaveNode: All save data deleted.");
		return Error.Ok;
	}

	public Error DeleteCompanyData()
	{
		GD.Print("SaveNode: Deleting company data.");
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

		HasCompany = false;
		CompanyLogoData = CompanyLogoData.CreateDefault();
		CompanyCareerData = new CompanyCareerData();
		CompanyRunData = new CompanyRunData();
		TownPhaseState = new TownPhaseState();
		WeatherState = new WeatherState();
		error = SaveCurrentManifest();
		GD.Print(error == Error.Ok ? "SaveNode: Company data deleted." : $"SaveNode: Company data delete failed while saving manifest. Error: {error}.");
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

	public Error DeleteRunData()
	{
		GD.Print("SaveNode: Deleting run data.");
		var error = DeleteFileIfExists(CompanyRunPath);
		if (error != Error.Ok)
			return error;

		error = DeleteFileIfExists(TownPhasePath);
		if (error != Error.Ok)
			return error;

		CompanyRunData = new CompanyRunData();
		TownPhaseState = new TownPhaseState();
		WeatherState = new WeatherState();
		error = SaveCurrentManifest();
		GD.Print(error == Error.Ok ? "SaveNode: Run data deleted." : $"SaveNode: Run data delete failed while saving manifest. Error: {error}.");
		return error;
	}

	public Error DeleteSettingsData()
	{
		GD.Print("SaveNode: Deleting settings data.");
		var error = DeleteFileIfExists(SettingsPath);
		if (error != Error.Ok)
			return error;

		SettingsConfig = new SettingsConfig();
		error = SaveCurrentManifest();
		GD.Print(error == Error.Ok ? "SaveNode: Settings data deleted." : $"SaveNode: Settings data delete failed while saving manifest. Error: {error}.");
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

		if (ReferencesMissingResourcePaths(path))
		{
			GD.PushWarning($"SaveNode: Save resource '{path}' references missing project resources. Using fresh fallback data for the current refactor.");
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

	private static bool ReferencesMissingResourcePaths(string path)
	{
		if (!FileAccess.FileExists(path))
			return false;

		var text = FileAccess.GetFileAsString(path);
		var searchStart = 0;
		const string marker = "path=\"res://";
		while (true)
		{
			var markerIndex = text.IndexOf(marker, searchStart, StringComparison.Ordinal);
			if (markerIndex < 0)
				return false;

			var pathStart = markerIndex + "path=\"".Length;
			var pathEnd = text.IndexOf('"', pathStart);
			if (pathEnd < 0)
				return false;

			var resourcePath = text[pathStart..pathEnd];
			if (!ResourceLoader.Exists(resourcePath) && !FileAccess.FileExists(resourcePath))
				return true;

			searchStart = pathEnd + 1;
		}
	}

	private static string GetResourcePath(ConfigFile manifest, string key, string fallback)
	{
		return (string)manifest.GetValue("resources", key, fallback);
	}

	private static Error DeleteFileIfExists(string path)
	{
		if (!FileAccess.FileExists(path))
		{
			GD.Print($"SaveNode: Delete skipped; file does not exist: {path}");
			return Error.Ok;
		}

		var error = DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(path));
		if (error != Error.Ok)
			GD.PushError($"Failed to delete save file '{path}': {error}");
		else
			GD.Print($"SaveNode: Deleted file: {path}");

		return error;
	}
}
