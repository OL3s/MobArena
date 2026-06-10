using Godot;
using MobArena.Scenes.Components.Arena;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Items;
using System.Collections.Generic;

namespace MobArena.Scripts.Resources.Combat;

[GlobalClass]
public partial class ArenaCombatState : Resource
{
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void StatusValueChangedEventHandler(StatusEffectType type, float currentValue);

    private const float StatusValueDecayPerSecond = 100f;
    private const int PoisonDamagePerSecond = 100;

    [Export]
    public int MaxHealth { get; private set; } = 1;

    [Export]
    public int CurrentHealth { get; private set; } = 1;

    [Export]
    public ArmorData ArmorProfile { get; private set; }

    [Export]
    public CombatantStatusProfileData StatusProfile { get; private set; }

    private readonly Dictionary<StatusEffectType, float> _statusValues = new();
    private float _poisonTickAccumulator;
    private int _minimumHealthFloor;
    private bool _damageLocked;

    public bool IsDead => CurrentHealth <= 0;

    public void Configure(int maxHealth, int currentHealth = -1, ArmorData armorProfile = null, CombatantStatusProfileData statusProfile = null)
    {
        MaxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = currentHealth < 0
            ? MaxHealth
            : Mathf.Clamp(currentHealth, 0, MaxHealth);
        ArmorProfile = armorProfile;
        StatusProfile = statusProfile ?? new CombatantStatusProfileData();
        _statusValues.Clear();
        _poisonTickAccumulator = 0f;
        _minimumHealthFloor = 0;
        _damageLocked = false;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public int ApplyDamage(CombatDamageData damage)
    {
        return damage == null ? 0 : ApplyRawDamage(GetMitigatedDamage(damage));
    }

    public int ApplyRawDamage(int amount)
    {
        if (_damageLocked || IsDead || amount <= 0)
            return 0;

        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(_minimumHealthFloor, CurrentHealth - amount);
        EmitHealthChangedIfNeeded(previousHealth);

        if (CurrentHealth <= 0 && previousHealth > 0)
            EmitSignal(SignalName.Died);

        return previousHealth - CurrentHealth;
    }

    public void SetDamageLocked(bool locked)
    {
        _damageLocked = locked;
    }

    public void SetDeathPreventionEnabled(bool enabled)
    {
        _minimumHealthFloor = enabled && CurrentHealth > 0 ? 1 : 0;
    }

    public int GetMitigatedDamage(CombatDamageData damage)
    {
        return damage?.GetMitigatedTotalDamage(ArmorProfile) ?? 0;
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
        EmitHealthChangedIfNeeded(previousHealth);
    }

    public void SetHealth(int health)
    {
        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Clamp(health, 0, MaxHealth);
        EmitHealthChangedIfNeeded(previousHealth);

        if (CurrentHealth <= 0 && previousHealth > 0)
            EmitSignal(SignalName.Died);
    }

    public float ApplyStatusEffect(StatusEffectType type, float value, ArenaCombatantState combatantState = ArenaCombatantState.Default)
    {
        var scaledValue = value * (StatusProfile?.GetStateMultiplier(combatantState, type) ?? 1f);
        var defendedValue = StatusProfile?.ApplyDefenseToEffect(scaledValue, type) ?? scaledValue;
        var maxValue = StatusProfile?.GetMaxValue(type) ?? 300f;
        if (defendedValue <= 0f || maxValue <= 0f)
            return 0f;

        _statusValues.TryGetValue(type, out var currentValue);
        var nextValue = Mathf.Min(maxValue, Mathf.Max(currentValue, defendedValue));
        if (Mathf.IsEqualApprox(currentValue, nextValue))
            return 0f;

        _statusValues[type] = nextValue;
        EmitSignal(SignalName.StatusValueChanged, (int)type, nextValue);
        return nextValue;
    }

    public float GetStatusValue(StatusEffectType type)
    {
        return _statusValues.GetValueOrDefault(type, 0f);
    }

    public bool HasActiveStatus(StatusEffectType type)
    {
        return GetStatusValue(type) > (StatusProfile?.GetMinValue(type) ?? 1f);
    }

    public void TickStatusEffects(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || _statusValues.Count <= 0)
        {
            _poisonTickAccumulator = 0f;
            return;
        }

        TickPoisonDamage(deltaSeconds);

        var decay = deltaSeconds * StatusValueDecayPerSecond;
        var keys = new List<StatusEffectType>(_statusValues.Keys);
        foreach (var type in keys)
        {
            var current = _statusValues[type];
            var next = Mathf.Max(0f, current - decay);
            if (Mathf.IsEqualApprox(current, next))
                continue;

            if (next <= 0f)
                _statusValues.Remove(type);
            else
                _statusValues[type] = next;

            EmitSignal(SignalName.StatusValueChanged, (int)type, next);
        }
    }

    private void TickPoisonDamage(float deltaSeconds)
    {
        if (!HasActiveStatus(StatusEffectType.Poison))
        {
            _poisonTickAccumulator = 0f;
            return;
        }

        _poisonTickAccumulator += deltaSeconds;
        while (_poisonTickAccumulator >= 1f && !IsDead && HasActiveStatus(StatusEffectType.Poison))
        {
            _poisonTickAccumulator -= 1f;
            ApplyRawDamage(PoisonDamagePerSecond);
        }
    }

    private void EmitHealthChangedIfNeeded(int previousHealth)
    {
        if (CurrentHealth != previousHealth)
            EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}
