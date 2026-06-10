using Godot;
using System.Collections.Generic;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public partial class ArenaAttackAreaOfEffect : Area2D, IArenaCombatEffect
{
    private const uint CombatantCollisionMask = 2u;

    private readonly Dictionary<ulong, float> _targetTickCooldowns = new();
    private ArenaCombatEffectContext _context;
    private ArenaAttackAreaOfEffectData _effectData;
    private CollisionShape2D _collisionShape;
    private ArenaEffectCircleVisual _circleVisual;
    private float _remainingLifetime;
    private float _totalLifetime;
    private int _hitsApplied;

    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _circleVisual = GetNodeOrNull<ArenaEffectCircleVisual>("CircleVisual");
        CollisionLayer = 0;
        CollisionMask = CombatantCollisionMask;
        BodyEntered += OnBodyEntered;
        BodyExited += OnBodyExited;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_context == null || _effectData == null)
            return;

        var deltaSeconds = (float)delta;
        _remainingLifetime -= deltaSeconds;
        RefreshVisualFade();

        foreach (var body in GetOverlappingBodies())
        {
            if (body is not ArenaCombatant target || target == _context.Source)
                continue;

            var targetId = target.GetInstanceId();
            var cooldown = _targetTickCooldowns.GetValueOrDefault(targetId, 0f) - deltaSeconds;
            _targetTickCooldowns[targetId] = cooldown;
            if (cooldown <= 0f)
                TryHit(target);
        }

        if (_remainingLifetime <= 0f)
        {
            ArenaCombatEffectSpawner.TrySpawn(GetParent(), GlobalPosition, GlobalRotation, _context, _effectData.OnExpireEffect);
            ArenaCombatEffectSpawner.TrySpawnScene(GetParent(), GlobalPosition, GlobalRotation, _effectData.OnExpireScene);
            QueueFree();
        }
    }

    public void Initialize(ArenaCombatEffectContext context)
    {
        _context = context;
        _effectData = context?.Effect as ArenaAttackAreaOfEffectData;
        if (_context == null || _effectData == null)
        {
            GD.PushError("Arena area-of-effect initialization failed: missing area-of-effect data.");
            QueueFree();
            return;
        }

        _totalLifetime = Mathf.Max(0.01f, _effectData.LifetimeSeconds);
        _remainingLifetime = _totalLifetime;
        ConfigureShape();
        ConfigureVisual();
        CallDeferred(MethodName.ApplyInitialOverlaps);
    }

    private void ConfigureShape()
    {
        if (_collisionShape == null)
            _collisionShape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (_collisionShape == null)
            return;

        var circle = _collisionShape.Shape as CircleShape2D ?? new CircleShape2D();
        circle.Radius = Mathf.Max(1f, _effectData.Radius);
        _collisionShape.Shape = circle;
    }

    private void ConfigureVisual()
    {
        if (_circleVisual == null)
            _circleVisual = GetNodeOrNull<ArenaEffectCircleVisual>("CircleVisual");
        if (_circleVisual == null)
            return;

        _circleVisual.Configure(Mathf.Max(1f, _effectData.Radius), _effectData.FillColor, _effectData.OutlineColor);
        RefreshVisualFade();
    }

    private void RefreshVisualFade()
    {
        if (_circleVisual == null || _totalLifetime <= 0f)
            return;

        _circleVisual.SetAlphaRatio(_remainingLifetime / _totalLifetime);
    }

    private void ApplyInitialOverlaps()
    {
        foreach (var body in GetOverlappingBodies())
        {
            if (body is ArenaCombatant target)
                TryHit(target);
        }
    }

    private void OnBodyEntered(Node2D body)
    {
        if (body is ArenaCombatant target)
            TryHit(target);
    }

    private void OnBodyExited(Node2D body)
    {
        if (body is ArenaCombatant target)
            _targetTickCooldowns.Remove(target.GetInstanceId());
    }

    private void TryHit(ArenaCombatant target)
    {
        if (_effectData == null || target == null || target == _context.Source)
            return;

        if (!_effectData.UnlimitedHits && _hitsApplied >= _effectData.MaxHits)
            return;

        var applied = ApplyToTarget(target);
        if (_effectData.Apply?.ResolveDamage(_context.ItemDamage) != null && applied <= 0)
            return;

        _hitsApplied++;
        _targetTickCooldowns[target.GetInstanceId()] = Mathf.Max(0.01f, _effectData.TickSeconds);
        PrintHitDebug(target, applied);
        ArenaCombatEffectSpawner.TrySpawn(GetParent(), target.GlobalPosition, GlobalRotation, _context, _effectData.OnHitEffect);
        ArenaCombatEffectSpawner.TrySpawnScene(GetParent(), target.GlobalPosition, GlobalRotation, _effectData.OnHitScene);
    }

    private int ApplyToTarget(ArenaCombatant target)
    {
        var damage = _context.ScaleDamage(_effectData.Apply?.ResolveDamage(_context.ItemDamage));
        var applied = damage == null
            ? target.ApplyRawDamage(0, _context.Source)
            : target.ApplyDamage(damage, _context.Source);

        var force = _effectData.Apply?.GetForce(_context.Direction);
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
        GD.Print($"Combat hit: AreaOfEffect -> {target?.Name ?? "UnknownTarget"}, action={_context.ActionName}, effect={_effectData}, damage={appliedDamage}, target={targetHealth}, hits={_hitsApplied}/{_effectData.MaxHits}, tick={_effectData.TickSeconds:0.##}, chain={_context.ChainDepth}/{_context.MaxChainDepth}.");
    }
}
