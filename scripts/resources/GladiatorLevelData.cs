using Godot;

namespace MobArena.Scripts.Resources;

public partial class GladiatorLevelData : Resource
{
    private const int BaseHealth = 20;
    private const int HealthPerVitality = 3;
    private const int BaseStamina = 12;
    private const int StaminaPerEndurance = 2;
    private const float FirstAttributeLevelExp = 200f;
    private const float AttributeLevelExpStep = 20f;
    private const int DefaultAttributeMaxStartingLevel = 5;

    public enum AttributeKind
    {
        Strength,
        Agility,
        Vitality,
        Endurance
    }

    [Export]
    public float StrengthExp { get; private set; }

    [Export]
    public float AgilityExp { get; private set; }

    [Export]
    public float VitalityExp { get; private set; }

    [Export]
    public float EnduranceExp { get; private set; }

    public int Strength => GetAttributeLevel(StrengthExp);

    public int Agility => GetAttributeLevel(AgilityExp);

    public int Vitality => GetAttributeLevel(VitalityExp);

    public int Endurance => GetAttributeLevel(EnduranceExp);

    public float TotalExp => StrengthExp + AgilityExp + VitalityExp + EnduranceExp;

    public int TotalLevel => Strength + Agility + Vitality + Endurance;

    public int GetMaxHealth()
    {
        return BaseHealth + Vitality * HealthPerVitality;
    }

    public int GetMaxStamina()
    {
        return BaseStamina + Endurance * StaminaPerEndurance;
    }

    public float GetAttributeExp(AttributeKind attributeKind)
    {
        return attributeKind switch
        {
            AttributeKind.Agility => AgilityExp,
            AttributeKind.Vitality => VitalityExp,
            AttributeKind.Endurance => EnduranceExp,
            _ => StrengthExp
        };
    }

    public int GetAttributeLevel(AttributeKind attributeKind)
    {
        return GetAttributeLevel(GetAttributeExp(attributeKind));
    }

    public float GetAttributeLevelProgress(AttributeKind attributeKind)
    {
        return GetAttributeLevelProgress(GetAttributeExp(attributeKind));
    }

    public void AddAttributeExp(AttributeKind attributeKind, float amount)
    {
        if (amount <= 0f)
            return;

        SetAttributeExp(attributeKind, GetAttributeExp(attributeKind) + amount);
    }

    public void SetAttributeExp(AttributeKind attributeKind, float exp)
    {
        exp = Mathf.Max(0f, exp);
        switch (attributeKind)
        {
            case AttributeKind.Agility:
                AgilityExp = exp;
                break;
            case AttributeKind.Vitality:
                VitalityExp = exp;
                break;
            case AttributeKind.Endurance:
                EnduranceExp = exp;
                break;
            default:
                StrengthExp = exp;
                break;
        }
    }

    public static int GetAttributeLevel(float exp)
    {
        exp = Mathf.Max(0f, exp);
        var level = 1;
        while (exp >= GetAttributeExpForLevel(level + 1))
            level++;

        return level;
    }

    public static float GetAttributeLevelProgress(float exp)
    {
        var level = GetAttributeLevel(exp);
        var levelStartExp = GetAttributeExpForLevel(level);
        var nextLevelExp = GetAttributeExpForLevel(level + 1);
        return Mathf.Clamp((exp - levelStartExp) / (nextLevelExp - levelStartExp), 0f, 1f);
    }

    public static float GetAttributeExpForLevel(int level)
    {
        if (level <= 1)
            return 0f;

        var completedLevels = level - 1;
        return completedLevels * FirstAttributeLevelExp
            + (completedLevels - 1) * completedLevels * AttributeLevelExpStep * 0.5f;
    }

    public static GladiatorLevelData CreateDefault(RandomNumberGenerator random)
    {
        var maxStartingExp = GetAttributeExpForLevel(DefaultAttributeMaxStartingLevel) - 1f;
        return new GladiatorLevelData
        {
            StrengthExp = random.RandfRange(0f, maxStartingExp),
            AgilityExp = random.RandfRange(0f, maxStartingExp),
            VitalityExp = random.RandfRange(0f, maxStartingExp),
            EnduranceExp = random.RandfRange(0f, maxStartingExp)
        };
    }
}
