using Godot;
using MobArena.Scripts.Resources.Combat.Effects;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public partial class ArenaAttackThrownProjectile : Node2D, IArenaCombatEffect
{
    private ArenaCombatEffectContext _context;
    private ArenaAttackThrownProjectileData _effectData;
    private ArenaAttackVisual _visual;
    private Sprite2D _shadow;
    private Vector2 _startPosition;
    private Vector2 _targetPosition;
    private float _elapsed;
    private bool _landed;

    public override void _Ready()
    {
        _visual = GetNodeOrNull<ArenaAttackVisual>("Visual");
        _shadow = GetNodeOrNull<Sprite2D>("Shadow");
    }

    public override void _Process(double delta)
    {
        if (_context == null || _effectData == null || _landed)
            return;

        ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
        _elapsed += (float)delta;
        var travelSeconds = Mathf.Max(0.05f, _effectData.TravelSeconds);
        var progress = Mathf.Clamp(_elapsed / travelSeconds, 0f, 1f);
        GlobalPosition = _startPosition.Lerp(_targetPosition, progress);

        var fakeHeight = Mathf.Sin(progress * Mathf.Pi) * Mathf.Max(0f, _effectData.ArcHeight);
        if (_visual != null)
            _visual.Position = new Vector2(0f, -fakeHeight);
        RefreshShadow(fakeHeight);

        if (progress >= 1f)
            Land();
    }

    public void Initialize(ArenaCombatEffectContext context)
    {
        _context = context;
        _effectData = context?.Effect as ArenaAttackThrownProjectileData;
        if (_context == null || _effectData == null)
        {
            GD.PushError("Arena thrown projectile initialization failed: missing thrown projectile effect data.");
            QueueFree();
            return;
        }

        var direction = _context.Direction == Vector2.Zero ? Vector2.Right : _context.Direction.Normalized();
        _startPosition = GlobalPosition;
        _targetPosition = _startPosition + direction * Mathf.Max(1f, _context.ScaleRange(_effectData.Range));
        GlobalRotation = direction.Angle();
        ZIndex = Mathf.RoundToInt(GlobalPosition.Y);
        ConfigureVisual();
        RefreshShadow(0f);
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
            _visual.ConfigureCircle(8f, new Color(0.55f, 0.78f, 1f, 0.8f), new Color(0.8f, 0.92f, 1f, 1f));
    }

    private void RefreshShadow(float fakeHeight)
    {
        if (_shadow == null)
            _shadow = GetNodeOrNull<Sprite2D>("Shadow");
        if (_shadow == null)
            return;

        var heightRatio = Mathf.Clamp(fakeHeight / Mathf.Max(1f, _effectData?.ArcHeight ?? 1f), 0f, 1f);
        _shadow.Scale = _effectData.GroundShadowScale.Lerp(_effectData.ApexShadowScale, heightRatio);
        _shadow.Modulate = new Color(1f, 1f, 1f, Mathf.Lerp(_effectData.GroundShadowAlpha, _effectData.ApexShadowAlpha, heightRatio));
    }

    private void Land()
    {
        if (_landed)
            return;

        _landed = true;
        GD.Print($"Combat land: ThrownProjectile action={_context.ActionName}, position={GlobalPosition}, range={_context.ScaleRange(_effectData.Range):0.#}, buildup={_context.BuildupScalar:0.##}, chain={_context.ChainDepth}/{_context.MaxChainDepth}.");
        ArenaCombatEffectSpawner.TrySpawn(GetParent(), GlobalPosition, GlobalRotation, _context, _effectData.OnExpireEffect);
        ArenaCombatEffectSpawner.TrySpawnScene(GetParent(), GlobalPosition, GlobalRotation, _effectData.OnExpireScene);
        QueueFree();
    }
}
