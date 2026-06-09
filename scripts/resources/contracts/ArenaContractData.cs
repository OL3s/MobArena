using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scripts.Resources.Contracts;

[GlobalClass]
public partial class ArenaContractData : Resource
{
    private const float DefaultFameRewardRatio = 0.1f;
    private const float DefaultFameDecayRatio = 0.01f;
    private const int MinimumThreatStarFame = 30;
    private const float ThreatStarGrowthBase = 1.7f;
    private const int MaxThreatStars = 10;

    [Export]
    public string DisplayName { get; private set; } = "Arena Contract";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [Export]
    public EnemyMobFamilyData FamilyData { get; private set; }

    [Export]
    public ArenaContractDifficulty Difficulty { get; private set; } = ArenaContractDifficulty.Easy;

    [Export]
    public int FameCost { get; private set; }

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float FameRewardRatio { get; private set; } = DefaultFameRewardRatio;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float FameCostRatio { get; private set; } = DefaultFameDecayRatio;

    [Export]
    public Array<MobData> Mobs { get; private set; } = new();

    [Export]
    public int GoldReward { get; private set; }

    public void ConfigureGenerated(
        string displayName,
        string description,
        EnemyMobFamilyData mobFamily,
        ArenaContractDifficulty difficulty,
        Array<MobData> mobs,
        int goldReward,
        int minimumFameCost = 0,
        float fameRewardRatio = DefaultFameRewardRatio,
        float fameDecayRatio = DefaultFameDecayRatio)
    {
        DisplayName = displayName;
        Description = description;
        FamilyData = mobFamily;
        Difficulty = difficulty;
        Mobs = mobs ?? new Array<MobData>();
        GoldReward = goldReward;
        FameCost = minimumFameCost;
        FameRewardRatio = fameRewardRatio;
        FameCostRatio = fameDecayRatio;
    }

    public MobFamily GetFamilyKey()
    {
        return FamilyData?.Family ?? MobFamily.Slimes;
    }

    public int GetThreatFameValue()
    {
        var value = 0;
        foreach (var mob in Mobs)
        {
            if (mob is EnemyMobData enemyMob)
                value += enemyMob.FameValue;
        }

        return value;
    }

    public Array<EnemyMobData> GetEnemyMobs()
    {
        var enemyMobs = new Array<EnemyMobData>();
        foreach (var mob in Mobs)
        {
            if (mob is EnemyMobData enemyMob)
                enemyMobs.Add(enemyMob);
        }

        return enemyMobs;
    }

    public int GetGrossFameReward()
    {
        return Mathf.RoundToInt(GetThreatFameValue() * FameRewardRatio);
    }

    public int GetThreatStarCount()
    {
        var fame = Mathf.Max(MinimumThreatStarFame, GetThreatFameValue());
        var scaled = Mathf.Log(fame / (float)MinimumThreatStarFame) / Mathf.Log(ThreatStarGrowthBase);
        return Mathf.Clamp(1 + Mathf.FloorToInt(scaled), 1, MaxThreatStars);
    }

    public static int GetMobFameBudget(int currentCompanyFame, ArenaContractDifficulty difficulty)
    {
        return difficulty switch
        {
            ArenaContractDifficulty.Easy => Mathf.RoundToInt(30f + 3f * Mathf.Sqrt(currentCompanyFame)),
            ArenaContractDifficulty.Medium => Mathf.RoundToInt(45f + 0.45f * currentCompanyFame),
            ArenaContractDifficulty.Hard => Mathf.RoundToInt(70f + 0.75f * currentCompanyFame + 0.02f * Mathf.Pow(currentCompanyFame, 1.35f)),
            ArenaContractDifficulty.Champion => Mathf.RoundToInt(90f + 0.6f * currentCompanyFame + 0.015f * Mathf.Pow(currentCompanyFame, 1.25f)),
            _ => Mathf.RoundToInt(80f + 0.5f * currentCompanyFame)
        };
    }

    public bool IsChampionContract()
    {
        foreach (var mob in Mobs)
        {
            if (mob is ChampionMobData)
                return true;
        }

        return false;
    }

    public int GetFameCost(int currentCompanyFame)
    {
        return Mathf.Max(FameCost, Mathf.RoundToInt(currentCompanyFame * FameCostRatio));
    }

    public int GetNetFameReward(int currentCompanyFame)
    {
        return GetGrossFameReward() - GetFameCost(currentCompanyFame);
    }
}
