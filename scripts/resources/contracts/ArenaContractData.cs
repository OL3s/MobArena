using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scripts.Resources.Contracts;

[GlobalClass]
public partial class ArenaContractData : Resource
{
    private const float DefaultFameCostRatio = 2.5f;

    [Export]
    public string DisplayName { get; private set; } = "Arena Contract";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [Export]
    public int FameCost { get; private set; }

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float FameCostRatio { get; private set; } = DefaultFameCostRatio;

    [Export]
    public Array<MobData> Mobs { get; private set; } = new();

    [Export]
    public int GoldReward { get; private set; }

    public int GetBaseFameReward()
    {
        var reward = 0;
        foreach (var mob in Mobs)
        {
            if (mob is EnemyMobData enemyMob)
                reward += enemyMob.FameValue;
        }

        return reward;
    }

    public int GetFameCost(int currentCompanyFame)
    {
        return Mathf.Max(FameCost, Mathf.RoundToInt(currentCompanyFame * FameCostRatio));
    }

    public int GetNetFameReward(int currentCompanyFame)
    {
        return GetBaseFameReward() - GetFameCost(currentCompanyFame);
    }
}
