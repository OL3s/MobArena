using Godot;

namespace MobArena.Scripts.Resources.Mobs;

[GlobalClass]
public abstract partial class MobData : Resource
{
    [Export]
    public string DisplayName { get; private set; } = "Mob";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [Export]
    public Texture2D UiIcon { get; private set; }

    [Export]
    public MobAppearanceData Appearance { get; private set; }

    [Export]
    public PackedScene Scene { get; private set; }

    public Texture2D GetUiIconTexture()
    {
        return Appearance?.UiIcon ?? UiIcon;
    }

    public Texture2D GetBodyForwardTexture()
    {
        return Appearance?.BodyForward ?? GetUiIconTexture();
    }

    public Texture2D GetBodyBackTexture()
    {
        return Appearance?.BodyBack ?? GetBodyForwardTexture();
    }

    public bool UsesSeparatedHands()
    {
        return Appearance?.UsesSeparatedHands == true;
    }

    public Texture2D GetHandTexture()
    {
        return Appearance?.HandTexture;
    }
}
