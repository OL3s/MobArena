using Godot;

namespace MobArena.Scripts.Resources;

public enum GladiatorRiskStatus
{
    None,
    Idle,
    Exhausted,
    LowHealth,
    Critical
}

public partial class GladiatorData : Resource
{
    public const float MaxConditionValue = 10f;
    private const float DefaultConditionValue = MaxConditionValue * 0.8f;
    private const float DefaultConditionMin = 6f;
    private const float DefaultHealthMinRatio = 0.2f;
    private const float ConditionPenaltyThreshold = 0.5f;
    private const int AttributeValueGold = 2;
    private const int VitalsValueDivisor = 4;

    private static readonly string[] DefaultNames =
    {
        "Aulus",
        "Cassia",
        "Drusus",
        "Livia",
        "Maro",
        "Sabina"
    };

    private static readonly string[] PortraitPaths =
    {
        "res://assets/gladiators/gladiator_01.svg",
        "res://assets/gladiators/gladiator_02.svg",
        "res://assets/gladiators/gladiator_03.svg"
    };

    [Export]
    public string GladiatorName { get; private set; } = "Aulus";

    [Export]
    public int PortraitIndex { get; private set; }

    [Export]
    public int Health { get; private set; } = 35;

    public int MaxHealth => Level?.GetMaxHealth() ?? 0;

    [Export]
    public int Stamina { get; private set; } = 22;

    public int MaxStamina => Level?.GetMaxStamina() ?? 0;

    public int RecoverableMaxHealth => ApplyConditionLimit(MaxHealth);

    public int RecoverableMaxStamina => ApplyConditionLimit(MaxStamina);

    public float RecoverableConditionRatio => GetConditionMultiplier();

    [Export]
    public float Exhaustion { get; private set; } = DefaultConditionValue;

    [Export]
    public GladiatorLevelData Level { get; private set; } = new();

    [Export]
    public GladiatorEquipmentData Equipment { get; private set; } = new();

    [Export]
    public GladiatorCareerData GladiatorCareer { get; private set; } = new();

    [Export]
    public int InitialCost { get; private set; } = 25;

    public int GetMarketValue()
    {
        var attributeValue = Level == null
            ? 0
            : (Level.Strength + Level.Agility + Level.Vitality + Level.Endurance) * AttributeValueGold;
        var vitalsValue = Mathf.RoundToInt((MaxHealth + MaxStamina) / (float)VitalsValueDivisor);
        var baseValue = Mathf.Max(1, InitialCost + attributeValue + vitalsValue);
        return Mathf.Max(1, Mathf.RoundToInt(baseValue * GetSaleConditionMultiplier()));
    }

    public int GetMarketSaleValue()
    {
        return Mathf.Max(1, Mathf.RoundToInt(GetMarketValue() * 0.5f));
    }

    public GladiatorRiskStatus GetRiskStatus(float exhaustionWarningThreshold, float lowHealthWarningRatio)
    {
        var isExhausted = Exhaustion < exhaustionWarningThreshold;
        var isLowHealth = MaxHealth > 0 && Health / (float)MaxHealth < Mathf.Clamp(lowHealthWarningRatio, 0f, 1f);

        if (isExhausted && isLowHealth)
            return GladiatorRiskStatus.Critical;

        if (isExhausted)
            return GladiatorRiskStatus.Exhausted;

        return isLowHealth ? GladiatorRiskStatus.LowHealth : GladiatorRiskStatus.None;
    }

    public static GladiatorData CreateDefault()
    {
        var random = new RandomNumberGenerator();
        random.Randomize();

        var level = GladiatorLevelData.CreateDefault(random);
        var maxHealth = level.GetMaxHealth();
        var health = Mathf.Max(1, Mathf.RoundToInt(maxHealth * random.RandfRange(DefaultHealthMinRatio, 1f)));

        return new GladiatorData
        {
            GladiatorName = DefaultNames[random.RandiRange(0, DefaultNames.Length - 1)],
            PortraitIndex = random.RandiRange(0, PortraitPaths.Length - 1),
            Level = level,
            Health = health,
            Stamina = level.GetMaxStamina(),
            Exhaustion = random.RandfRange(DefaultConditionMin, MaxConditionValue),
            Equipment = GladiatorEquipmentData.CreateDefault(random),
            GladiatorCareer = new GladiatorCareerData(),
            InitialCost = random.RandiRange(20, 45)
        };
    }

    public void SetGladiatorName(string gladiatorName)
    {
        GladiatorName = string.IsNullOrWhiteSpace(gladiatorName)
            ? "Aulus"
            : gladiatorName.Trim();
    }

    public void SetPortraitIndex(int portraitIndex)
    {
        PortraitIndex = NormalizeIndex(portraitIndex, PortraitPaths.Length);
    }

    public Texture2D GetPortraitTexture()
    {
        return ResourceLoader.Load<Texture2D>(PortraitPaths[NormalizeIndex(PortraitIndex, PortraitPaths.Length)]);
    }

    public void AddExhaustion(float amount)
    {
        if (amount <= 0f)
            return;

        SetExhaustion(Exhaustion + amount);
    }

    public void SetExhaustion(float exhaustion)
    {
        Exhaustion = Mathf.Clamp(exhaustion, 0f, MaxConditionValue);
        ClampCurrentVitalsToRecoverableCaps();
    }

    public void RestoreHealth(int amount)
    {
        if (amount <= 0)
            return;

        Health = Mathf.Min(Health + amount, RecoverableMaxHealth);
    }

    public void SetHealth(int health)
    {
        Health = Mathf.Clamp(health, 0, MaxHealth);
        ClampCurrentVitalsToRecoverableCaps();
    }

    public void RestoreStamina(int amount)
    {
        if (amount <= 0)
            return;

        Stamina = Mathf.Min(Stamina + amount, RecoverableMaxStamina);
    }

    public void SpendStamina(int amount)
    {
        if (amount <= 0)
            return;

        Stamina = Mathf.Max(0, Stamina - amount);
    }

    public void ApplyRecoverableCaps()
    {
        ClampCurrentVitalsToRecoverableCaps();
    }

    internal void ApplyDeathState()
    {
        Health = 0;
        Stamina = 0;
    }

    private int ApplyConditionLimit(int baseMax)
    {
        return Mathf.RoundToInt(baseMax * GetConditionMultiplier());
    }

    private float GetConditionMultiplier()
    {
        var conditionRatio = Mathf.Clamp(Exhaustion / MaxConditionValue, 0f, 1f);
        return conditionRatio >= ConditionPenaltyThreshold
            ? 1f
            : conditionRatio / ConditionPenaltyThreshold;
    }

    private float GetSaleConditionMultiplier()
    {
        var lowestConditionRatio = Mathf.Clamp(Exhaustion / MaxConditionValue, 0f, 1f);
        var healthRatio = MaxHealth <= 0 ? 0f : Mathf.Clamp(Health / (float)MaxHealth, 0f, 1f);
        var staminaRatio = MaxStamina <= 0 ? 0f : Mathf.Clamp(Stamina / (float)MaxStamina, 0f, 1f);
        var readinessRatio = (healthRatio + staminaRatio) * 0.5f;

        return Mathf.Clamp(0.25f + lowestConditionRatio * 0.55f + readinessRatio * 0.2f, 0.1f, 1f);
    }

    private void ClampCurrentVitalsToRecoverableCaps()
    {
        Health = Mathf.Min(Health, RecoverableMaxHealth);
        Stamina = Mathf.Min(Stamina, RecoverableMaxStamina);
    }

    private static int NormalizeIndex(int index, int count)
    {
        return ((index % count) + count) % count;
    }
}
