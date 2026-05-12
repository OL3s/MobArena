using Godot;

namespace MobArena.Scripts.Resources;

public partial class TownTimeState : Resource
{
    public enum TimeSpeed
    {
        X0 = 0,
        X1 = 1,
        X10 = 10,
        X100 = 100
    }

    private const int MinutesPerDay = 24 * 60;
    private const int StoresOpenMinute = 7 * 60;
    private const int StoresCloseMinute = 22 * 60;

    [Signal]
    public delegate void TimeChangedEventHandler();

    [Signal]
    public delegate void ChampionDeadlineReachedEventHandler();

    [Export]
    public int CurrentDay { get; private set; } = 1;

    [Export]
    public int MinutesIntoDay { get; private set; } = 8 * 60;

    [Export]
    public int ChampionDeadlineDay { get; private set; } = 7;

    [Export]
    public TimeSpeed CurrentSpeed { get; private set; } = TimeSpeed.X0;

    [Export]
    public TimeSpeed LastRunningSpeed { get; private set; } = TimeSpeed.X1;

    public void TickOneSecond()
    {
        var minutesToAdvance = GetMinutesPerTick();
        if (minutesToAdvance <= 0)
            return;

        AdvanceMinutes(minutesToAdvance);
    }

    public void AdvanceMinutes(int minutes)
    {
        if (minutes <= 0)
            return;

        var totalMinutes = MinutesIntoDay + minutes;
        var daysPassed = totalMinutes / MinutesPerDay;

        MinutesIntoDay = totalMinutes % MinutesPerDay;
        CurrentDay += daysPassed;

        EmitSignal(SignalName.TimeChanged);

        if (IsChampionDue())
            EmitSignal(SignalName.ChampionDeadlineReached);
    }

    public void IncreaseSpeed()
    {
        CurrentSpeed = CurrentSpeed switch
        {
            TimeSpeed.X0 => TimeSpeed.X1,
            TimeSpeed.X1 => TimeSpeed.X10,
            TimeSpeed.X10 => TimeSpeed.X100,
            _ => TimeSpeed.X100
        };

        RememberRunningSpeed();

        EmitSignal(SignalName.TimeChanged);
    }

    public void DecreaseSpeed()
    {
        CurrentSpeed = CurrentSpeed switch
        {
            TimeSpeed.X100 => TimeSpeed.X10,
            TimeSpeed.X10 => TimeSpeed.X1,
            TimeSpeed.X1 => TimeSpeed.X0,
            _ => TimeSpeed.X0
        };

        RememberRunningSpeed();

        EmitSignal(SignalName.TimeChanged);
    }

    public void TogglePaused()
    {
        if (CurrentSpeed == TimeSpeed.X0)
        {
            CurrentSpeed = LastRunningSpeed == TimeSpeed.X0 ? TimeSpeed.X1 : LastRunningSpeed;
        }
        else
        {
            LastRunningSpeed = CurrentSpeed;
            CurrentSpeed = TimeSpeed.X0;
        }

        EmitSignal(SignalName.TimeChanged);
    }

    public void ResetToPause()
    {
        if (CurrentSpeed != TimeSpeed.X0)
            LastRunningSpeed = CurrentSpeed;

        CurrentSpeed = TimeSpeed.X0;
        EmitSignal(SignalName.TimeChanged);
    }

    public string GetSpeedLabel()
    {
        return CurrentSpeed switch
        {
            TimeSpeed.X100 => "Fast",
            TimeSpeed.X10 => "Normal",
            TimeSpeed.X0 => "Paused",
            _ => "Slowed"
        };
    }

    public string GetDayLabel()
    {
        return $"Day {CurrentDay}";
    }

    public string GetDigitalTimeLabel()
    {
        var hours = MinutesIntoDay / 60;
        var minutes = MinutesIntoDay % 60;
        return $"{hours:00}:{minutes:00}";
    }

    public double GetDayProgressValue()
    {
        return MinutesIntoDay;
    }

    public double GetDayProgressMax()
    {
        return MinutesPerDay;
    }

    public int GetTownOpenMinute()
    {
        return StoresOpenMinute;
    }

    public int GetTownCloseMinute()
    {
        return StoresCloseMinute;
    }

    public string GetDayPhaseLabel()
    {
        return IsTownOpen() ? "Open" : "Sleeping";
    }

    public bool IsTownOpen()
    {
        return MinutesIntoDay >= StoresOpenMinute && MinutesIntoDay < StoresCloseMinute;
    }

    public bool IsTownSleeping()
    {
        return !IsTownOpen();
    }

    public bool AreStoresOpen()
    {
        return IsTownOpen();
    }

    public double GetChampionProgressValue()
    {
        return Mathf.Clamp(CurrentDay, 1, ChampionDeadlineDay);
    }

    public double GetChampionProgressMax()
    {
        return ChampionDeadlineDay;
    }

    public double GetChampionFinalDayStart()
    {
        return Mathf.Max(1, ChampionDeadlineDay - 1);
    }

    public string GetChampionDeadlineLabel()
    {
        return $"Day {ChampionDeadlineDay}";
    }

    public bool IsChampionDue()
    {
        return CurrentDay >= ChampionDeadlineDay;
    }

    private int GetMinutesPerTick()
    {
        return CurrentSpeed switch
        {
            TimeSpeed.X100 => 100,
            TimeSpeed.X10 => 10,
            TimeSpeed.X0 => 0,
            _ => 1
        };
    }

    private void RememberRunningSpeed()
    {
        if (CurrentSpeed != TimeSpeed.X0)
            LastRunningSpeed = CurrentSpeed;
    }
}
