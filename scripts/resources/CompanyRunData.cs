using Godot;
using Godot.Collections;

namespace MobArena.Scripts.Resources;

public partial class CompanyRunData : Resource
{
    [Signal]
    public delegate void RunChangedEventHandler();

    [Export]
    public int Gold { get; private set; } = 100;

    [Export]
    public Array<GladiatorData> Gladiators { get; private set; } = new() { GladiatorData.CreateDefault() };

    public int AliveGladiators => Gladiators.Count;

    [Export]
    public int MobsKilled { get; private set; }

    public void AddGold(int amount, CompanyCareerData careerData)
    {
        if (amount <= 0)
            return;

        Gold += amount;
        careerData?.AddGoldEarned(amount);
        EmitSignal(SignalName.RunChanged);
    }

    public bool TrySpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (Gold < amount)
            return false;

        Gold -= amount;
        EmitSignal(SignalName.RunChanged);
        return true;
    }

    public void AddMobKilled(CompanyCareerData careerData, int amount = 1)
    {
        if (amount <= 0)
            return;

        MobsKilled += amount;
        careerData?.AddMobsKilled(amount);
        EmitSignal(SignalName.RunChanged);
    }
}
