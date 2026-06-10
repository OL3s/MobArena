using Godot;

namespace MobArena.Scripts.Resources;

public static class PhaseTransitionController
{
    public static bool CompleteArenaDay(TownPhaseState phaseState, CompanyRunData companyRunData, WeatherState weatherState = null)
    {
        if (phaseState == null || !phaseState.IsDay())
        {
            GD.Print($"PhaseTransitionController: Complete arena day failed; {DescribeContext(phaseState, companyRunData)}.");
            return false;
        }

        ExecuteBuildingWork(companyRunData);
        companyRunData?.CompleteArenaContractAssignments();
        phaseState.MoveToNight();
        weatherState?.ChooseRandomWeather(phaseState);
        companyRunData?.NotifyRunChanged();
        GD.Print($"PhaseTransitionController: Completed arena day. Day={phaseState.CurrentDay}, phase={phaseState.CurrentPhase}.");
        return true;
    }

    public static bool CompleteArenaContract(TownPhaseState phaseState, CompanyRunData companyRunData, WeatherState weatherState = null)
    {
        if (phaseState == null || !phaseState.IsDay())
        {
            GD.Print($"PhaseTransitionController: Complete arena contract failed; {DescribeContext(phaseState, companyRunData)}.");
            return false;
        }

        ExecuteBuildingWork(companyRunData);
        companyRunData?.CompleteArenaContractAssignments();
        phaseState.MoveToNight();
        weatherState?.ChooseRandomWeather(phaseState);
        companyRunData?.NotifyRunChanged();
        GD.Print($"PhaseTransitionController: Completed arena contract. Day={phaseState.CurrentDay}, phase={phaseState.CurrentPhase}.");
        return true;
    }

    public static bool AdvanceToNextDay(TownPhaseState phaseState, CompanyRunData companyRunData, WeatherState weatherState = null)
    {
        if (phaseState == null || !phaseState.CanAdvanceToNextDay)
        {
            GD.Print($"PhaseTransitionController: Advance to next day failed; {DescribeContext(phaseState, companyRunData)}.");
            return false;
        }

		ExecuteBuildingWork(companyRunData);
		companyRunData?.PayNightSalary();
        companyRunData?.Market?.ExecuteNewDay();
        phaseState.MoveToNextDay();
        weatherState?.ChooseRandomWeather(phaseState);
        companyRunData?.NotifyRunChanged();
        GD.Print($"PhaseTransitionController: Advanced to next day. Day={phaseState.CurrentDay}, phase={phaseState.CurrentPhase}.");
        return true;
    }

    private static void ExecuteBuildingWork(CompanyRunData companyRunData)
    {
        companyRunData?.ExecutePhaseBuildingWork();
    }

    private static string DescribeContext(TownPhaseState phaseState, CompanyRunData companyRunData)
    {
        var day = phaseState?.CurrentDay.ToString() ?? "unknown";
        var phase = phaseState?.CurrentPhase.ToString() ?? "unknown";
        var contract = companyRunData?.ActiveArenaContract?.DisplayName ?? "none";
        var arenaGladiators = companyRunData?.TownAssignments?.ArenaGladiators?.Count ?? 0;
        return $"day={day}, phase={phase}, contract='{contract}', arenaGladiators={arenaGladiators}";
    }
}
