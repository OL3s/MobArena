using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts.Resources.Contracts;

public static class ArenaContractResultResolver
{
    public enum ContractResult
    {
        None,
        Completed,
        ForceRetired
    }

    public static ContractResult ResolveWin(SaveNode saveNode, bool requireActiveContract = true)
    {
        var runData = saveNode?.CompanyRunData;
        var phaseState = saveNode?.TownPhaseState;
        var contract = runData?.ActiveArenaContract;
        if (saveNode == null || runData == null || phaseState == null || (requireActiveContract && contract == null))
            return ContractResult.None;

        KillDefeatedArenaGladiators(runData, saveNode.CompanyCareerData);

        if (contract != null)
        {
            runData.AddGold(contract.GoldReward, saveNode.CompanyCareerData);
            var netFameReward = contract.GetNetFameReward(runData.Fame);
            if (netFameReward >= 0)
                runData.AddFame(netFameReward);
            else
                runData.LoseFame(-netFameReward);

            saveNode.CompanyCareerData?.AddContractCompleted();
            if (contract.IsChampionContract())
                saveNode.CompanyCareerData?.AddChampionDefeated();
        }

        foreach (var gladiator in CopyArenaGladiators(runData))
        {
            gladiator?.GladiatorCareer?.AddContractCompleted();
            gladiator?.GladiatorCareer?.AddWin();
        }

        CompleteArenaDay(saveNode);
        return ContractResult.Completed;
    }

    public static ContractResult ResolveVisibleContractWin(SaveNode saveNode, int contractIndex)
    {
        if (!ArenaContractSelection.TryGetVisibleContract(saveNode, contractIndex, out var contract))
            return ContractResult.None;

        saveNode.CompanyRunData?.SetActiveArenaContract(contract);
        return ResolveWin(saveNode);
    }

    public static ContractResult ResolveLoss(SaveNode saveNode)
    {
        var runData = saveNode?.CompanyRunData;
        var contract = runData?.ActiveArenaContract;
        if (saveNode == null || runData == null)
            return ContractResult.None;

        if (contract?.IsChampionContract() == true)
            return ResolveCompanyLoss(
                saveNode,
                "Company Force-Retired",
                "The company failed its mandatory Champion Day contract and has been force-retired. The run has ended, and any qualifying result was recorded.");

        return ResolveForfeit(saveNode);
    }

    public static ContractResult ResolveCompanyLoss(SaveNode saveNode, string notificationTitle = null, string notificationText = null)
    {
        if (saveNode == null)
            return ContractResult.None;

        saveNode.QueueCompanyLossNotification(
            notificationTitle ?? "Company Retired",
            notificationText ?? "The company has been retired and the run has ended. Any qualifying result was recorded.");
        saveNode.ForceRetireCurrentCompany();
        return ContractResult.ForceRetired;
    }

    public static ContractResult ResolveForfeit(SaveNode saveNode)
    {
        var runData = saveNode?.CompanyRunData;
        if (saveNode == null || runData == null)
            return ContractResult.None;

        KillDefeatedArenaGladiators(runData, saveNode.CompanyCareerData);
        CompleteArenaDay(saveNode);
        return ContractResult.Completed;
    }

    private static void CompleteArenaDay(SaveNode saveNode)
    {
        PhaseTransitionController.CompleteArenaContract(saveNode.TownPhaseState, saveNode.CompanyRunData, saveNode.WeatherState);
        saveNode.CompanyRunData?.ClearActiveArenaContract();
    }

    private static void KillDefeatedArenaGladiators(CompanyRunData runData, CompanyCareerData careerData)
    {
        foreach (var gladiator in CopyArenaGladiators(runData))
        {
            if (gladiator?.Health <= 0)
                runData.KillGladiator(gladiator, careerData, false);
        }
    }

    private static Godot.Collections.Array<GladiatorData> CopyArenaGladiators(CompanyRunData runData)
    {
        var copy = new Godot.Collections.Array<GladiatorData>();
        var arenaGladiators = runData?.TownAssignments?.ArenaGladiators;
        if (arenaGladiators == null)
            return copy;

        foreach (var gladiator in arenaGladiators)
        {
            if (gladiator != null)
                copy.Add(gladiator);
        }

        return copy;
    }
}
