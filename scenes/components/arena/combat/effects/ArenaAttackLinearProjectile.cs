using Godot;
using System.Collections.Generic;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public partial class ArenaAttackLinearProjectile : Area2D, IArenaCombatEffect
{
    private const uint WallCollisionMask = 1u;
    private const uint CombatantCollisionMask = 2u;

    private readonly HashSet<ulong> _hitTargets = new();

    private ArenaCombatEffectContext _context;
    private ArenaAttackLinearProjectileData _effectData;
    private CollisionShape2D _collisionShape;
    private ArenaAttackVisual _visual;
    private Sprite2D _shadow;
    private Vector2 _direction = Vector2.Right;
    private float _distanceTraveled;
    private int _penetrationsUsed;
    private bool _destroyed;

    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _visual = GetNodeOrNull<ArenaAttackVisual>("Visual");
        _shadow = GetNodeOrNull<Sprite2D>("Shadow");
        CollisionLayer = 0;
        CollisionMask = WallCollisionMask | CombatantCollisionMask;
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_context == null || _effectData == null || _destroyed)
            return;

        ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
        var movement = _direction * Mathf.Max(1f, _context.ScaleSpeed(_effectData.Speed)) * (float)delta;
        GlobalPosition += movement;
        _distanceTraveled += movement.Length();

        if (_distanceTraveled >= Mathf.Max(1f, _context.ScaleRange(_effectData.Range)))
            DestroyProjectile(true);
    }

    public void Initialize(ArenaCombatEffectContext context)
    {
        _context = context;
        _effectData = context?.Effect as ArenaAttackLinearProjectileData;
        if (_context == null || _effectData == null)
        {
            GD.PushError("Arena linear projectile initialization failed: missing linear projectile effect data.");
            QueueFree();
            return;
        }

        _direction = _context.Direction == Vector2.Zero ? Vector2.Right : _context.Direction.Normalized();
        GlobalRotation = _direction.Angle();
        ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
        ConfigureShape();
        ConfigureVisual();
        ConfigureShadow();
        CallDeferred(MethodName.ApplyInitialOverlaps);
    }

    private void ConfigureShape()
    {
        if (_collisionShape == null)
            _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collisionShape == null)
            return;

        var rectangle = _collisionShape.Shape as RectangleShape2D ?? new RectangleShape2D();
        rectangle.Size = new Vector2(Mathf.Max(1f, _effectData.HitboxLength), Mathf.Max(1f, _effectData.HitboxWidth));
        _collisionShape.Shape = rectangle;
        _collisionShape.Position = new Vector2(rectangle.Size.X * 0.5f, 0f);
    }

    private void ConfigureVisual()
    {
        if (_visual == null)
            _visual = GetNodeOrNull<ArenaAttackVisual>("Visual");
        if (_visual == null)
            return;

        if (_effectData.VisualTexture != null)
            _visual.ConfigureTexture(_effectData.VisualTexture, _effectData.VisualDisplayHeight);
        else
            _visual.ConfigureRectangle(_effectData.HitboxLength, _effectData.HitboxWidth);
        _visual.Position = new Vector2(0f, -Mathf.Max(0f, _effectData.VisualHeight));
    }

    private void ConfigureShadow()
    {
        if (_shadow == null)
            _shadow = GetNodeOrNull<Sprite2D>("Shadow");
        if (_shadow == null)
            return;

        _shadow.Scale = _effectData.ShadowScale;
        _shadow.Modulate = new Color(1f, 1f, 1f, Mathf.Clamp(_effectData.ShadowAlpha, 0f, 1f));
    }

    private void ApplyInitialOverlaps()
    {
        foreach (var body in GetOverlappingBodies())
            TryHit(body);
    }

    private void OnBodyEntered(Node2D body)
    {
        TryHit(body);
    }

    private void TryHit(Node body)
    {
        if (_destroyed || _effectData == null || _penetrationsUsed >= Mathf.Max(1, _effectData.MaxPenetrations))
            return;

        if (body is not ArenaCombatant target)
        {
            HandleWallHit(body as Node2D);
            return;
        }

        if (target == _context.Source)
            return;

        var targetId = target.GetInstanceId();
        if (!_effectData.CanHitSameTargetMultipleTimes && _hitTargets.Contains(targetId))
            return;

        var applied = ApplyToTarget(target);
        if (_effectData.Apply?.ResolveDamage(_context.ItemDamage) != null && applied <= 0)
            return;

        _hitTargets.Add(targetId);
        _penetrationsUsed++;
        PrintHitDebug(target, applied);
        SpawnChainedHit();

        if (_penetrationsUsed >= Mathf.Max(1, _effectData.MaxPenetrations))
            DestroyProjectile(false);
    }

    private void HandleWallHit(Node2D wall)
    {
        if (wall == null)
            return;

        if (!_effectData.BounceOffWalls)
        {
            DestroyProjectile(true);
            return;
        }

        var normal = GetWallNormal(wall);
        _direction = _direction.Bounce(normal).Normalized();
        GlobalRotation = _direction.Angle();
        GlobalPosition += _direction * 4f;
        GameLogger.Combat($"Combat bounce: LinearProjectile action={_context.ActionName}, wall={wall.Name}.");
    }

    private Vector2 GetWallNormal(Node2D wall)
    {
        var delta = GlobalPosition - wall.GlobalPosition;
        if (Mathf.Abs(delta.X) > Mathf.Abs(delta.Y))
            return delta.X >= 0f ? Vector2.Right : Vector2.Left;

        return delta.Y >= 0f ? Vector2.Down : Vector2.Up;
    }

    private int ApplyToTarget(ArenaCombatant target)
    {
        var damage = _context.ScaleDamage(_effectData.Apply?.ResolveDamage(_context.ItemDamage));
        var applied = damage == null
            ? target.ApplyRawDamage(0, _context.Source)
            : target.ApplyDamage(damage, _context.Source);

        var force = _effectData.Apply?.GetForce(_direction);
        if (force.HasValue)
            target.AddExternalForce(force.Value);

        _effectData.Apply?.ApplyStatus(target, _context.Source, applied);
        return applied;
    }

    private void PrintHitDebug(ArenaCombatant target, int appliedDamage)
    {
        var targetHealth = target?.CombatState == null
            ? "unknown HP"
            : $"{target.CombatState.CurrentHealth}/{target.CombatState.MaxHealth} HP";
        GameLogger.Combat($"Combat hit: LinearProjectile -> {target?.Name ?? "UnknownTarget"}, action={_context.ActionName}, damage={appliedDamage}, target={targetHealth}.");
    }

    private void SpawnChainedHit()
    {
        ArenaCombatEffectSpawner.TrySpawn(GetParent(), GlobalPosition, GlobalRotation, _context, _effectData.OnHitEffect);
        ArenaCombatEffectSpawner.TrySpawnScene(GetParent(), GlobalPosition, GlobalRotation, _effectData.OnHitScene);
    }

    private void DestroyProjectile(bool expired)
    {
        if (_destroyed)
            return;

        _destroyed = true;
        if (expired)
        {
            GameLogger.Combat($"Combat expire: LinearProjectile action={_context.ActionName}, distance={_distanceTraveled:0.#}/{_context.ScaleRange(_effectData.Range):0.#}.");
            ArenaCombatEffectSpawner.TrySpawn(GetParent(), GlobalPosition, GlobalRotation, _context, _effectData.OnExpireEffect);
            ArenaCombatEffectSpawner.TrySpawnScene(GetParent(), GlobalPosition, GlobalRotation, _effectData.OnExpireScene);
        }

        QueueFree();
    }
}
