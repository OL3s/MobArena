using Godot;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scripts.Resources.Combat;

[GlobalClass]
public partial class ArenaCombatState : Resource
{
    [Signal]
    public delegate void HealthChangedEventHandler(int currentHealth, int maxHealth);

    [Signal]
    public delegate void DiedEventHandler();

    [Export]
    public int MaxHealth { get; private set; } = 1;

    [Export]
    public int CurrentHealth { get; private set; } = 1;

    [Export]
    public ArmorData ArmorProfile { get; private set; }

    public bool IsDead => CurrentHealth <= 0;

    public void Configure(int maxHealth, int currentHealth = -1, ArmorData armorProfile = null)
    {
        MaxHealth = Mathf.Max(1, maxHealth);
        CurrentHealth = currentHealth < 0
            ? MaxHealth
            : Mathf.Clamp(currentHealth, 0, MaxHealth);
        ArmorProfile = armorProfile;
        EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }

    public int ApplyDamage(CombatDamageData damage)
    {
        return damage == null ? 0 : ApplyRawDamage(GetMitigatedDamage(damage));
    }

    public int ApplyRawDamage(int amount)
    {
        if (IsDead || amount <= 0)
            return 0;

        var previousHealth = CurrentHealth;
        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        EmitHealthChangedIfNeeded(previousHealth);

        if (CurrentHealth <= 0 && previousHealth > 0)
            EmitSignal(SignalName.Died);

        return previousHealth - CurrentHealth;
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

    private void EmitHealthChangedIfNeeded(int previousHealth)
    {
        if (CurrentHealth != previousHealth)
            EmitSignal(SignalName.HealthChanged, CurrentHealth, MaxHealth);
    }
}
