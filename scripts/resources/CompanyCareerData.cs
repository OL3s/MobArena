using Godot;

namespace MobArena.Scripts.Resources;

public partial class CompanyCareerData : Resource
{
    [Signal]
    public delegate void CareerChangedEventHandler();

    [Export]
    public int TotalGladiatorsInCareer { get; private set; }

    [Export]
    public int GladiatorsDead { get; private set; }

    [Export]
    public int TotalGoldEarned { get; private set; }

    [Export]
    public int ContractsCompleted { get; private set; }

    [Export]
    public int MobsKilled { get; private set; }

    [Export]
    public int ChampionsDefeated { get; private set; }

    public bool HasCompletedContracts => ContractsCompleted > 0;

    public bool HasReachedSpecialtyBuildings => ContractsCompleted >= 2;

    public void AddGladiator()
    {
        AddGladiators(1);
    }

    public void AddGladiators(int amount)
    {
        if (amount <= 0)
            return;

        TotalGladiatorsInCareer += amount;
        EmitSignal(SignalName.CareerChanged);
    }

    public void AddGladiatorDeath()
    {
        GladiatorsDead++;
        EmitSignal(SignalName.CareerChanged);
    }

    public void AddGoldEarned(int amount)
    {
        if (amount <= 0)
            return;

        TotalGoldEarned += amount;
        EmitSignal(SignalName.CareerChanged);
    }

    public void AddContractCompleted()
    {
        ContractsCompleted++;
        EmitSignal(SignalName.CareerChanged);
    }

    public void AddMobKilled()
    {
        AddMobsKilled(1);
    }

    public void AddMobsKilled(int amount)
    {
        if (amount <= 0)
            return;

        MobsKilled += amount;
        EmitSignal(SignalName.CareerChanged);
    }

    public void AddChampionDefeated()
    {
        ChampionsDefeated++;
        EmitSignal(SignalName.CareerChanged);
    }

    public CompanyCareerData CreateCopy()
    {
        return new CompanyCareerData
        {
            TotalGladiatorsInCareer = TotalGladiatorsInCareer,
            GladiatorsDead = GladiatorsDead,
            TotalGoldEarned = TotalGoldEarned,
            ContractsCompleted = ContractsCompleted,
            MobsKilled = MobsKilled,
            ChampionsDefeated = ChampionsDefeated
        };
    }
}
