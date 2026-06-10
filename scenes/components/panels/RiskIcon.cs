using Godot;

namespace MobArena.Scenes.Components.Panels;

public partial class RiskIcon : TextureRect
{
    public void Configure(Texture2D texture)
    {
        Texture = texture;
        Visible = texture != null;
    }
}
