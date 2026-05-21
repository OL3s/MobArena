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
    public Texture2D Icon { get; private set; }

    [Export]
    public PackedScene Scene { get; private set; }
}
