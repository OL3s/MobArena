using Godot;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.UI;

public partial class TimelineLine : Control
{
    [Export]
    public double MaxValue { get; set; } = 1.0;

    [Export]
    public double Value { get; set; }

    [Export]
    public Color TrackColor { get; set; } = new(0.85f, 0.8f, 0.7f, 0.16f);

    [Export]
    public Color FillColor { get; set; } = new(0.85f, 0.8f, 0.7f, 0.45f);

    [Export]
    public float LineHeight { get; set; } = 8.0f;

    [Export]
    public float MarkerRadius { get; set; } = 6.0f;

    private readonly List<Segment> _segments = new();

    public override void _Draw()
    {
        var rect = GetTrackRect();
        DrawRect(rect, TrackColor);

        foreach (var segment in _segments)
        {
            var start = GetNormalizedPosition(segment.Start);
            var end = GetNormalizedPosition(segment.End);
            if (end <= start)
                continue;

            DrawRect(new Rect2(rect.Position + new Vector2(rect.Size.X * start, 0), new Vector2(rect.Size.X * (end - start), rect.Size.Y)), segment.Color);
        }

        var progress = GetNormalizedPosition(Value);
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X * progress, rect.Size.Y)), FillColor);
        DrawCircle(new Vector2(rect.Position.X + rect.Size.X * progress, rect.GetCenter().Y), MarkerRadius, FillColor.Lightened(0.25f));
    }

    public void SetValue(double value, double maxValue)
    {
        Value = value;
        MaxValue = maxValue <= 0.0 ? 1.0 : maxValue;
        QueueRedraw();
    }

    public void ClearSegments()
    {
        _segments.Clear();
        QueueRedraw();
    }

    public void AddSegment(double start, double end, Color color)
    {
        _segments.Add(new Segment(start, end, color));
        QueueRedraw();
    }

    private Rect2 GetTrackRect()
    {
        var size = Size;
        var y = Mathf.Round((size.Y - LineHeight) * 0.5f);
        return new Rect2(0.0f, y, size.X, LineHeight);
    }

    private float GetNormalizedPosition(double value)
    {
        return (float)Mathf.Clamp(value / MaxValue, 0.0, 1.0);
    }

    private readonly record struct Segment(double Start, double End, Color Color);
}
