using Godot;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Gladiators;
using MobArena.Scripts.Resources.Items;

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
    private const float ConditionPenaltyThreshold = 0.5f;
    private const int AttributeValueGold = 2;
    private const int VitalsValueDivisor = 40;

    private static readonly string[] AppearancePaths =
    {
        "res://resources/gladiator_appearances/appearance_01.tres",
        "res://resources/gladiator_appearances/appearance_02.tres",
        "res://resources/gladiator_appearances/appearance_03.tres"
    };

    [Export]
    public string GladiatorName { get; private set; } = "Aulus";

    [Export]
    public int PortraitIndex { get; private set; }

    [Export]
    public GladiatorAppearanceData Appearance { get; private set; }

    [Export]
    public int Health { get; private set; } = 350;

    public int MaxHealth => Level?.GetMaxHealth() ?? 0;

    [Export]
    public int Stamina { get; private set; } = 220;

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

    public int GetArmorValue(CombatDamageType damageType)
    {
        return Equipment?.Armor?.GetArmorValue(damageType) ?? 0;
    }

    public int ApplyArmorToDamage(int damage, CombatDamageType damageType)
    {
        return ArmorItemData.ApplyArmorToDamage(damage, GetArmorValue(damageType));
    }

    public int ApplyArmorToDamage(CombatDamageData damageData)
    {
        return damageData?.GetMitigatedTotalDamage(this) ?? 0;
    }

    public static GladiatorData CreateDefault()
    {
        return GladiatorGenerator.CreateDefault();
    }

    internal static int AppearanceCount => AppearancePaths.Length;

    internal static GladiatorData CreateGenerated(
        string gladiatorName,
        int portraitIndex,
        GladiatorLevelData level,
        int health,
        int stamina,
        float exhaustion,
        GladiatorEquipmentData equipment,
        GladiatorCareerData gladiatorCareer,
        int initialCost)
    {
        return new GladiatorData
        {
            GladiatorName = string.IsNullOrWhiteSpace(gladiatorName) ? "Aulus" : gladiatorName.Trim(),
            PortraitIndex = NormalizeIndex(portraitIndex, AppearancePaths.Length),
            Appearance = LoadAppearance(portraitIndex),
            Level = level ?? new GladiatorLevelData(),
            Health = health,
            Stamina = stamina,
            Exhaustion = Mathf.Clamp(exhaustion, 0f, MaxConditionValue),
            Equipment = equipment ?? new GladiatorEquipmentData(),
            GladiatorCareer = gladiatorCareer ?? new GladiatorCareerData(),
            InitialCost = Mathf.Max(1, initialCost)
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
        PortraitIndex = NormalizeIndex(portraitIndex, AppearancePaths.Length);
        Appearance = LoadAppearance(PortraitIndex);
    }

    public Texture2D GetUiIconTexture()
    {
        return GetAppearance()?.UiIcon ?? ResourceLoader.Load<Texture2D>("res://assets/gladiators/gladiator_01.svg");
    }

    public Texture2D GetBodyForwardTexture()
    {
        return GetAppearance()?.BodyForward ?? GetUiIconTexture();
    }

    public Texture2D GetBodyBackTexture()
    {
        return GetAppearance()?.BodyBack ?? GetBodyForwardTexture();
    }

    public bool UsesSeparatedHands()
    {
        return GetAppearance()?.UsesSeparatedHands == true;
    }

    public Texture2D GetHandTexture()
    {
        return GetAppearance()?.HandTexture;
    }

    public GladiatorAppearanceData GetAppearance()
    {
        Appearance ??= LoadAppearance(PortraitIndex);
        return Appearance;
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

    private static GladiatorAppearanceData LoadAppearance(int index)
    {
        return ResourceLoader.Load<GladiatorAppearanceData>(AppearancePaths[NormalizeIndex(index, AppearancePaths.Length)]);
    }
}
