using Godot;

namespace MobArena.Scenes.Components.UI;

public partial class DifficultyStars : HBoxContainer
{
    public void Configure(Texture2D starIcon, int starCount)
    {
        foreach (var child in GetChildren())
            child.QueueFree();

        for (var i = 0; i < starCount; i++)
        {
            var icon = new TextureRect
            {
                CustomMinimumSize = new Vector2(22, 22),
                Texture = starIcon,
                ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                MouseFilter = MouseFilterEnum.Ignore
            };
            AddChild(icon);
        }
    }
}
