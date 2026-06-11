using Godot;

namespace MobArena.Scenes.Components.UI;

public partial class ItemActionIconStack : HBoxContainer
{
    public void AddIcon(Texture2D texture, string tooltip, float size = 22f)
    {
        AddChild(new TextureRect
        {
            Texture = texture,
            CustomMinimumSize = new Vector2(size, size),
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TooltipText = tooltip
        });
    }
}
