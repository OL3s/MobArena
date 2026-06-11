using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class ItemRequirementData : Resource
{
    [Export]
    public int RequiredStrength { get; private set; }

    [Export]
    public int RequiredAgility { get; private set; }

    public bool IsMetBy(GladiatorLevelData levels)
    {
        if (levels == null)
            return RequiredStrength <= 0 && RequiredAgility <= 0;

        return levels.Strength >= RequiredStrength
            && levels.Agility >= RequiredAgility;
    }
}
