using Godot;
using System.Collections.Generic;

namespace MobArena.Scripts.Resources;

public partial class CompanyTimeProgression : Resource
{
    private const float MinutesPerDay = 24f * 60f;

    [Export]
    public float ProvisionsDecayPerDay { get; private set; } = 1f;

    [Export]
    public float ExhaustionRecoveryPerDay { get; private set; } = 2f;

    public void AdvanceCompanyRunMinutes(CompanyRunData companyRunData, CompanyCareerData companyCareerData, int minutes)
    {
        if (companyRunData == null || minutes <= 0)
            return;

        var days = minutes / MinutesPerDay;
        companyRunData.Rations?.AddConsumptionProgress(companyRunData.AliveGladiators * days);

        foreach (var gladiator in companyRunData.Gladiators)
        {
            if (gladiator == null)
                continue;

            gladiator.SetProvisions(gladiator.Provisions - ProvisionsDecayPerDay * days);
            gladiator.SetExhaustion(gladiator.Exhaustion + ExhaustionRecoveryPerDay * days);
        }

        companyRunData.AutoFeedGladiatorsBelowThreshold();

        var deadGladiators = new List<GladiatorData>();
        foreach (var gladiator in companyRunData.Gladiators)
        {
            if (gladiator == null)
                continue;

            if (gladiator.Provisions <= 0f)
                deadGladiators.Add(gladiator);
        }

        foreach (var gladiator in deadGladiators)
        {
            companyRunData.KillGladiator(gladiator, companyCareerData);
        }

        companyRunData.NotifyRunChanged();
    }
}
