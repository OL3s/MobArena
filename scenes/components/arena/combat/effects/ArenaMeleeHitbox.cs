using Godot;
using System.Collections.Generic;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public partial class ArenaMeleeHitbox : Area2D, IArenaCombatEffect
{
    private const uint CombatantCollisionMask = 2u;

    private readonly HashSet<ulong> _hitTargets = new();

    private ArenaCombatEffectContext _context;
    private ArenaMeleeEffectData _effectData;
    private CollisionShape2D _collisionShape;
    private ArenaEffectCircleVisual _circleVisual;
    private float _remainingLifetime;
    private float _totalLifetime;
    private float _remainingActiveSeconds;
    private int _hitsApplied;

    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
        _circleVisual = GetNodeOrNull<ArenaEffectCircleVisual>("CircleVisual");
        CollisionLayer = 0;
        CollisionMask = CombatantCollisionMask;
        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_context == null || _effectData == null)
            return;

        var deltaSeconds = (float)delta;
        _remainingLifetime -= deltaSeconds;
        _remainingActiveSeconds -= deltaSeconds;
        RefreshVisualFade();

        if (_remainingActiveSeconds <= 0f)
            SetMonitoringDeferred(false);

        if (_remainingLifetime <= 0f)
        {
            SpawnChainedScene(_effectData.OnExpireScene);
            QueueFree();
        }
    }

    public void Initialize(ArenaCombatEffectContext context)
    {
        _context = context;
        _effectData = context?.Effect as ArenaMeleeEffectData;
        if (_context == null || _effectData == null)
        {
            GD.PushError("Arena melee hitbox initialization failed: missing melee effect data.");
            QueueFree();
            return;
        }

        _totalLifetime = Mathf.Max(0.01f, _effectData.LifetimeSeconds);
        _remainingLifetime = _totalLifetime;
        _remainingActiveSeconds = Mathf.Max(0.01f, _effectData.ActiveSeconds);
        GlobalPosition += _context.Direction * _effectData.ForwardOffset;
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
        circle.Radius = Mathf.Max(1f, _effectData.HitboxRadius);
        _collisionShape.Shape = circle;
    }

    private void ConfigureVisual()
    {
        if (_circleVisual == null)
            _circleVisual = GetNodeOrNull<ArenaEffectCircleVisual>("CircleVisual");

        if (_circleVisual == null)
            return;

        _circleVisual.Configure(Mathf.Max(1f, _effectData.HitboxRadius));
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
            TryHit(body);
    }

    private void OnBodyEntered(Node2D body)
    {
        TryHit(body);
    }

    private void TryHit(Node body)
    {
        if (_effectData == null || _remainingActiveSeconds <= 0f || _hitsApplied >= _effectData.MaxHits)
            return;

        if (body is not ArenaCombatant target || target == _context.Source)
            return;

        var targetId = target.GetInstanceId();
        if (!_effectData.CanHitSameTargetMultipleTimes && _hitTargets.Contains(targetId))
            return;

        var damage = ResolveDamage();
        var applied = damage == null
            ? target.ApplyRawDamage(0, _context.Source)
            : target.ApplyDamage(damage, _context.Source);

        if (damage != null && applied <= 0)
            return;

        _hitTargets.Add(targetId);
        _hitsApplied++;
        ApplyHitForce(target);
        ApplyStatus(target, applied);
        PrintHitDebug(target, applied);
        SpawnChainedScene(_effectData.OnHitScene);

        if (_hitsApplied >= _effectData.MaxHits)
            SetMonitoringDeferred(false);
    }

    private void SetMonitoringDeferred(bool monitoring)
    {
        SetDeferred(Area2D.PropertyName.Monitoring, monitoring);
    }

    private CombatDamageData ResolveDamage()
    {
        return _effectData.Apply?.ResolveDamage(_context.ItemDamage);
    }

    private void ApplyHitForce(ArenaCombatant target)
    {
        var force = _effectData.Apply?.GetForce(_context.Direction);
        if (force.HasValue)
            target.AddExternalForce(force.Value);
    }

    private void ApplyStatus(ArenaCombatant target, int appliedDamage)
    {
        _effectData.Apply?.ApplyStatus(target, _context.Source, appliedDamage);
    }

    private void PrintHitDebug(ArenaCombatant target, int appliedDamage)
    {
        var sourceName = _context.Source?.Name ?? "UnknownSource";
        var targetName = target?.Name ?? "UnknownTarget";
        var actionName = string.IsNullOrWhiteSpace(_context.Action?.DisplayName)
            ? "UnnamedAction"
            : _context.Action.DisplayName;
        var targetHealth = target?.CombatState == null
            ? "unknown HP"
            : $"{target.CombatState.CurrentHealth}/{target.CombatState.MaxHealth} HP";

        var applyLabel = _effectData.Apply?.ToString() ?? "Apply=None";
        GD.Print($"Combat hit: {sourceName} -> {targetName}, action={actionName}, {applyLabel}, damage={appliedDamage}, target={targetHealth}, hits={_hitsApplied}/{_effectData.MaxHits}.");
    }

    private void SpawnChainedScene(PackedScene scene)
    {
        if (scene == null)
            return;

        var instance = scene.Instantiate<Node2D>();
        GetParent()?.AddChild(instance);
        instance.GlobalPosition = GlobalPosition;
    }
}
