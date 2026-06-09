using Godot;
using Godot.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MobArena.Scripts.Resources.Mobs;

[GlobalClass]
public partial class EnemyMobFamilyData : MobFamilyData
{
    [Export]
    public Array<MobFamilyMobEntryData> Mobs { get; private set; } = new();

    public List<EnemyMobData> GetEligibleMobs(int currentCompanyFame, bool includeChampions = true)
    {
        return GetEligibleEntries(currentCompanyFame, includeChampions)
            .Select(entry => entry.Mob)
            .Where(mob => mob != null)
            .ToList();
    }

    public List<MobFamilyMobEntryData> GetEligibleEntries(int currentCompanyFame, bool includeChampions = true)
    {
        var entries = new List<MobFamilyMobEntryData>();
        foreach (var entry in Mobs)
        {
            if (entry?.Mob == null)
                continue;

            if (!includeChampions && entry.Mob is ChampionMobData)
                continue;

            if (entry.MinimumCompanyFame <= currentCompanyFame)
                entries.Add(entry);
        }

        return entries;
    }
}
