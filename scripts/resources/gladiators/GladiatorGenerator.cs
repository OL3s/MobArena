using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources.Gladiators;

public static class GladiatorGenerator
{
    private const float DefaultConditionMin = 6f;
    private const float DefaultHealthMinRatio = 0.2f;

    private static readonly string[] DefaultNames =
    {
        "Aulus",
        "Cassia",
        "Drusus",
        "Livia",
        "Maro",
        "Sabina"
    };

    public static GladiatorData CreateDefault()
    {
        var random = new RandomNumberGenerator();
        random.Randomize();
        return CreateDefault(random);
    }

    public static GladiatorData CreateDefault(RandomNumberGenerator random)
    {
        random ??= new RandomNumberGenerator();
        var level = GladiatorLevelData.CreateDefault(random);
        var maxHealth = level.GetMaxHealth();
        var health = Mathf.Max(1, Mathf.RoundToInt(maxHealth * random.RandfRange(DefaultHealthMinRatio, 1f)));
        var appearanceIndex = random.RandiRange(0, GladiatorData.AppearanceCount - 1);

        return GladiatorData.CreateGenerated(
            DefaultNames[random.RandiRange(0, DefaultNames.Length - 1)],
            appearanceIndex,
            level,
            health,
            level.GetMaxStamina(),
            random.RandfRange(DefaultConditionMin, GladiatorData.MaxConditionValue),
            GladiatorEquipmentData.CreateDefault(random),
            new GladiatorCareerData(),
            random.RandiRange(20, 45));
    }
}
