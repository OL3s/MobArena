using Godot;

namespace MobArena.Scripts.Resources;

public abstract partial class CharacterAppearanceData : Resource
{
    [Export]
    public string DisplayName { get; private set; } = "Character Appearance";

    [Export]
    public Texture2D UiIcon { get; private set; }

    [Export]
    public Texture2D BodyForward { get; private set; }

    [Export]
    public Texture2D BodyBack { get; private set; }

    [Export]
    public bool UsesSeparatedHands { get; private set; }

    [Export]
    public Texture2D HandTexture { get; private set; }
}
