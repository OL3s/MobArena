using System;
using System.Collections.Generic;
using Godot;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Contracts;
using MobArena.Scripts.Resources.Gladiators;

namespace MobArena.Scripts;

public static class SaveCommandLineController
{
	private const string HelpFlag = "--help";
	private const string DeleteFlag = "--delete";
	private const string SaveFlag = "--save";
	private const string PrintSaveFlag = "--print-save";
	private const string GenerateCompanyFlag = "--generate-company";
	private const string GenerateCompanyIfMissingFlag = "--generate-company-if-missing";
	private const string GenerateGladiatorFlag = "--generate-gladiator";
	private const string ContractFlag = "--contract";
	private const string CompleteContractFlag = "--complete-contract";
	private const string AddMoneyFlag = "--add-money";
	private const string AddGoldFlag = "--add-gold";
	private const string AddFameFlag = "--add-fame";
	private const string BuyEquipmentFlag = "--buy-equipment";
	private const string BuyGladiatorFlag = "--buy-gladiator";
	private const string CompleteDayFlag = "--complete-day";
	private const string CompleteArenaDayFlag = "--complete-arena-day";
	private const string NextDayFlag = "--next-day";
	private const string WeatherFlag = "--weather";
	private const string GotoSceneFlag = "--goto-scene";
	private const string GotoFlag = "--goto";
	private const string GotoMainMenuFlag = "--goto-main-menu";
	private const string GotoTownFlag = "--goto-town";
	private const string GotoArenaFlag = "--goto-arena";
	private const string MainMenuScenePath = "res://scenes/main_menu.tscn";
	private const string TownScenePath = "res://scenes/town.tscn";
	private const string ArenaScenePath = "res://scenes/arena.tscn";
	private const string HelpText = """
Mob Arena runtime CLI commands:
  --help                                      Print this help text and exit.
  --save                                      Save current runtime state.
  --print-save                                Print current save summary.
  --delete                                    Delete all save data.
  --generate-company-if-missing               Create a default company only when none exists.
  --generate-company                          Create a default company, replacing active company data.
  --generate-gladiator                        Add one default gladiator to the active company.
  --add-money[=amount]                        Add gold. Alias: --add-gold. Missing/invalid amount defaults to 0.
  --add-fame[=amount]                         Add fame. Missing/invalid amount defaults to 0.
  --buy-equipment[=index]                     Buy market item stock by index. Default index: 0.
  --buy-gladiator[=index]                     Buy gladiator market stock by index. Default index: 0.
  --contract[=index]                          Complete visible arena contract by index. Alias: --complete-contract. Default index: 0.
  --complete-day                              Complete current day phase. Alias: --complete-arena-day.
  --next-day                                  Advance from night to the next day.
  --weather[=Cloudy|Sun|Rain|0|1|2]           Set weather. Missing/invalid value defaults to Cloudy.
  --goto-scene=<main-menu|town|arena>         Load a scene resource. Alias: --goto=<scene>.
  --goto-main-menu | --goto-town | --goto-arena

Commands can be stacked left to right and quit automatically. Values use --flag=value; spaces separate commands.
""";

	private readonly record struct CommandLineCommand(string Name, string Value);

	public static bool TryHandle(SaveNode saveNode)
	{
		if (saveNode == null)
			return false;

		var commands = GetCommandLineCommands();
		if (commands.Count <= 0)
			return false;

		var exitCode = 0;
		foreach (var command in commands)
		{
			exitCode = ExecuteCommand(saveNode, command);
			if (exitCode != 0)
				break;
		}

		GameLogger.CLI($"command sequence completed with exit code {exitCode}.");
		saveNode.SuppressExitSaveForCommandLine();
		saveNode.GetTree().Quit(exitCode);
		return true;
	}

	private static int ExecuteCommand(SaveNode saveNode, CommandLineCommand command)
	{
		return command.Name switch
		{
			HelpFlag => HandleHelp(),
			SaveFlag => SaveCommand(saveNode, "save"),
			PrintSaveFlag => HandlePrintSave(saveNode),
			DeleteFlag => HandleSaveDelete(saveNode),
			GenerateCompanyFlag => HandleCompanyGeneration(saveNode, true),
			GenerateCompanyIfMissingFlag => HandleCompanyGeneration(saveNode, false),
			GenerateGladiatorFlag => HandleGenerateGladiator(saveNode),
			ContractFlag or CompleteContractFlag => HandleCompleteContract(saveNode, command),
			AddMoneyFlag or AddGoldFlag => HandleAddGold(saveNode, command),
			AddFameFlag => HandleAddFame(saveNode, command),
			BuyEquipmentFlag => HandleBuyEquipment(saveNode, command),
			BuyGladiatorFlag => HandleBuyGladiator(saveNode, command),
			CompleteDayFlag or CompleteArenaDayFlag => HandleCompleteArenaDay(saveNode),
			NextDayFlag => HandleNextDay(saveNode),
			WeatherFlag => HandleWeather(saveNode, command),
			GotoSceneFlag or GotoFlag => HandleGotoScene(saveNode, command.Value),
			GotoMainMenuFlag => HandleGotoScene(saveNode, "main-menu"),
			GotoTownFlag => HandleGotoScene(saveNode, "town"),
			GotoArenaFlag => HandleGotoScene(saveNode, "arena"),
			_ => 0
		};
	}

	private static int HandleHelp()
	{
		GameLogger.CLI(HelpText);
		return 0;
	}

	private static int HandleSaveDelete(SaveNode saveNode)
	{
		var error = saveNode.DeleteSave();
		var exitCode = error == Error.Ok ? 0 : 1;
		GameLogger.CLI($"save data delete completed with exit code {exitCode}.");
		return exitCode;
	}

	private static int HandlePrintSave(SaveNode saveNode)
	{
		var loadError = saveNode.Load();
		if (loadError != Error.Ok && loadError != Error.FileNotFound)
		{
			GameLogger.CLI($"print save failed while loading existing data. Error: {loadError}.");
			return 1;
		}

		var runData = saveNode.CompanyRunData;
		var careerData = saveNode.CompanyCareerData;
		var phaseState = saveNode.TownPhaseState;
		var weatherState = saveNode.WeatherState;
		GameLogger.CLI($"save summary: hasCompany={saveNode.HasCompany}, company='{saveNode.CompanyLogoData?.CompanyName ?? "None"}', gold={runData?.Gold ?? 0}, fame={runData?.Fame ?? 0}, gladiators={runData?.Gladiators?.Count ?? 0}, inventory={runData?.Inventory?.Count ?? 0}, contractsCompleted={careerData?.ContractsCompleted ?? 0}, day={phaseState?.CurrentDay ?? 0}, phase={phaseState?.CurrentPhase.ToString() ?? "None"}, weather={weatherState?.CurrentWeather.ToString() ?? "None"}.");
		return 0;
	}

	private static int HandleCompanyGeneration(SaveNode saveNode, bool overwrite)
	{
		var loadError = saveNode.Load();
		if (loadError != Error.Ok && loadError != Error.FileNotFound)
		{
			GameLogger.CLI($"company generation failed while loading existing data. Error: {loadError}.");
			return 1;
		}

		if (saveNode.HasCompany && !overwrite)
		{
			GameLogger.CLI($"company generation skipped; a company already exists.");
			return 0;
		}

		GenerateCompany(saveNode);
		var saveError = saveNode.Save();
		var exitCode = saveError == Error.Ok ? 0 : 1;
		GameLogger.CLI($"company generation completed with exit code {exitCode}.");
		return exitCode;
	}

	private static int HandleGenerateGladiator(SaveNode saveNode)
	{
		if (!TryLoadCompany(saveNode, "generate gladiator"))
			return 1;

		saveNode.CompanyRunData.AddGladiator(GladiatorGenerator.CreateDefault(), saveNode.CompanyCareerData);
		var saveError = saveNode.Save();
		var exitCode = saveError == Error.Ok ? 0 : 1;
		GameLogger.CLI($"generate gladiator completed with exit code {exitCode}.");
		return exitCode;
	}

	private static int HandleCompleteContract(SaveNode saveNode, CommandLineCommand command)
	{
		if (!TryLoadCompany(saveNode, "complete contract"))
			return 1;

		var result = ArenaContractResultResolver.ResolveVisibleContractWin(saveNode, GetOptionalIndex(command));
		if (result != ArenaContractResultResolver.ContractResult.Completed)
		{
			GameLogger.CLI($"complete contract failed; resolver returned {result}.");
			return 1;
		}

		var saveError = saveNode.Save();
		var exitCode = saveError == Error.Ok ? 0 : 1;
		GameLogger.CLI($"complete contract completed with exit code {exitCode}.");
		return exitCode;
	}

	private static int HandleAddGold(SaveNode saveNode, CommandLineCommand command)
	{
		if (!TryLoadCompany(saveNode, "add money"))
			return 1;

		var amount = GetOptionalAmount(command);
		saveNode.CompanyRunData.AddGold(amount, saveNode.CompanyCareerData);
		return SaveCommand(saveNode, "add money");
	}

	private static int HandleAddFame(SaveNode saveNode, CommandLineCommand command)
	{
		if (!TryLoadCompany(saveNode, "add fame"))
			return 1;

		var amount = GetOptionalAmount(command);
		saveNode.CompanyRunData.AddFame(amount);
		return SaveCommand(saveNode, "add fame");
	}

	private static int HandleBuyEquipment(SaveNode saveNode, CommandLineCommand command)
	{
		if (!TryLoadCompany(saveNode, "buy equipment"))
			return 1;

		if (!saveNode.CompanyRunData.TryBuyMarketItem(GetOptionalIndex(command)))
			return 1;

		return SaveCommand(saveNode, "buy equipment");
	}

	private static int HandleBuyGladiator(SaveNode saveNode, CommandLineCommand command)
	{
		if (!TryLoadCompany(saveNode, "buy gladiator"))
			return 1;

		if (!saveNode.CompanyRunData.TryBuyMarketGladiator(GetOptionalIndex(command), saveNode.CompanyCareerData))
			return 1;

		return SaveCommand(saveNode, "buy gladiator");
	}

	private static int HandleCompleteArenaDay(SaveNode saveNode)
	{
		if (!TryLoadCompany(saveNode, "complete day"))
			return 1;

		if (!PhaseTransitionController.CompleteArenaDay(saveNode.TownPhaseState, saveNode.CompanyRunData, saveNode.WeatherState))
		{
			GameLogger.CLI($"complete day failed; town is not in day phase.");
			return 1;
		}

		return SaveCommand(saveNode, "complete day");
	}

	private static int HandleNextDay(SaveNode saveNode)
	{
		if (!TryLoadCompany(saveNode, "next day"))
			return 1;

		if (!PhaseTransitionController.AdvanceToNextDay(saveNode.TownPhaseState, saveNode.CompanyRunData, saveNode.WeatherState))
		{
			GameLogger.CLI($"next day failed; town is not in night phase.");
			return 1;
		}

		return SaveCommand(saveNode, "next day");
	}

	private static int HandleWeather(SaveNode saveNode, CommandLineCommand command)
	{
		if (!TryLoadCompany(saveNode, "weather"))
			return 1;

		saveNode.WeatherState.SetWeather(GetWeatherValue(command.Value));
		return SaveCommand(saveNode, "weather");
	}

	private static bool TryLoadCompany(SaveNode saveNode, string actionName)
	{
		var loadError = saveNode.Load();
		if (loadError != Error.Ok && loadError != Error.FileNotFound)
		{
			GameLogger.CLI($"{actionName} failed while loading existing data. Error: {loadError}.");
			return false;
		}

		if (saveNode.HasCompany)
			return true;

		GameLogger.CLI($"{actionName} failed; no active company exists.");
		return false;
	}

	private static int GetOptionalAmount(CommandLineCommand command)
	{
		if (int.TryParse(command.Value, out var amount) && amount >= 0)
			return amount;

		GameLogger.CLI($"{command.Name} value missing or invalid; defaulting to 0.");
		return 0;
	}

	private static int GetOptionalIndex(CommandLineCommand command)
	{
		return int.TryParse(command.Value, out var index) && index >= 0 ? index : 0;
	}

	private static WeatherState.WeatherVisual GetWeatherValue(string value)
	{
		if (int.TryParse(value, out var index) && Enum.IsDefined(typeof(WeatherState.WeatherVisual), index))
			return (WeatherState.WeatherVisual)index;

		if (!string.IsNullOrWhiteSpace(value) && Enum.TryParse<WeatherState.WeatherVisual>(value, true, out var weather))
			return weather;

		GameLogger.CLI($"--weather value missing or invalid; defaulting to Cloudy.");
		return WeatherState.WeatherVisual.Cloudy;
	}

	private static int SaveCommand(SaveNode saveNode, string actionName)
	{
		var saveError = saveNode.Save();
		var exitCode = saveError == Error.Ok ? 0 : 1;
		GameLogger.CLI($"{actionName} completed with exit code {exitCode}.");
		return exitCode;
	}

	private static int HandleGotoScene(SaveNode saveNode, string sceneName)
	{
		var scenePath = GetScenePath(sceneName);
		if (string.IsNullOrWhiteSpace(scenePath))
		{
			GameLogger.CLI($"goto scene failed; unknown scene '{sceneName}'. Expected main-menu, town, or arena.");
			return 1;
		}

		var scene = ResourceLoader.Load<PackedScene>(scenePath);
		if (scene == null)
		{
			GameLogger.CLI($"goto scene failed; could not load '{scenePath}'.");
			return 1;
		}

		GameLogger.CLI($"loaded scene resource '{scenePath}'.");
		return 0;
	}

	private static string GetScenePath(string sceneName)
	{
		return sceneName?.Trim().ToLowerInvariant() switch
		{
			"main" or "main-menu" or "menu" => MainMenuScenePath,
			"town" => TownScenePath,
			"arena" => ArenaScenePath,
			_ => string.Empty
		};
	}

	private static void GenerateCompany(SaveNode saveNode)
	{
		var logoData = CompanyLogoData.CreateDefault();
		logoData.SetCompanyName("Command-Line Company");
		saveNode.CompanyLogoData.CopyFrom(logoData);
		saveNode.StartNewCompanyRun();
		saveNode.HasCompany = true;
	}

	private static List<CommandLineCommand> GetCommandLineCommands()
	{
		var commands = new List<CommandLineCommand>();
		var args = OS.GetCmdlineUserArgs();
		if (args.Length <= 0)
			args = OS.GetCmdlineArgs();

		for (var index = 0; index < args.Length; index++)
		{
			var arg = args[index];
			var separatorIndex = arg.IndexOf('=');
			var commandName = separatorIndex >= 0 ? arg[..separatorIndex] : arg;
			if (!IsCommandLineCommand(commandName))
				continue;

			var value = separatorIndex >= 0 ? arg[(separatorIndex + 1)..] : string.Empty;
			commands.Add(new CommandLineCommand(commandName, value));
		}

		return commands;
	}

	private static bool IsCommandLineCommand(string arg)
	{
		return arg switch
		{
			HelpFlag => true,
			SaveFlag => true,
			PrintSaveFlag => true,
			DeleteFlag => true,
			GenerateCompanyFlag or GenerateCompanyIfMissingFlag => true,
			GenerateGladiatorFlag or ContractFlag or CompleteContractFlag => true,
			AddMoneyFlag or AddGoldFlag => true,
			AddFameFlag => true,
			BuyEquipmentFlag or BuyGladiatorFlag => true,
			CompleteDayFlag or CompleteArenaDayFlag or NextDayFlag or WeatherFlag => true,
			GotoSceneFlag or GotoFlag or GotoMainMenuFlag or GotoTownFlag or GotoArenaFlag => true,
			_ => false
		};
	}

}
