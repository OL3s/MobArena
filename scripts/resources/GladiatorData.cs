using Godot;

namespace MobArena.Scripts.Resources;

public partial class GladiatorData : Resource
{
    private const float MaxConditionValue = 10f;
    private const float DefaultConditionValue = MaxConditionValue * 0.8f;
    private const float ConditionPenaltyThreshold = 0.5f;

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

    public int MaxHealth => Level.GetMaxHealth();

    [Export]
    public int Stamina { get; private set; } = 22;

    public int MaxStamina => Level.GetMaxStamina();

    public int RecoverableMaxHealth => ApplyConditionLimit(MaxHealth);

    public int RecoverableMaxStamina => ApplyConditionLimit(MaxStamina);

    public float RecoverableConditionRatio => GetConditionMultiplier();

    [Export]
    public float Exhaustion { get; private set; } = DefaultConditionValue;

    [Export]
    public float Provisions { get; private set; } = DefaultConditionValue;

    [Export]
    public GladiatorLevelData Level { get; private set; } = new();

    [Export]
    public GladiatorEquipmentData Equipment { get; private set; } = new();

    [Export]
    public GladiatorCareerData GladiatorCareer { get; private set; } = new();

    [Export]
    public int InitialCost { get; private set; } = 25;

    public static GladiatorData CreateDefault()
    {
        var random = new RandomNumberGenerator();
        random.Randomize();

        var level = GladiatorLevelData.CreateDefault(random);

        return new GladiatorData
        {
            GladiatorName = DefaultNames[random.RandiRange(0, DefaultNames.Length - 1)],
            PortraitIndex = random.RandiRange(0, PortraitPaths.Length - 1),
            Level = level,
            Health = level.GetMaxHealth(),
            Stamina = level.GetMaxStamina(),
            Exhaustion = DefaultConditionValue,
            Provisions = DefaultConditionValue,
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

    public void SetProvisions(float provisions)
    {
        Provisions = Mathf.Clamp(provisions, 0f, MaxConditionValue);
        ClampCurrentVitalsToRecoverableCaps();
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

    public void RestoreStamina(int amount)
    {
        if (amount <= 0)
            return;

        Stamina = Mathf.Min(Stamina + amount, RecoverableMaxStamina);
    }

    public void ApplyRecoverableCaps()
    {
        ClampCurrentVitalsToRecoverableCaps();
    }

    internal void ApplyDeathState()
    {
        Health = 0;
        Stamina = 0;
        Provisions = 0f;
    }

    private int ApplyConditionLimit(int baseMax)
    {
        return Mathf.RoundToInt(baseMax * GetConditionMultiplier());
    }

    private float GetConditionMultiplier()
    {
        var conditionRatio = Mathf.Clamp(Mathf.Min(Exhaustion, Provisions) / MaxConditionValue, 0f, 1f);
        return conditionRatio >= ConditionPenaltyThreshold
            ? 1f
            : conditionRatio / ConditionPenaltyThreshold;
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
