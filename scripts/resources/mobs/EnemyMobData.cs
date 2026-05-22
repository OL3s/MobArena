using Godot;

namespace MobArena.Scripts.Resources.Mobs;

[GlobalClass]
public partial class EnemyMobData : MobData
{
    [Export]
    public MobFamily Family { get; private set; } = MobFamily.Slimes;

    [Export]
    public int MaxHealth { get; private set; } = 10;

    [Export]
    public int FameValue { get; private set; } = 1;
}
