using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scripts.Resources.Contracts;

public static class ArenaContractGenerator
{
    private const float FameRewardRatio = 0.1f;
    private const float FameDecayRatio = 0.01f;
    private const float GoldRewardThreatRatio = 1.0f;
    private const int MaxMobTypesPerContract = 4;
    private static readonly RandomNumberGenerator Random = new();

    public static Array<ArenaContractData> GenerateContracts(int currentCompanyFame, bool isChampionDay, MobFamily family = MobFamily.Slimes)
    {
        var preferredFamily = MobFamilyCatalog.FindFamily(family);
        return GenerateContracts(currentCompanyFame, isChampionDay, preferredFamily);
    }

    public static Array<ArenaContractData> GenerateContracts(int currentCompanyFame, bool isChampionDay, EnemyMobFamilyData family)
    {
        var families = MobFamilyCatalog.LoadEnemyFamiliesList();
        return isChampionDay
            ? GenerateChampionContracts(currentCompanyFame, family, families)
            : GenerateStandardContracts(currentCompanyFame, family, families);
    }

    public static Array<ArenaContractData> GenerateRandomContracts(int currentCompanyFame, bool isChampionDay)
    {
        var families = MobFamilyCatalog.LoadEnemyFamiliesList();
        return isChampionDay
            ? GenerateRandomChampionContracts(currentCompanyFame, families)
            : GenerateRandomStandardContracts(currentCompanyFame, families);
    }

    public static Array<EnemyMobFamilyData> GetEligibleFamilies(int currentCompanyFame, bool isChampionDay)
    {
        var result = new Array<EnemyMobFamilyData>();
        foreach (var family in MobFamilyCatalog.LoadEnemyFamiliesList())
        {
            if (IsFamilyEligible(family, currentCompanyFame, isChampionDay))
                result.Add(family);
        }

        return result;
    }

    private static Array<ArenaContractData> GenerateRandomStandardContracts(int currentCompanyFame, List<EnemyMobFamilyData> families)
    {
        var contracts = new Array<ArenaContractData>();
        foreach (var difficulty in new[] { ArenaContractDifficulty.Easy, ArenaContractDifficulty.Medium, ArenaContractDifficulty.Hard })
        {
            var budget = ArenaContractData.GetMobFameBudget(currentCompanyFame, difficulty);
            var family = GetRandomEligibleFamily(families, currentCompanyFame, budget, false);
            var contract = family == null ? null : CreateStandardContract(difficulty, currentCompanyFame, family);
            if (contract != null)
                contracts.Add(contract);
        }

        return contracts;
    }

    private static Array<ArenaContractData> GenerateRandomChampionContracts(int currentCompanyFame, List<EnemyMobFamilyData> families)
    {
        var contracts = new Array<ArenaContractData>();
        foreach (var difficulty in new[] { ArenaContractDifficulty.Easy, ArenaContractDifficulty.Medium, ArenaContractDifficulty.Hard })
        {
            var budget = GetChampionMobFameBudget(currentCompanyFame, difficulty);
            var family = GetRandomEligibleFamily(families, currentCompanyFame, budget, true);
            var contract = family == null ? null : CreateChampionContract(difficulty, currentCompanyFame, family);
            if (contract != null)
                contracts.Add(contract);
        }

        return contracts;
    }

    private static Array<ArenaContractData> GenerateStandardContracts(int currentCompanyFame, EnemyMobFamilyData preferredFamily, List<EnemyMobFamilyData> families)
    {
        var budget = ArenaContractData.GetMobFameBudget(currentCompanyFame, ArenaContractDifficulty.Easy);
        var family = IsFamilyEligibleForBudget(preferredFamily, currentCompanyFame, budget, false)
            ? preferredFamily
            : GetFirstEligibleFamily(families, currentCompanyFame, budget, false);
        if (family == null)
            return new Array<ArenaContractData>();

        var contracts = new Array<ArenaContractData>();
        foreach (var difficulty in new[] { ArenaContractDifficulty.Easy, ArenaContractDifficulty.Medium, ArenaContractDifficulty.Hard })
        {
            var contract = CreateStandardContract(difficulty, currentCompanyFame, family);
            if (contract != null)
                contracts.Add(contract);
        }

        return contracts;
    }

    private static Array<ArenaContractData> GenerateChampionContracts(int currentCompanyFame, EnemyMobFamilyData preferredFamily, List<EnemyMobFamilyData> families)
    {
        var budget = GetChampionMobFameBudget(currentCompanyFame, ArenaContractDifficulty.Easy);
        var family = IsFamilyEligibleForBudget(preferredFamily, currentCompanyFame, budget, true)
            ? preferredFamily
            : GetFirstEligibleFamily(families, currentCompanyFame, budget, true);
        if (family == null)
            return new Array<ArenaContractData>();

        var contracts = new Array<ArenaContractData>();
        foreach (var difficulty in new[] { ArenaContractDifficulty.Easy, ArenaContractDifficulty.Medium, ArenaContractDifficulty.Hard })
        {
            var contract = CreateChampionContract(difficulty, currentCompanyFame, family);
            if (contract != null)
                contracts.Add(contract);
        }

        return contracts;
    }

    private static ArenaContractData CreateChampionContract(ArenaContractDifficulty difficulty, int currentCompanyFame, EnemyMobFamilyData family)
    {
        var totalBudget = GetChampionMobFameBudget(currentCompanyFame, difficulty);
        var familyMobs = family.GetEligibleMobs(currentCompanyFame);
        var champion = SelectChampionForBudget(GetEligibleChampions(family, currentCompanyFame), familyMobs, totalBudget);
        if (champion == null)
            return null;

        var supportBudget = Mathf.Max(0, totalBudget - champion.FameValue);
        var supportMobs = GetNonChampionMobs(family, currentCompanyFame)
            .Where(mob => mob.FameValue <= champion.FameValue)
            .ToList();
        var contractMobs = new Array<MobData> { champion };
        foreach (var mob in FillMobBudget(
            supportMobs,
            supportBudget,
            GetMaxTotalMobCount(difficulty) - 1,
            MaxMobTypesPerContract - 1))
        {
            contractMobs.Add(mob);
        }

        var contract = new ArenaContractData();
        contract.ConfigureGenerated(
            $"Champion {GetFamilyLabel(family)} Contract",
            $"A generated {difficulty.ToString().ToLowerInvariant()} Champion Day contract with one champion and same-family support mobs.",
            family,
            difficulty,
            contractMobs,
            GetGoldReward(contractMobs),
            fameRewardRatio: FameRewardRatio,
            fameDecayRatio: FameDecayRatio);
        return contract;
    }

    private static ArenaContractData CreateStandardContract(ArenaContractDifficulty difficulty, int currentCompanyFame, EnemyMobFamilyData family)
    {
        var budget = ArenaContractData.GetMobFameBudget(currentCompanyFame, difficulty);
        var familyMobs = GetNonChampionMobs(family, currentCompanyFame);
        if (familyMobs.Count <= 0)
            return null;

        var contractMobs = FillMobBudget(familyMobs, budget, GetMaxTotalMobCount(difficulty), MaxMobTypesPerContract);
        var contract = new ArenaContractData();
        contract.ConfigureGenerated(
            $"{GetFamilyLabel(family)} Contract",
            $"A generated {difficulty.ToString().ToLowerInvariant()} contract built from {GetFamilyLabel(family).ToLowerInvariant()} mobs and current company fame.",
            family,
            difficulty,
            contractMobs,
            GetGoldReward(contractMobs),
            fameRewardRatio: FameRewardRatio,
            fameDecayRatio: FameDecayRatio);
        return contract;
    }

    private static bool IsFamilyEligible(EnemyMobFamilyData family, int currentCompanyFame, bool isChampionDay)
    {
        var budget = isChampionDay
            ? GetChampionMobFameBudget(currentCompanyFame, ArenaContractDifficulty.Easy)
            : ArenaContractData.GetMobFameBudget(currentCompanyFame, ArenaContractDifficulty.Easy);
        return IsFamilyEligibleForBudget(family, currentCompanyFame, budget, isChampionDay);
    }

    private static bool IsFamilyEligibleForBudget(EnemyMobFamilyData family, int currentCompanyFame, int budget, bool isChampionDay)
    {
        if (family == null || family.FameValue > currentCompanyFame)
            return false;

        if (!isChampionDay)
            return HasMobThatFits(GetNonChampionMobs(family, currentCompanyFame), budget);

        return SelectChampionForBudget(GetEligibleChampions(family, currentCompanyFame), family.GetEligibleMobs(currentCompanyFame), budget) != null;
    }

    private static EnemyMobFamilyData GetFirstEligibleFamily(List<EnemyMobFamilyData> families, int currentCompanyFame, int budget, bool isChampionDay)
    {
        return families.FirstOrDefault(family => IsFamilyEligibleForBudget(family, currentCompanyFame, budget, isChampionDay));
    }

    private static EnemyMobFamilyData GetRandomEligibleFamily(List<EnemyMobFamilyData> families, int currentCompanyFame, int budget, bool isChampionDay)
    {
        var eligibleFamilies = families
            .Where(family => IsFamilyEligibleForBudget(family, currentCompanyFame, budget, isChampionDay))
            .ToList();
        return eligibleFamilies.Count <= 0
            ? null
            : eligibleFamilies[Random.RandiRange(0, eligibleFamilies.Count - 1)];
    }

    private static List<ChampionMobData> GetEligibleChampions(EnemyMobFamilyData family, int currentCompanyFame)
    {
        return family.GetEligibleMobs(currentCompanyFame)
            .OfType<ChampionMobData>()
            .OrderByDescending(mob => mob.FameValue)
            .ToList();
    }

    private static ChampionMobData SelectChampionForBudget(List<ChampionMobData> champions, List<EnemyMobData> familyMobs, int totalBudget)
    {
        return champions
            .Select(champion => new
            {
                Champion = champion,
                CheapestSupportFame = GetCheapestSupportFame(familyMobs, champion.FameValue)
            })
            .Where(option => option.CheapestSupportFame > 0 && option.Champion.FameValue + option.CheapestSupportFame <= totalBudget)
            .OrderByDescending(option => option.Champion.FameValue)
            .Select(option => option.Champion)
            .FirstOrDefault();
    }

    private static int GetCheapestSupportFame(List<EnemyMobData> familyMobs, int maxSupportFame)
    {
        return familyMobs
            .Where(mob => mob is not ChampionMobData
                && mob.FameValue > 0
                && mob.FameValue <= maxSupportFame)
            .OrderBy(mob => mob.FameValue)
            .Select(mob => mob.FameValue)
            .FirstOrDefault();
    }

    private static List<EnemyMobData> GetNonChampionMobs(EnemyMobFamilyData family, int currentCompanyFame)
    {
        return family.GetEligibleMobs(currentCompanyFame, false)
            .Where(mob => mob.FameValue > 0)
            .ToList();
    }

    private static bool HasMobThatFits(List<EnemyMobData> mobs, int budget)
    {
        return mobs.Any(mob => mob.FameValue > 0 && mob.FameValue <= budget);
    }

    private static Array<MobData> FillMobBudget(List<EnemyMobData> mobs, int budget, int maxTotalMobCount, int maxMobTypes)
    {
        var validMobs = SelectMobTypesForBudget(mobs, budget, maxMobTypes);
        var result = new Array<MobData>();
        if (validMobs.Count <= 0)
            return result;

        var total = 0;
        while (total < budget && result.Count < maxTotalMobCount)
        {
            var remaining = budget - total;
            var fittingMobs = validMobs.Where(mob => mob.FameValue <= remaining).ToList();
            var chosen = fittingMobs.Count > 0 ? fittingMobs[Random.RandiRange(0, fittingMobs.Count - 1)] : validMobs[^1];
            result.Add(chosen);
            total += chosen.FameValue;
        }

        return result;
    }

    private static List<EnemyMobData> SelectMobTypesForBudget(List<EnemyMobData> mobs, int budget, int maxMobTypes)
    {
        maxMobTypes = Mathf.Max(1, maxMobTypes);
        var positiveMobs = mobs
            .Where(mob => mob.FameValue > 0)
            .OrderBy(mob => mob.FameValue)
            .ToList();
        if (positiveMobs.Count <= maxMobTypes)
            return positiveMobs.OrderByDescending(mob => mob.FameValue).ToList();

        var selected = new List<EnemyMobData>();
        var candidates = positiveMobs
            .Where(mob => mob.FameValue <= budget)
            .ToList();
        if (candidates.Count <= 0)
            candidates.Add(positiveMobs[0]);

        while (selected.Count < maxMobTypes && candidates.Count > 0)
        {
            var index = Random.RandiRange(0, candidates.Count - 1);
            selected.Add(candidates[index]);
            candidates.RemoveAt(index);
        }

        return selected
            .Distinct()
            .OrderByDescending(mob => mob.FameValue)
            .ToList();
    }

    private static int GetMaxTotalMobCount(ArenaContractDifficulty difficulty)
    {
        return difficulty switch
        {
            ArenaContractDifficulty.Easy => 8,
            ArenaContractDifficulty.Medium => 10,
            _ => 12
        };
    }

    private static int GetChampionMobFameBudget(int currentCompanyFame, ArenaContractDifficulty difficulty)
    {
        var baseBudget = ArenaContractData.GetMobFameBudget(currentCompanyFame, difficulty);
        var multiplier = difficulty switch
        {
            ArenaContractDifficulty.Easy => 1.6f,
            ArenaContractDifficulty.Medium => 1.8f,
            ArenaContractDifficulty.Hard => 2.0f,
            _ => 1.6f
        };
        var flatBonus = difficulty switch
        {
            ArenaContractDifficulty.Easy => 35,
            ArenaContractDifficulty.Medium => 55,
            ArenaContractDifficulty.Hard => 80,
            _ => 35
        };

        return Mathf.RoundToInt(baseBudget * multiplier + flatBonus);
    }

    private static int GetGoldReward(Array<MobData> mobs)
    {
        var fameValue = 0;
        foreach (var mob in mobs)
        {
            if (mob is EnemyMobData enemyMob)
                fameValue += enemyMob.FameValue;
        }

        return Mathf.Max(10, Mathf.RoundToInt(fameValue * GoldRewardThreatRatio));
    }

    private static string GetFamilyLabel(EnemyMobFamilyData family)
    {
        return string.IsNullOrWhiteSpace(family?.DisplayName) ? "Enemy" : family.DisplayName;
    }
}
