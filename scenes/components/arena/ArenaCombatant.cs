using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Arena;

public abstract partial class ArenaCombatant : CharacterBody2D
{
    [Export]
    public float MoveSpeed { get; protected set; } = 160f;

    protected void MoveWithDirection(Vector2 direction)
    {
        Velocity = direction.Normalized() * MoveSpeed;
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

    protected static void FitSpriteHeight(Sprite2D sprite, Texture2D texture, float displayHeight)
    {
        if (sprite == null)
            return;

        sprite.Texture = texture;
        if (texture != null && texture.GetHeight() > 0)
            sprite.Scale = Vector2.One * (displayHeight / texture.GetHeight());
    }
}
