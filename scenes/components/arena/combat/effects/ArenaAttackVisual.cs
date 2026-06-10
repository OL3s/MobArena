using Godot;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public partial class ArenaAttackVisual : Node2D
{
    private enum VisualShape
    {
        Circle,
        Rectangle
    }

    private VisualShape _shape = VisualShape.Rectangle;
    private Texture2D _texture;
    private float _textureDisplayHeight = 18f;
    private Vector2 _size = new(42f, 12f);
    private float _radius = 8f;
    private Color _fillColor = new(0.95f, 0.86f, 0.48f, 0.8f);
    private Color _outlineColor = new(1f, 0.96f, 0.68f, 1f);
    private float _alphaRatio = 1f;

    public override void _Draw()
    {
        var fill = WithAlpha(_fillColor, _fillColor.A * _alphaRatio);
        var outline = WithAlpha(_outlineColor, _outlineColor.A * _alphaRatio);

        if (_texture != null)
        {
            DrawConfiguredTexture();
            return;
        }

        if (_shape == VisualShape.Circle)
        {
            DrawCircle(Vector2.Zero, _radius, fill);
            DrawArc(Vector2.Zero, _radius, 0f, Mathf.Tau, 24, outline, 2f);
            return;
        }

        var rect = new Rect2(new Vector2(0f, -_size.Y * 0.5f), _size);
        DrawRect(rect, fill);
        DrawRect(rect, outline, false, 2f);
    }

    public void ConfigureRectangle(float length, float width, Color? fillColor = null, Color? outlineColor = null)
    {
        _texture = null;
        _shape = VisualShape.Rectangle;
        _size = new Vector2(Mathf.Max(1f, length), Mathf.Max(1f, width));
        ApplyColors(fillColor, outlineColor);
    }

    public void ConfigureCircle(float radius, Color? fillColor = null, Color? outlineColor = null)
    {
        _texture = null;
        _shape = VisualShape.Circle;
        _radius = Mathf.Max(1f, radius);
        ApplyColors(fillColor, outlineColor);
    }

    public void ConfigureTexture(Texture2D texture, float displayHeight)
    {
        _texture = texture;
        _textureDisplayHeight = Mathf.Max(1f, displayHeight);
        QueueRedraw();
    }

    public void SetAlphaRatio(float alphaRatio)
    {
        _alphaRatio = Mathf.Clamp(alphaRatio, 0f, 1f);
        QueueRedraw();
    }

    private void ApplyColors(Color? fillColor, Color? outlineColor)
    {
        if (fillColor.HasValue)
            _fillColor = fillColor.Value;
        if (outlineColor.HasValue)
            _outlineColor = outlineColor.Value;

        QueueRedraw();
    }

    private void DrawConfiguredTexture()
    {
        var size = _texture.GetSize();
        if (size.Y <= 0f)
            return;

        var scale = _textureDisplayHeight / size.Y;
        var drawSize = size * scale;
        var rect = new Rect2(new Vector2(0f, -drawSize.Y * 0.5f), drawSize);
        DrawTextureRect(_texture, rect, false, WithAlpha(Colors.White, _alphaRatio));
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.A = Mathf.Clamp(alpha, 0f, 1f);
        return color;
    }
}
