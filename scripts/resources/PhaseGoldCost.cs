using System.Collections.Generic;

namespace MobArena.Scripts.Resources;

public enum PhaseGoldCostTiming
{
    DayToNight,
    NightToDay,
    Both
}

public readonly struct PhaseGoldCostLine
{
    public PhaseGoldCostLine(string label, int cost, PhaseGoldCostTiming timing)
    {
        Label = label;
        Cost = cost;
        Timing = timing;
    }

    public string Label { get; }

    public int Cost { get; }

    public PhaseGoldCostTiming Timing { get; }

    public int GetCostForPhase(TownPhaseState phaseState)
    {
        return IsDueForPhase(phaseState) ? Cost : 0;
    }

    public bool IsDueForPhase(TownPhaseState phaseState)
    {
        if (phaseState == null)
            return false;

        return Timing == PhaseGoldCostTiming.Both
            || (Timing == PhaseGoldCostTiming.DayToNight && phaseState.IsDay())
            || (Timing == PhaseGoldCostTiming.NightToDay && phaseState.IsNight());
    }
}

public interface IPhaseGoldCostSource
{
    int PhaseGoldCostDisplayOrder { get; }

    string PhaseGoldCostSection { get; }

    IEnumerable<PhaseGoldCostLine> GetPhaseGoldCostLines(CompanyRunData runData, TownPhaseState phaseState);
}

public static class PhaseGoldCostLineExtensions
{
    public static int SumCostsForPhase(this IEnumerable<PhaseGoldCostLine> lines, TownPhaseState phaseState, bool includeAll = false)
    {
        var total = 0;
        foreach (var line in lines)
        {
            total += includeAll ? line.Cost : line.GetCostForPhase(phaseState);
        }

        return total;
    }
}
