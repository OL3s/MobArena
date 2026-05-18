namespace MobArena.Scripts.Resources;

public static class GameTimeController
{
    private static readonly CompanyTimeProgression CompanyTimeProgression = new();

    public static int TickOneSecond(TownTimeState townTimeState, CompanyRunData companyRunData, CompanyCareerData companyCareerData)
    {
        if (townTimeState == null)
            return 0;

        var minutesAdvanced = townTimeState.TickOneSecond();
        if (minutesAdvanced <= 0)
            return 0;

        CompanyTimeProgression.AdvanceCompanyRunMinutes(companyRunData, companyCareerData, minutesAdvanced);
        return minutesAdvanced;
    }
}
