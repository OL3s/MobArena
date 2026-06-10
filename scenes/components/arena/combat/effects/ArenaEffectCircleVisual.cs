using Godot;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public partial class ArenaEffectCircleVisual : Node2D
{
    private float _radius = 28f;
    private Color _fillColor = new(1f, 0.78f, 0.25f, 0.22f);
    private Color _outlineColor = new(1f, 0.92f, 0.45f, 0.7f);
    private float _alphaRatio = 1f;

    public override void _Draw()
    {
        var alpha = Mathf.Clamp(_alphaRatio, 0f, 1f);
        DrawCircle(Vector2.Zero, _radius, WithAlpha(_fillColor, _fillColor.A * alpha));
        DrawArc(Vector2.Zero, _radius, 0f, Mathf.Tau, 48, WithAlpha(_outlineColor, _outlineColor.A * alpha), 2f);
    }

    public void Configure(float radius, Color? fillColor = null, Color? outlineColor = null)
    {
        _radius = Mathf.Max(1f, radius);
        if (fillColor.HasValue)
            _fillColor = fillColor.Value;
        if (outlineColor.HasValue)
            _outlineColor = outlineColor.Value;

        QueueRedraw();
    }

    public void SetAlphaRatio(float alphaRatio)
    {
        _alphaRatio = Mathf.Clamp(alphaRatio, 0f, 1f);
        QueueRedraw();
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = Mathf.Clamp(alpha, 0f, 1f);
        return color;
    }
}
