using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts.Resources.Contracts;

public static class ArenaContractResultResolver
{
    private const float SkipContractFameMultiplier = 0.9f;

    public enum ContractResult
    {
        None,
        Completed,
        DemoComplete,
        ForceRetired
    }

    public static ContractResult ResolveWin(SaveNode saveNode, bool requireActiveContract = true)
    {
        var runData = saveNode?.CompanyRunData;
        var phaseState = saveNode?.TownPhaseState;
        var contract = runData?.ActiveArenaContract;
        if (saveNode?.IsDemoComplete == true)
        {
            GameLogger.Contract($"Arena result: win ignored because demo is complete; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        if (saveNode == null || runData == null || phaseState == null || (requireActiveContract && contract == null))
        {
            GameLogger.Contract($"Arena result: win ignored; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        GameLogger.Contract($"Arena result: resolving win; {DescribeContext(saveNode)}.");

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
        GameLogger.Contract($"Arena result: win completed; {DescribeContext(saveNode)}.");
        return saveNode.IsDemoComplete ? ContractResult.DemoComplete : ContractResult.Completed;
    }

    public static ContractResult ResolveVisibleContractWin(SaveNode saveNode, int contractIndex)
    {
        if (saveNode == null)
        {
            GameLogger.Contract($"Arena result: visible contract win ignored because arena start is blocked: save node is missing; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        if (!saveNode.CanStartArenaContract(out var blockReason))
        {
            GameLogger.Contract($"Arena result: visible contract win ignored because arena start is blocked: {blockReason}; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        if (!ArenaContractSelection.TryGetVisibleContract(saveNode, contractIndex, out var contract))
            return ContractResult.None;

        saveNode.CompanyRunData?.SetActiveArenaContract(contract);
        return ResolveWin(saveNode);
    }

    public static bool CanSkipDailyContract(SaveNode saveNode, out string blockReason)
    {
        var phaseState = saveNode?.TownPhaseState;
        if (saveNode?.CompanyRunData == null || phaseState == null)
        {
            blockReason = "save state is missing run or phase data";
            return false;
        }

        if (phaseState.IsChampionDay)
        {
            blockReason = "Champion Day contracts cannot be skipped";
            return false;
        }

        if (saveNode.CompanyCareerData?.HasCompletedContracts != true && saveNode.SkipTutorial != true)
        {
            blockReason = "complete the first contract before skipping daily contracts";
            return false;
        }

        if (!phaseState.IsDay())
        {
            blockReason = $"daily contracts can only be skipped during Day; current phase is {phaseState.GetPhaseLabel()}";
            return false;
        }

        blockReason = string.Empty;
        return true;
    }

    public static int GetSkipContractFameLoss(SaveNode saveNode)
    {
        var currentFame = Mathf.Max(0, saveNode?.CompanyRunData?.Fame ?? 0);
        var nextFame = Mathf.FloorToInt(currentFame * SkipContractFameMultiplier);
        return Mathf.Max(0, currentFame - nextFame);
    }

    public static ContractResult SkipDailyContract(SaveNode saveNode)
    {
        if (!CanSkipDailyContract(saveNode, out var blockReason))
        {
            GameLogger.Contract($"Arena result: skip contract ignored: {blockReason}; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        var runData = saveNode.CompanyRunData;
        var previousFame = runData.Fame;
        var fameLoss = GetSkipContractFameLoss(saveNode);
        if (!PhaseTransitionController.SkipArenaContract(saveNode.TownPhaseState, runData, saveNode.WeatherState))
        {
            GameLogger.Contract($"Arena result: skip contract ignored because phase transition failed; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        if (fameLoss > 0)
            runData.LoseFame(fameLoss);

        GameLogger.Contract($"Arena result: skipped daily contract; fame {previousFame} -> {runData.Fame}; {DescribeContext(saveNode)}.");
        return ContractResult.Completed;
    }

    public static ContractResult ResolveLoss(SaveNode saveNode)
    {
        var runData = saveNode?.CompanyRunData;
        var contract = runData?.ActiveArenaContract;
        if (saveNode == null || runData == null)
        {
            GameLogger.Contract($"Arena result: loss ignored; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        GameLogger.Contract($"Arena result: resolving loss; {DescribeContext(saveNode)}.");

        if (contract?.IsChampionContract() == true)
            return ResolveCompanyLoss(
                saveNode,
                "Company Force-Retired",
                "The company failed its mandatory Champion Day contract and has been force-retired. The run has ended, and any qualifying result was recorded.");

        return ResolveForfeit(saveNode, "loss non-champion");
    }

    public static ContractResult ResolveAllPlayersDefeated(SaveNode saveNode)
    {
        GameLogger.Contract($"Arena result: all players defeated requested; {DescribeContext(saveNode)}.");
        return ResolveLoss(saveNode);
    }

    public static ContractResult ResolveCompanyLoss(SaveNode saveNode, string notificationTitle = null, string notificationText = null)
    {
        if (saveNode == null)
        {
            GameLogger.Contract("Arena result: company loss ignored; save node is null.");
            return ContractResult.None;
        }

        GameLogger.Contract($"Arena result: resolving company loss; {DescribeContext(saveNode)}.");

        saveNode.QueueCompanyLossNotification(
            notificationTitle ?? "Company Retired",
            notificationText ?? "The company has been retired and the run has ended. Any qualifying result was recorded.");
        saveNode.ForceRetireCurrentCompany();
        GameLogger.Contract($"Arena result: company force-retired; {DescribeContext(saveNode)}.");
        return ContractResult.ForceRetired;
    }

    public static ContractResult ResolveForfeit(SaveNode saveNode, string source = "forfeit")
    {
        var runData = saveNode?.CompanyRunData;
        if (saveNode == null || runData == null)
        {
            GameLogger.Contract($"Arena result: {source} ignored; {DescribeContext(saveNode)}.");
            return ContractResult.None;
        }

        GameLogger.Contract($"Arena result: resolving {source}; {DescribeContext(saveNode)}.");

        KillDefeatedArenaGladiators(runData, saveNode.CompanyCareerData);
        CompleteArenaDay(saveNode);
        GameLogger.Contract($"Arena result: {source} completed; {DescribeContext(saveNode)}.");
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

    private static string DescribeContext(SaveNode saveNode)
    {
        var runData = saveNode?.CompanyRunData;
        var phaseState = saveNode?.TownPhaseState;
        var contract = runData?.ActiveArenaContract;
        var arenaGladiators = runData?.TownAssignments?.ArenaGladiators;
        var totalGladiators = arenaGladiators?.Count ?? 0;
        var defeatedGladiators = 0;

        if (arenaGladiators != null)
        {
            foreach (var gladiator in arenaGladiators)
            {
                if (gladiator?.Health <= 0)
                    defeatedGladiators++;
            }
        }

        var contractName = contract?.DisplayName ?? "none";
        var contractKind = contract?.IsChampionContract() == true ? "champion" : "standard";
        var day = phaseState?.CurrentDay.ToString() ?? "unknown";
        var phase = phaseState?.CurrentPhase.ToString() ?? "unknown";
        return $"contract='{contractName}', kind={contractKind}, day={day}, phase={phase}, arenaGladiators={defeatedGladiators}/{totalGladiators} defeated";
    }
}
