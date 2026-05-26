using Godot;

namespace MobArena.Scripts.Resources;

public static class PhaseTransitionController
{
    public static bool CompleteArenaDay(TownPhaseState phaseState, CompanyRunData companyRunData, WeatherState weatherState = null)
    {
        if (phaseState == null || !phaseState.IsDay())
        {
            GD.Print("PhaseTransitionController: Complete arena day failed; phase is not day.");
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
            GD.Print("PhaseTransitionController: Complete arena contract failed; phase is not day.");
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
            GD.Print("PhaseTransitionController: Advance to next day failed; phase is not night.");
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
}
