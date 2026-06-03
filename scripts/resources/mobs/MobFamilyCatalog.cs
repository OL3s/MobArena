using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MobArena.Scripts.Resources.Mobs;

public static class MobFamilyCatalog
{
    private const string MobFamilyResourceDirectory = "res://resources/mob_families";

    public static Array<EnemyMobFamilyData> LoadEnemyFamilies()
    {
        var families = new Array<EnemyMobFamilyData>();
        foreach (var family in LoadEnemyFamiliesList())
            families.Add(family);

        return families;
    }

    public static List<EnemyMobFamilyData> LoadEnemyFamiliesList()
    {
        return GetTresPaths(MobFamilyResourceDirectory)
            .Select(path => ResourceLoader.Load<EnemyMobFamilyData>(path))
            .Where(family => family != null)
            .OrderBy(family => family.FameValue)
            .ThenBy(family => family.DisplayName)
            .ToList();
    }

    public static EnemyMobFamilyData FindFamily(MobFamily family)
    {
        return LoadEnemyFamiliesList().FirstOrDefault(familyData => familyData.Family == family);
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
