using Godot;
using MobArena.Scripts;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Effects;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.Arena;

public abstract partial class ArenaCombatant : CharacterBody2D
{
    [Signal]
    public delegate void CombatantStateChangedEventHandler(ArenaCombatantState state);

    private const string ArenaCombatantGroup = "arena_combatants";
    private const uint WallCollisionMask = 1u;
    private const uint CombatantCollisionLayer = 2u;
    private const float FallbackStateSpeedMultiplier = 0.5f;

    private static readonly Dictionary<ArenaCombatantState, float> StateSpeedMultipliers = new()
    {
        { ArenaCombatantState.Default, 1f },
        { ArenaCombatantState.Exhausted, 0.5f },
        { ArenaCombatantState.Release, 0.2f },
        { ArenaCombatantState.Windup, 0.1f },
        { ArenaCombatantState.Stunned, 0f }
    };

    [Export]
    public float MoveSpeed { get; protected set; } = 160f;

    [Export]
    public float SoftCollisionRadius { get; protected set; } = 28f;

    [Export]
    public float SoftCollisionStrength { get; protected set; } = 180f;

    [Export]
    public float MaxSoftCollisionSpeed { get; protected set; } = 160f;

    [Export]
    public float ExternalForceFriction { get; protected set; } = 900f;

    [Export]
    public Vector2 LookDirection { get; private set; } = Vector2.Right;

    public ArenaCombatTeam Team { get; private set; } = ArenaCombatTeam.Neutral;

    public ArenaCombatState CombatState { get; private set; }

    public bool IsDead => CombatState?.IsDead == true;

    public ArenaCombatantState CombatantState { get; private set; } = ArenaCombatantState.Default;

    public Vector2 ExternalForce { get; private set; } = Vector2.Zero;

    private float _visualXSign = 1f;

    public override void _Process(double delta)
    {
        ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
    }

    public override void _ExitTree()
    {
        DetachCombatStateSignals();
    }

    protected void ConfigureTopDownMotion()
    {
        MotionMode = MotionModeEnum.Floating;
        CollisionLayer = CombatantCollisionLayer;
        CollisionMask = WallCollisionMask;
        AddToGroup(ArenaCombatantGroup);
    }

    protected void ConfigureCombatState(ArenaCombatState combatState, ArenaCombatTeam team)
    {
        DetachCombatStateSignals();

        CombatState = combatState;
        Team = team;

        if (CombatState == null)
            return;

        CombatState.HealthChanged += OnCombatStateHealthChanged;
        CombatState.Died += OnCombatStateDied;
        OnCombatStateHealthChanged(CombatState.CurrentHealth, CombatState.MaxHealth);
    }

    private void DetachCombatStateSignals()
    {
        if (CombatState == null)
            return;

        CombatState.HealthChanged -= OnCombatStateHealthChanged;
        CombatState.Died -= OnCombatStateDied;
    }

    public bool CanReceiveDamageFrom(ArenaCombatant source)
    {
        if (IsDead)
            return false;

        if (source == null || source.Team == ArenaCombatTeam.Neutral || Team == ArenaCombatTeam.Neutral)
            return true;

        return source.Team != Team;
    }

    public bool CheckVulnerable()
    {
        return CombatantState == ArenaCombatantState.Windup;
    }

    public bool CanMove()
    {
        return CombatantState is ArenaCombatantState.Default
            or ArenaCombatantState.Exhausted
            or ArenaCombatantState.Windup
            or ArenaCombatantState.Release
            or ArenaCombatantState.Blocking;
    }

    public bool CanReceiveInput()
    {
        return CombatantState is ArenaCombatantState.Default
            or ArenaCombatantState.Exhausted
            or ArenaCombatantState.Windup
            or ArenaCombatantState.Release
            or ArenaCombatantState.Blocking;
    }

    public bool CanStartAction()
    {
        return CombatantState == ArenaCombatantState.Default;
    }

    public float GetSpeedMultiplier()
    {
        return StateSpeedMultipliers.GetValueOrDefault(CombatantState, FallbackStateSpeedMultiplier);
    }

    public void AddExternalForce(Vector2 force)
    {
        ExternalForce += force;
    }

    public void ClearExternalForce()
    {
        ExternalForce = Vector2.Zero;
    }

    public float ApplyStatusEffect(StatusEffectApplicationData application, int appliedDamage, ArenaCombatant source = null)
    {
        if (application == null || CombatState == null || IsDead)
            return 0f;

        var value = application.ResolveValue(appliedDamage);
        var actualValue = CombatState.ApplyStatusEffect(application.Type, value, CombatantState);
        if (application.Type == StatusEffectType.Stun && CombatState.HasActiveStatus(StatusEffectType.Stun))
            SetCombatantState(ArenaCombatantState.Stunned);

        return actualValue;
    }

    protected void SetCombatantState(ArenaCombatantState state)
    {
        if (CombatantState == state)
            return;

        CombatantState = state;
        EmitSignal(SignalName.CombatantStateChanged, (int)CombatantState);
        OnCombatantStateChanged(CombatantState);
    }

    public int ApplyDamage(CombatDamageData damage, ArenaCombatant source = null)
    {
        if (!CanReceiveDamageFrom(source))
            return 0;

        return CombatState?.ApplyDamage(damage) ?? 0;
    }

    public int ApplyRawDamage(int amount, ArenaCombatant source = null)
    {
        if (!CanReceiveDamageFrom(source))
            return 0;

        return CombatState?.ApplyRawDamage(amount) ?? 0;
    }

    public void SetDamageLocked(bool locked)
    {
        CombatState?.SetDamageLocked(locked);
    }

    public void SetDeathPreventionEnabled(bool enabled)
    {
        CombatState?.SetDeathPreventionEnabled(enabled);
    }

    protected virtual void OnCombatStateHealthChanged(int currentHealth, int maxHealth)
    {
    }

    protected virtual void OnCombatStateDied()
    {
        SetCombatantState(ArenaCombatantState.Dead);
    }

    protected virtual void OnCombatantStateChanged(ArenaCombatantState state)
    {
    }

    protected void ApplyDebugStateModulate(params CanvasItem[] visuals)
    {
        var color = SaveNode.Get()?.DevEnabled == true
            ? GetDebugStateColor(CombatantState)
            : Colors.White;

        foreach (var visual in visuals)
        {
            if (visual != null)
                visual.Modulate = color;
        }
    }

    private static Color GetDebugStateColor(ArenaCombatantState state)
    {
        return state switch
        {
            ArenaCombatantState.Default => Colors.White,
            ArenaCombatantState.Windup => new Color(1f, 0.72f, 0.35f),
            ArenaCombatantState.Release => new Color(1f, 0.9f, 0.35f),
            ArenaCombatantState.Stunned => new Color(1f, 0.35f, 0.35f),
            ArenaCombatantState.Dead => new Color(0.45f, 0.45f, 0.45f),
            ArenaCombatantState.Dodging => new Color(0.55f, 1f, 0.65f),
            ArenaCombatantState.Blocking => new Color(0.65f, 0.8f, 1f),
            ArenaCombatantState.Exhausted => new Color(0.7f, 0.85f, 1f),
            _ => Colors.White
        };
    }

    protected void MoveWithDirection(Vector2 direction)
    {
        Velocity = direction.Normalized() * MoveSpeed * GetSpeedMultiplier() + GetSoftCollisionVelocity() + ExternalForce;
        SetLookDirectionFromInput(direction);
        MoveAndSlide();
    }

    protected void TickCombatantStatusEffects(float delta)
    {
        CombatState?.TickStatusEffects(delta);
        if (CombatantState == ArenaCombatantState.Stunned && CombatState?.HasActiveStatus(StatusEffectType.Stun) != true)
            SetCombatantState(ArenaCombatantState.Default);
    }

    protected void MoveWithInputState(ArenaCombatInputState inputState)
    {
        var desiredVelocity = inputState?.IsMoving == true
            ? inputState.MoveDirection * inputState.MoveStrength * MoveSpeed * GetSpeedMultiplier()
            : Vector2.Zero;

        Velocity = desiredVelocity + GetSoftCollisionVelocity() + ExternalForce;
        MoveAndSlide();
    }

    protected void MoveWithSoftCollisionOnly()
    {
        Velocity = GetSoftCollisionVelocity() + ExternalForce;
        MoveAndSlide();
    }

    protected void DecayExternalForce(float delta)
    {
        if (delta <= 0f || ExternalForce == Vector2.Zero)
            return;

        ExternalForce = ExternalForce.MoveToward(Vector2.Zero, Mathf.Max(0f, ExternalForceFriction) * delta);
    }

    private Vector2 GetSoftCollisionVelocity()
    {
        if (SoftCollisionRadius <= 0f || SoftCollisionStrength <= 0f)
            return Vector2.Zero;

        var push = Vector2.Zero;
        foreach (var node in GetTree().GetNodesInGroup(ArenaCombatantGroup))
        {
            if (node is not ArenaCombatant other || other == this || !IsInstanceValid(other))
                continue;

            var combinedRadius = SoftCollisionRadius + other.SoftCollisionRadius;
            if (combinedRadius <= 0f)
                continue;

            var away = GlobalPosition - other.GlobalPosition;
            var distanceSquared = away.LengthSquared();
            if (distanceSquared >= combinedRadius * combinedRadius)
                continue;

            if (distanceSquared <= 0.001f)
                away = GetInstanceId() > other.GetInstanceId() ? Vector2.Right : Vector2.Left;

            var distance = Mathf.Max(away.Length(), 0.001f);
            var overlapRatio = (combinedRadius - distance) / combinedRadius;
            push += away / distance * overlapRatio * SoftCollisionStrength;
        }

        return push.Length() > MaxSoftCollisionSpeed
            ? push.Normalized() * MaxSoftCollisionSpeed
            : push;
    }

    public void SetLookDirection(Vector2 lookDirection)
    {
        if (lookDirection == Vector2.Zero)
            return;

        LookDirection = lookDirection.Normalized();
        if (!Mathf.IsZeroApprox(LookDirection.X))
            _visualXSign = Mathf.Sign(LookDirection.X);
    }

    protected void SetLookDirectionFromInput(Vector2 lookDirection)
    {
        SetLookDirection(lookDirection);
    }

    protected void ForceLookDirection(Vector2 lookDirection)
    {
        if (lookDirection == Vector2.Zero)
            return;

        LookDirection = lookDirection.Normalized();
    }

    protected void ApplyLookVisual(Sprite2D sprite, Texture2D frontTexture, Texture2D backTexture, float displayHeight)
    {
        if (sprite == null)
            return;

        var xSign = GetVisualXSign();

        var texture = LookDirection.Y < 0f
            ? backTexture ?? frontTexture
            : frontTexture ?? backTexture;

        sprite.Texture = texture;
        if (texture != null && texture.GetHeight() > 0)
        {
            var scale = displayHeight / texture.GetHeight();
            sprite.Scale = new Vector2(scale * xSign, scale);
        }
    }

    protected void ApplyDirectionalVisual(Sprite2D sprite, Texture2D texture, float displayHeight, Vector2 localPosition)
    {
        if (sprite == null)
            return;

        if (texture == null)
        {
            sprite.Hide();
            return;
        }

        var xSign = GetVisualXSign();
        sprite.Show();
        sprite.Texture = texture;
        sprite.Position = new Vector2(localPosition.X * xSign, localPosition.Y);

        if (texture.GetHeight() > 0)
        {
            var scale = displayHeight / texture.GetHeight();
            sprite.Scale = new Vector2(scale * xSign, scale);
        }
    }

    protected float GetVisualXSign()
    {
        return _visualXSign;
    }

    protected static void FitSpriteHeight(Sprite2D sprite, Texture2D texture, float displayHeight)
    {
        if (sprite == null)
            return;

        sprite.Texture = texture;
        if (texture != null && texture.GetHeight() > 0)
            sprite.Scale = Vector2.One * (displayHeight / texture.GetHeight());
    }
}
