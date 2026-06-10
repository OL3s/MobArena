using Godot;
using MobArena.Scripts.Resources.Combat;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class ArmorItemData : ItemData
{
    [Export]
    public ArmorData ArmorProfile { get; private set; }

    [Export]
    public Texture2D ArmorForwardTexture { get; private set; }

    [Export]
    public Texture2D ArmorBackTexture { get; private set; }

    [Export]
    public float ArmorDisplayHeight { get; private set; } = 96f;

    [Export]
    public Vector2 ArmorTextureOffset { get; private set; } = Vector2.Zero;

    public float GetArmorDisplayHeight(float fallbackDisplayHeight = 96f)
    {
        return ArmorDisplayHeight > 0f ? ArmorDisplayHeight : fallbackDisplayHeight;
    }

    public Vector2 GetArmorTextureOffset()
    {
        return ArmorTextureOffset;
    }

    public void SetArmorVisualTuning(float displayHeight, Vector2 textureOffset)
    {
        ArmorDisplayHeight = Mathf.Max(1f, displayHeight);
        ArmorTextureOffset = textureOffset;
    }

    public int GetArmorValue(CombatDamageType type)
    {
        return ArmorProfile?.GetArmorValue(type) ?? 0;
    }

    public int ApplyArmorToDamage(int damage, CombatDamageType type)
    {
        return ArmorProfile?.ApplyArmorToDamage(damage, type) ?? ArmorData.ApplyArmorToDamage(damage, 0);
    }

    public int ApplyArmorToDamage(CombatDamageEntryData damageEntry)
    {
        return ArmorProfile?.ApplyArmorToDamage(damageEntry) ?? damageEntry?.GetRawDamage() ?? 0;
    }

    public int ApplyArmorToDamage(CombatDamageData damageData)
    {
        return ArmorProfile?.ApplyArmorToDamage(damageData) ?? damageData?.GetRawTotalDamage() ?? 0;
    }

    public static int ApplyArmorToDamage(int damage, int armor)
    {
        return ArmorData.ApplyArmorToDamage(damage, armor);
    }
}
