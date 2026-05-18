using Godot;

namespace MobArena.Scripts.Resources;

public partial class GladiatorCareerData : Resource
{
    [Export]
    public int Kills { get; private set; }

    [Export]
    public int Wins { get; private set; }

    [Export]
    public int ContractsCompleted { get; private set; }

    public void AddKill(int amount = 1)
    {
        if (amount <= 0)
            return;

        Kills += amount;
    }

    public void AddWin()
    {
        Wins++;
    }

    public void AddContractCompleted()
    {
        ContractsCompleted++;
    }
}
