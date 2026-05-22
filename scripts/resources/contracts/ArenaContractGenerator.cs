using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scripts.Resources.Contracts;

public static class ArenaContractGenerator
{
    private const string MobResourceDirectory = "res://resources/mobs";
    private const float FameRewardRatio = 0.1f;
    private const float FameDecayRatio = 0.01f;
    private const int MaxMobTypesPerContract = 4;
    private static readonly RandomNumberGenerator Random = new();

    public static Array<ArenaContractData> GenerateContracts(int currentCompanyFame, bool isChampionDay, MobFamily family = MobFamily.Slimes)
    {
        var mobs = LoadEnemyMobs();
        return isChampionDay
            ? GenerateChampionContracts(currentCompanyFame, family, mobs)
            : GenerateStandardContracts(currentCompanyFame, family, mobs);
    }

    public static Array<ArenaContractData> GenerateRandomContracts(int currentCompanyFame, bool isChampionDay)
    {
        var mobs = LoadEnemyMobs();
        return isChampionDay
            ? GenerateRandomChampionContracts(currentCompanyFame, mobs)
            : GenerateRandomStandardContracts(currentCompanyFame, mobs);
    }

    private static Array<ArenaContractData> GenerateRandomStandardContracts(int currentCompanyFame, List<EnemyMobData> mobs)
    {
        var contracts = new Array<ArenaContractData>();
        foreach (var difficulty in new[] { ArenaContractDifficulty.Easy, ArenaContractDifficulty.Medium, ArenaContractDifficulty.Hard })
        {
            var familyMobs = GetRandomEligibleFamilyMobs(mobs, ArenaContractData.GetMobFameBudget(currentCompanyFame, difficulty));
            if (familyMobs.Count > 0)
                contracts.Add(CreateStandardContract(difficulty, currentCompanyFame, familyMobs));
        }

        return contracts;
    }

    private static Array<ArenaContractData> GenerateRandomChampionContracts(int currentCompanyFame, List<EnemyMobData> mobs)
    {
        var contracts = new Array<ArenaContractData>();
        foreach (var difficulty in new[] { ArenaContractDifficulty.Easy, ArenaContractDifficulty.Medium, ArenaContractDifficulty.Hard })
        {
            var budget = GetChampionMobFameBudget(currentCompanyFame, difficulty);
            var champions = GetRandomEligibleChampionFamily(mobs, budget);
            var contract = champions.Count > 0 ? CreateChampionContract(difficulty, currentCompanyFame, champions, mobs) : null;
            if (contract != null)
                contracts.Add(contract);
        }

        return contracts;
    }

    public static Array<MobFamily> GetEligibleFamilies(int currentCompanyFame, bool isChampionDay)
    {
        var mobs = LoadEnemyMobs();
        return GetEligibleFamilies(mobs, currentCompanyFame, isChampionDay);
    }

    private static Array<MobFamily> GetEligibleFamilies(List<EnemyMobData> mobs, int currentCompanyFame, bool isChampionDay)
    {
        var families = new Array<MobFamily>();
        foreach (MobFamily family in Enum.GetValues(typeof(MobFamily)))
        {
            if (IsFamilyEligible(mobs, family, currentCompanyFame, isChampionDay))
                families.Add(family);
        }

        return families;
    }

    private static bool IsFamilyEligible(List<EnemyMobData> mobs, MobFamily family, int currentCompanyFame, bool isChampionDay)
    {
        if (!isChampionDay)
            return HasFamilyMobThatFits(mobs, family, ArenaContractData.GetMobFameBudget(currentCompanyFame, ArenaContractDifficulty.Easy));

        var easyChampionBudget = GetChampionMobFameBudget(currentCompanyFame, ArenaContractDifficulty.Easy);
        var champions = GetEligibleChampions(mobs, family);
        return SelectChampionForBudget(champions, mobs, easyChampionBudget) != null;
    }

    private static Array<ArenaContractData> GenerateStandardContracts(int currentCompanyFame, MobFamily family, List<EnemyMobData> mobs)
    {
        var familyMobs = GetEligibleFamilyMobs(mobs, family, ArenaContractData.GetMobFameBudget(currentCompanyFame, ArenaContractDifficulty.Easy));
        if (familyMobs.Count <= 0)
            return new Array<ArenaContractData>();

        return new Array<ArenaContractData>
        {
            CreateStandardContract(ArenaContractDifficulty.Easy, currentCompanyFame, familyMobs),
            CreateStandardContract(ArenaContractDifficulty.Medium, currentCompanyFame, familyMobs),
            CreateStandardContract(ArenaContractDifficulty.Hard, currentCompanyFame, familyMobs)
        };
    }

    private static Array<ArenaContractData> GenerateChampionContracts(int currentCompanyFame, MobFamily family, List<EnemyMobData> mobs)
    {
        var champions = GetEligibleChampions(mobs, family)
            .OrderByDescending(mob => mob.FameValue)
            .ToList();

        if (champions.Count <= 0)
            champions = mobs
                .OfType<ChampionMobData>()
                .OrderByDescending(mob => mob.FameValue)
                .ToList();

        var contracts = new Array<ArenaContractData>();
        foreach (var difficulty in new[] { ArenaContractDifficulty.Easy, ArenaContractDifficulty.Medium, ArenaContractDifficulty.Hard })
        {
            var contract = CreateChampionContract(difficulty, currentCompanyFame, champions, mobs);
            if (contract != null)
                contracts.Add(contract);
        }

        return contracts;
    }

    private static ArenaContractData CreateChampionContract(
        ArenaContractDifficulty difficulty,
        int currentCompanyFame,
        List<ChampionMobData> champions,
        List<EnemyMobData> mobs)
    {
        var totalBudget = GetChampionMobFameBudget(currentCompanyFame, difficulty);
        var champion = SelectChampionForBudget(champions, mobs, totalBudget);
        if (champion == null)
            return null;

        var supportBudget = Mathf.Max(0, totalBudget - champion.FameValue);
        var supportMobs = GetNonChampionFamilyMobs(mobs, champion.Family)
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
            $"Champion {GetFamilyLabel(champion.Family)} Contract",
            $"A generated {difficulty.ToString().ToLowerInvariant()} Champion Day contract with one champion and same-family support mobs.",
            champion.Family,
            difficulty,
            contractMobs,
            GetGoldReward(contractMobs, difficulty),
            fameRewardRatio: FameRewardRatio,
            fameDecayRatio: FameDecayRatio);
        return contract;
    }

    private static List<ChampionMobData> GetEligibleChampions(List<EnemyMobData> mobs, MobFamily family)
    {
        return mobs
            .OfType<ChampionMobData>()
            .Where(mob => mob.Family == family)
            .ToList();
    }

    private static ChampionMobData SelectChampionForBudget(List<ChampionMobData> champions, List<EnemyMobData> mobs, int totalBudget)
    {
        return champions
            .Select(champion => new
            {
                Champion = champion,
                CheapestSupportFame = GetCheapestSupportFame(mobs, champion.Family, champion.FameValue)
            })
            .Where(option => option.CheapestSupportFame > 0 && option.Champion.FameValue + option.CheapestSupportFame <= totalBudget)
            .OrderByDescending(option => option.Champion.FameValue)
            .Select(option => option.Champion)
            .FirstOrDefault();
    }

    private static int GetCheapestSupportFame(List<EnemyMobData> mobs, MobFamily family, int maxSupportFame)
    {
        return mobs
            .Where(mob => mob.Family == family
                && mob is not ChampionMobData
                && mob.FameValue > 0
                && mob.FameValue <= maxSupportFame)
            .OrderBy(mob => mob.FameValue)
            .Select(mob => mob.FameValue)
            .FirstOrDefault();
    }

    private static ArenaContractData CreateStandardContract(
        ArenaContractDifficulty difficulty,
        int currentCompanyFame,
        List<EnemyMobData> familyMobs)
    {
        var budget = ArenaContractData.GetMobFameBudget(currentCompanyFame, difficulty);
        var maxTotalMobCount = GetMaxTotalMobCount(difficulty);

        var family = familyMobs.Count > 0 ? familyMobs[0].Family : MobFamily.Slimes;
        var contractMobs = FillMobBudget(familyMobs, budget, maxTotalMobCount, MaxMobTypesPerContract);
        var contract = new ArenaContractData();
        contract.ConfigureGenerated(
            $"{GetFamilyLabel(family)} Contract",
            $"A generated {difficulty.ToString().ToLowerInvariant()} contract built from {GetFamilyLabel(family).ToLowerInvariant()} mobs and current company fame.",
            family,
            difficulty,
            contractMobs,
            GetGoldReward(contractMobs, difficulty),
            fameRewardRatio: FameRewardRatio,
            fameDecayRatio: FameDecayRatio);
        return contract;
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

    private static List<EnemyMobData> GetEligibleFamilyMobs(List<EnemyMobData> mobs, MobFamily preferredFamily, int minimumBudget)
    {
        if (HasFamilyMobThatFits(mobs, preferredFamily, minimumBudget))
            return GetNonChampionFamilyMobs(mobs, preferredFamily);

        var fallbackMob = mobs
            .Where(mob => mob is not ChampionMobData && mob.FameValue > 0 && mob.FameValue <= minimumBudget)
            .OrderBy(mob => mob.FameValue)
            .FirstOrDefault();

        return fallbackMob == null
            ? new List<EnemyMobData>()
            : GetNonChampionFamilyMobs(mobs, fallbackMob.Family);
    }

    private static List<EnemyMobData> GetRandomEligibleFamilyMobs(List<EnemyMobData> mobs, int budget)
    {
        var families = new List<MobFamily>();
        foreach (MobFamily family in Enum.GetValues(typeof(MobFamily)))
        {
            if (HasFamilyMobThatFits(mobs, family, budget))
                families.Add(family);
        }

        if (families.Count <= 0)
            return new List<EnemyMobData>();

        return GetNonChampionFamilyMobs(mobs, families[Random.RandiRange(0, families.Count - 1)]);
    }

    private static List<ChampionMobData> GetRandomEligibleChampionFamily(List<EnemyMobData> mobs, int budget)
    {
        var familyChampions = new List<List<ChampionMobData>>();
        foreach (MobFamily family in Enum.GetValues(typeof(MobFamily)))
        {
            var champions = GetEligibleChampions(mobs, family);
            if (SelectChampionForBudget(champions, mobs, budget) != null)
                familyChampions.Add(champions);
        }

        return familyChampions.Count <= 0
            ? new List<ChampionMobData>()
            : familyChampions[Random.RandiRange(0, familyChampions.Count - 1)];
    }

    private static bool HasFamilyMobThatFits(List<EnemyMobData> mobs, MobFamily family, int budget)
    {
        return mobs.Any(mob => mob.Family == family
            && mob is not ChampionMobData
            && mob.FameValue > 0
            && mob.FameValue <= budget);
    }

    private static List<EnemyMobData> GetNonChampionFamilyMobs(List<EnemyMobData> mobs, MobFamily family)
    {
        return mobs
            .Where(mob => mob.Family == family && mob is not ChampionMobData)
            .ToList();
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

    private static int GetGoldReward(Array<MobData> mobs, ArenaContractDifficulty difficulty)
    {
        var fameValue = 0;
        foreach (var mob in mobs)
        {
            if (mob is EnemyMobData enemyMob)
                fameValue += enemyMob.FameValue;
        }

        var multiplier = difficulty switch
        {
            ArenaContractDifficulty.Easy => 1.2f,
            ArenaContractDifficulty.Medium => 1.55f,
            ArenaContractDifficulty.Hard => 2.0f,
            ArenaContractDifficulty.Champion => 2.5f,
            _ => 1.0f
        };
        return Mathf.Max(10, Mathf.RoundToInt(fameValue * multiplier));
    }

    private static string GetFamilyLabel(MobFamily family)
    {
        return family switch
        {
            MobFamily.Slimes => "Slime",
            MobFamily.Goblins => "Goblin",
            MobFamily.Undead => "Undead",
            MobFamily.Demons => "Demon",
            _ => family.ToString()
        };
    }

    private static List<EnemyMobData> LoadEnemyMobs()
    {
        var mobs = new List<EnemyMobData>();
        foreach (var path in GetTresPaths(MobResourceDirectory))
        {
            var mob = ResourceLoader.Load<EnemyMobData>(path);
            if (mob != null)
                mobs.Add(mob);
        }

        return mobs;
    }

    private static IEnumerable<string> GetTresPaths(string directoryPath)
    {
        var directory = DirAccess.Open(directoryPath);
        if (directory == null)
            yield break;

        directory.ListDirBegin();
        while (true)
        {
            var entry = directory.GetNext();
            if (string.IsNullOrEmpty(entry))
                break;

            if (entry.StartsWith(".", StringComparison.Ordinal))
                continue;

            var path = $"{directoryPath}/{entry}";
            if (directory.CurrentIsDir())
            {
                foreach (var childPath in GetTresPaths(path))
                    yield return childPath;
            }
            else if (entry.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
            {
                yield return path;
            }
        }

        directory.ListDirEnd();
    }
}
