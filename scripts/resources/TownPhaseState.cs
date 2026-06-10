using Godot;

namespace MobArena.Scripts.Resources;

public partial class TownPhaseState : Resource
{
    public enum DayPhase
    {
        Day,
        Night
    }

    [Signal]
    public delegate void PhaseChangedEventHandler();

    [Export]
    public int CurrentDay { get; private set; } = 1;

    [Export]
    public DayPhase CurrentPhase { get; private set; } = DayPhase.Day;

    public bool CanAdvanceToNextDay => CurrentPhase == DayPhase.Night;

    public bool IsChampionDay => CurrentDay % 7 == 0;

    public int DaysUntilChampion
    {
        get
        {
            var remainder = CurrentDay % 7;
            return remainder == 0 ? 0 : 7 - remainder;
        }
    }

    public string GetChampionLabel()
    {
        return IsChampionDay ? "Champion Day is today!" : $"Champion in {DaysUntilChampion} days";
    }

    public string GetDayLabel()
    {
        return $"Day {CurrentDay}";
    }

    public string GetPhaseLabel()
    {
        return CurrentPhase == DayPhase.Day ? "Day" : "Night";
    }

    public double GetPhaseProgressValue()
    {
        return CurrentPhase == DayPhase.Day ? 0.0 : 1.0;
    }

    public double GetPhaseProgressMax()
    {
        return 1.0;
    }

    public bool IsDay()
    {
        return CurrentPhase == DayPhase.Day;
    }

    public bool IsNight()
    {
        return CurrentPhase == DayPhase.Night;
    }

    public void MoveToNight()
    {
        if (CurrentPhase == DayPhase.Night)
            return;

        CurrentPhase = DayPhase.Night;
        GD.Print($"TownPhaseState: Moved to night on day {CurrentDay}.");
        EmitSignal(SignalName.PhaseChanged);
    }

    public void MoveToNextDay()
    {
        CurrentDay++;
        CurrentPhase = DayPhase.Day;
        GD.Print($"TownPhaseState: Moved to day {CurrentDay}.");
        EmitSignal(SignalName.PhaseChanged);
    }
}
