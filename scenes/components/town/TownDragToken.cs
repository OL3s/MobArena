using Godot;

namespace MobArena.Scenes.Components.Town;

public partial class TownDragToken : Sprite2D
{
    public void Configure(Texture2D texture, float targetHeight)
    {
        Texture = texture;
        Centered = true;
        Modulate = new Color(1f, 1f, 1f, 0.82f);
        if (texture != null && texture.GetHeight() > 0)
            Scale = Vector2.One * (targetHeight / texture.GetHeight());
    }
}
