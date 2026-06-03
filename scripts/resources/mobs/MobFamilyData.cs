using Godot;

namespace MobArena.Scripts.Resources.Mobs;

[GlobalClass]
public abstract partial class MobFamilyData : Resource
{
    [Export]
    public string DisplayName { get; private set; } = "Mob Family";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [Export]
    public Texture2D UiIcon { get; private set; }

    [Export]
    public MobFamily Family { get; private set; } = MobFamily.Slimes;

    [Export]
    public int FameValue { get; private set; }
}
