using Godot;

namespace MobArena.Scripts.Utils;

public static class MotionMath
{
    public static float LerpFloat(float from, float to, float weight)
    {
        return Mathf.Lerp(from, to, Mathf.Clamp(weight, 0f, 1f));
    }

    public static float SinFloat(double time, float speed = 1f, float amplitude = 1f, float phase = 0f)
    {
        return Mathf.Sin((float)time * speed + phase) * amplitude;
    }

    public static float BounceFloat(double time, float speed = 1f, float amplitude = 1f, float phase = 0f)
    {
        return Mathf.Abs(SinFloat(time, speed, amplitude, phase));
    }

}
