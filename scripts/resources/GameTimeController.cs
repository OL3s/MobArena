namespace MobArena.Scripts.Resources;

public static class GameTimeController
{
    private static readonly CompanyTimeProgression CompanyTimeProgression = new();

    public static int TickOneSecond(TownTimeState townTimeState, CompanyRunData companyRunData, CompanyCareerData companyCareerData)
    {
        if (townTimeState == null)
            return 0;

        var currentDay = townTimeState.CurrentDay;
        var minutesAdvanced = townTimeState.TickOneSecond();
        if (minutesAdvanced <= 0)
            return 0;

        CompanyTimeProgression.AdvanceCompanyRunMinutes(companyRunData, companyCareerData, minutesAdvanced);

        if (townTimeState.CurrentDay > currentDay)
            ExecuteNewDay(townTimeState, companyRunData);

        return minutesAdvanced;
    }

    public static void ExecuteNewDay(TownTimeState townTimeState, CompanyRunData companyRunData)
    {
        if (townTimeState == null)
            return;

        var isChampionDue = townTimeState.IsChampionDue();
        var starvingGladiatorCount = companyRunData?.GetStarvingGladiatorCount() ?? 0;

        townTimeState.ApplyNewDayWarnings(isChampionDue, starvingGladiatorCount);
    }
}
