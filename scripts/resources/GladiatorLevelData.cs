using Godot;

namespace MobArena.Scripts.Resources;

public partial class GladiatorLevelData : Resource
{
    private const int BaseHealth = 20;
    private const int HealthPerVitality = 3;
    private const int BaseStamina = 12;
    private const int StaminaPerEndurance = 2;

    [Export]
    public int Strength { get; private set; } = 5;

    [Export]
    public int Agility { get; private set; } = 5;

    [Export]
    public int Vitality { get; private set; } = 5;

    [Export]
    public int Endurance { get; private set; } = 5;

    public int GetMaxHealth()
    {
        return BaseHealth + Vitality * HealthPerVitality;
    }

    public int GetMaxStamina()
    {
        return BaseStamina + Endurance * StaminaPerEndurance;
    }

    public static GladiatorLevelData CreateDefault(RandomNumberGenerator random)
    {
        return new GladiatorLevelData
        {
            Strength = random.RandiRange(4, 7),
            Agility = random.RandiRange(4, 7),
            Vitality = random.RandiRange(4, 7),
            Endurance = random.RandiRange(4, 7)
        };
    }
}
