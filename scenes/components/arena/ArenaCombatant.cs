using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Arena;

public abstract partial class ArenaCombatant : CharacterBody2D
{
    [Export]
    public float MoveSpeed { get; protected set; } = 160f;

    [Export]
    public Vector2 LookDirection { get; private set; } = Vector2.Zero;

    protected void ConfigureTopDownMotion()
    {
        MotionMode = MotionModeEnum.Floating;
    }

    protected void MoveWithDirection(Vector2 direction)
    {
        Velocity = direction.Normalized() * MoveSpeed;
        SetLookDirectionFromInput(direction);
        MoveAndSlide();
    }

    protected void MoveWithInputState(ArenaCombatInputState inputState)
    {
        if (inputState?.IsMoving != true)
        {
            Velocity = Vector2.Zero;
            MoveAndSlide();
            return;
        }

        Velocity = inputState.MoveDirection * inputState.MoveStrength * MoveSpeed;
        MoveAndSlide();
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
        LookDirection = lookDirection;
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

    protected static void FitSpriteHeight(Sprite2D sprite, Texture2D texture, float displayHeight)
    {
        if (sprite == null)
            return;

        sprite.Texture = texture;
        if (texture != null && texture.GetHeight() > 0)
            sprite.Scale = Vector2.One * (displayHeight / texture.GetHeight());
    }
}
