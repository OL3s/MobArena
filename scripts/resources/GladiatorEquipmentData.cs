using Godot;

namespace MobArena.Scripts.Resources;

public partial class GladiatorEquipmentData : Resource
{
    public enum SignatureSkill
    {
        Dodge,
        Parry,
        Bash,
        Cleave
    }

    [Export]
    public string Armor { get; private set; } = "Cloth Wraps";

    [Export]
    public string ItemMain { get; private set; } = "Training Sword";

    [Export]
    public string ItemSecond { get; private set; } = "Wooden Buckler";

    [Export]
    public SignatureSkill Skill { get; private set; } = SignatureSkill.Dodge;

    public static GladiatorEquipmentData CreateDefault(RandomNumberGenerator random)
    {
        return new GladiatorEquipmentData
        {
            Skill = (SignatureSkill)random.RandiRange(0, (int)SignatureSkill.Cleave)
        };
    }
}
