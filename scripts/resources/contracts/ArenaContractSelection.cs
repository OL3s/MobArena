using Godot;
using Godot.Collections;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts.Resources.Contracts;

public static class ArenaContractSelection
{
	private const string StarterSlimePitContractPath = "res://resources/contracts/starter_slime_pit.tres";

	public static Array<ArenaContractData> GetVisibleContracts(SaveNode saveNode)
	{
		return GetVisibleContracts(
			saveNode?.CompanyRunData?.Fame ?? 0,
			saveNode?.TownPhaseState?.IsChampionDay == true,
			saveNode?.CompanyCareerData?.HasCompletedContracts == true || saveNode?.SkipTutorial == true);
	}

	public static Array<ArenaContractData> GetVisibleContracts(int companyFame, bool isChampionDay, bool hasCompletedContracts)
	{
		if (hasCompletedContracts)
			return ArenaContractGenerator.GenerateRandomContracts(companyFame, isChampionDay);

		var contracts = new Array<ArenaContractData>();
		var starterContract = ResourceLoader.Load<ArenaContractData>(StarterSlimePitContractPath);
		if (starterContract != null)
			contracts.Add(starterContract);

		return contracts;
	}

	public static bool TryGetVisibleContract(SaveNode saveNode, int contractIndex, out ArenaContractData contract)
	{
		contract = null;
		var contracts = GetVisibleContracts(saveNode);
		if (contractIndex < 0 || contractIndex >= contracts.Count)
		{
			GD.Print($"ArenaContractSelection: Contract index {contractIndex} is outside available range 0..{contracts.Count - 1}.");
			return false;
		}

		contract = contracts[contractIndex];
		if (contract != null)
			return true;

		GD.Print($"ArenaContractSelection: Contract index {contractIndex} is empty.");
		return false;
	}
}
