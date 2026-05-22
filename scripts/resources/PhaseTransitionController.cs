namespace MobArena.Scripts.Resources;

public static class PhaseTransitionController
{
    public static bool CompleteArenaDay(TownPhaseState phaseState, CompanyRunData companyRunData, WeatherState weatherState = null)
    {
        if (phaseState == null || !phaseState.IsDay())
            return false;

        ExecuteBuildingWork(companyRunData);
        companyRunData?.CompleteArenaContractAssignments();
        phaseState.MoveToNight();
        weatherState?.ChooseRandomWeather(phaseState);
        companyRunData?.NotifyRunChanged();
        return true;
    }

    public static bool CompleteArenaContract(TownPhaseState phaseState, CompanyRunData companyRunData, WeatherState weatherState = null)
    {
        if (phaseState == null || !phaseState.IsDay())
            return false;

        ExecuteBuildingWork(companyRunData);
        companyRunData?.CompleteArenaContractAssignments();
        phaseState.MoveToNight();
        weatherState?.ChooseRandomWeather(phaseState);
        companyRunData?.NotifyRunChanged();
        return true;
    }

    public static bool AdvanceToNextDay(TownPhaseState phaseState, CompanyRunData companyRunData, WeatherState weatherState = null)
    {
        if (phaseState == null || !phaseState.CanAdvanceToNextDay)
            return false;

		ExecuteBuildingWork(companyRunData);
		companyRunData?.PayNightSalary();
        companyRunData?.Market?.ExecuteNewDay();
        phaseState.MoveToNextDay();
        weatherState?.ChooseRandomWeather(phaseState);
        companyRunData?.NotifyRunChanged();
        return true;
    }

    private static void ExecuteBuildingWork(CompanyRunData companyRunData)
    {
        companyRunData?.ExecutePhaseBuildingWork();
    }
}
