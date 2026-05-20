namespace MobArena.Scripts.Resources;

public static class PhaseTransitionController
{
    public static bool CompleteArenaDay(TownPhaseState phaseState, CompanyRunData companyRunData)
    {
        if (phaseState == null || !phaseState.IsDay())
            return false;

        ExecuteBuildingWork(companyRunData);
        phaseState.MoveToNight();
        companyRunData?.NotifyRunChanged();
        return true;
    }

    public static bool CompleteArenaContract(TownPhaseState phaseState, CompanyRunData companyRunData)
    {
        if (phaseState == null || !phaseState.IsDay())
            return false;

        ExecuteBuildingWork(companyRunData);
        companyRunData?.CompleteArenaContractAssignments();
        phaseState.MoveToNight();
        companyRunData?.NotifyRunChanged();
        return true;
    }

    public static bool AdvanceToNextDay(TownPhaseState phaseState, CompanyRunData companyRunData)
    {
        if (phaseState == null || !phaseState.CanAdvanceToNextDay)
            return false;

        if (companyRunData?.CanPayCurrentPhaseGoldCost(phaseState) == false)
            return false;

        ExecuteBuildingWork(companyRunData);
        companyRunData?.PayNightSalary();
        companyRunData?.Market?.ExecuteNewDay();
        phaseState.MoveToNextDay();
        companyRunData?.NotifyRunChanged();
        return true;
    }

    private static void ExecuteBuildingWork(CompanyRunData companyRunData)
    {
        companyRunData?.ExecutePhaseBuildingWork();
    }
}
