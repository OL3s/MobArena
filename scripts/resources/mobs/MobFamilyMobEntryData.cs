using Godot;

namespace MobArena.Scripts.Resources.Mobs;

[GlobalClass]
public partial class MobFamilyMobEntryData : Resource
{
    [Export]
    public EnemyMobData Mob { get; private set; }

    [Export]
    public int MinimumCompanyFame { get; private set; }
}
