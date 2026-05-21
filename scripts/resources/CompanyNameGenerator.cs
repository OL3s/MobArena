using Godot;

namespace MobArena.Scripts.Resources;

public static class CompanyNameGenerator
{
    private static readonly string[] Prefixes = { "The", "House", "Order", "Clan", "Company", "Guild", "Band", "Circle" };
    private static readonly string[] Adjectives = { "Red", "Bronze", "Iron", "Golden", "Silver", "Crimson", "Azure", "Emerald", "Purple", "Black", "White", "Storm" };
    private static readonly string[] Nouns = { "Lions", "Wolves", "Griffins", "Banners", "Blades", "Shields", "Barrels", "Hammers", "Crows", "Bulls", "Dragons", "Sentinels" };

    public static string CreateRandomName()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        return CreateRandomName(rng);
    }

    public static string CreateRandomName(RandomNumberGenerator rng)
    {
        rng ??= new RandomNumberGenerator();
        var prefix = Prefixes[rng.RandiRange(0, Prefixes.Length - 1)];
        var adjective = Adjectives[rng.RandiRange(0, Adjectives.Length - 1)];
        var noun = Nouns[rng.RandiRange(0, Nouns.Length - 1)];
        return $"{prefix} {adjective} {noun}";
    }
}
