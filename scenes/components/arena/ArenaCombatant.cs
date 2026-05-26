using Godot;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scenes.Components.Arena;

public abstract partial class ArenaCombatant : CharacterBody2D
{
    private const string ArenaCombatantGroup = "arena_combatants";
    private const uint WallCollisionMask = 1u;
    private const uint CombatantCollisionLayer = 2u;

    [Export]
    public float MoveSpeed { get; protected set; } = 160f;

    [Export]
    public float SoftCollisionRadius { get; protected set; } = 28f;

    [Export]
    public float SoftCollisionStrength { get; protected set; } = 180f;

    [Export]
    public float MaxSoftCollisionSpeed { get; protected set; } = 160f;

    [Export]
    public Vector2 LookDirection { get; private set; } = Vector2.Right;

    public ArenaCombatTeam Team { get; private set; } = ArenaCombatTeam.Neutral;

    public ArenaCombatState CombatState { get; private set; }

    public bool IsDead => CombatState?.IsDead == true;

    public override void _Process(double delta)
    {
        ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
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
        if (CombatState != null)
        {
            CombatState.HealthChanged -= OnCombatStateHealthChanged;
            CombatState.Died -= OnCombatStateDied;
        }

        CombatState = combatState;
        Team = team;

        if (CombatState == null)
            return;

        CombatState.HealthChanged += OnCombatStateHealthChanged;
        CombatState.Died += OnCombatStateDied;
        OnCombatStateHealthChanged(CombatState.CurrentHealth, CombatState.MaxHealth);
    }

    public bool CanReceiveDamageFrom(ArenaCombatant source)
    {
        if (IsDead)
            return false;

        if (source == null || source.Team == ArenaCombatTeam.Neutral || Team == ArenaCombatTeam.Neutral)
            return true;

        return source.Team != Team;
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

    protected virtual void OnCombatStateHealthChanged(int currentHealth, int maxHealth)
    {
    }

    protected virtual void OnCombatStateDied()
    {
    }

    protected void MoveWithDirection(Vector2 direction)
    {
        Velocity = direction.Normalized() * MoveSpeed + GetSoftCollisionVelocity();
        SetLookDirectionFromInput(direction);
        MoveAndSlide();
    }

    protected void MoveWithInputState(ArenaCombatInputState inputState)
    {
        var desiredVelocity = inputState?.IsMoving == true
            ? inputState.MoveDirection * inputState.MoveStrength * MoveSpeed
            : Vector2.Zero;

        Velocity = desiredVelocity + GetSoftCollisionVelocity();
        MoveAndSlide();
    }

    protected void MoveWithSoftCollisionOnly()
    {
        Velocity = GetSoftCollisionVelocity();
        MoveAndSlide();
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

        var xSign = (float)Mathf.Sign(sprite.Scale.X);
        if (LookDirection.X > 0f)
            xSign = 1f;
        else if (LookDirection.X < 0f)
            xSign = -1f;

        if (xSign == 0f)
            xSign = 1f;

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
        if (LookDirection.X > 0f)
            return 1f;
        if (LookDirection.X < 0f)
            return -1f;

        return 1f;
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
