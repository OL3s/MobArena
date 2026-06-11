using Godot;

namespace MobArena.Scenes.Components.UI;

public static class UiIconLoader
{
    public const string FallbackIconPath = "res://assets/ui/icons/question_mark.svg";

    public static Texture2D LoadIcon(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return LoadFallbackIcon();

        return ResourceLoader.Load<Texture2D>(path) ?? LoadFallbackIcon();
    }

    public static Texture2D LoadFallbackIcon()
    {
        return ResourceLoader.Load<Texture2D>(FallbackIconPath);
    }
}
