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
    private float _remainingLifetime;
    private float _remainingActiveSeconds;
    private int _hitsApplied;

    public override void _Ready()
    {
        _collisionShape = GetNode<CollisionShape2D>("CollisionShape2D");
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

        if (_remainingActiveSeconds <= 0f)
            Monitoring = false;

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

        _remainingLifetime = Mathf.Max(0.01f, _effectData.LifetimeSeconds);
        _remainingActiveSeconds = Mathf.Max(0.01f, _effectData.ActiveSeconds);
        GlobalPosition += _context.Direction * _effectData.ForwardOffset;
        ConfigureShape();
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
        SpawnChainedScene(_effectData.OnHitScene);

        if (_hitsApplied >= _effectData.MaxHits)
            Monitoring = false;
    }

    private CombatDamageData ResolveDamage()
    {
        if (_effectData.UseSourceItemDamage && _context.ItemDamage != null)
            return _context.ItemDamage;

        return _effectData.Damage;
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
