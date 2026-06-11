using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public abstract partial class EquipmentItemData : ItemData
{
    [Export]
    public Texture2D HeldTexture { get; private set; }

    [Export]
    public float HeldDisplayHeight { get; private set; } = 48f;

    [Export]
    public float HeldRotationDegrees { get; private set; }

    [Export]
    public Vector2 HeldTextureOffset { get; private set; } = Vector2.Zero;

    [Export]
    public int Weight { get; private set; }

    [Export]
    public ItemRequirementData Requirements { get; private set; }

    [Export]
    public ItemLevelMultiplierData LevelMultiplier { get; private set; }

    [Export]
    public AppliedItemCoatingData AppliedCoating { get; private set; }

    public bool HasActiveCoating => AppliedCoating?.IsActive == true;

    public Texture2D GetHeldTexture()
    {
        return HeldTexture ?? UiIcon;
    }

    public float GetHeldDisplayHeight(float fallbackDisplayHeight = 48f)
    {
        return HeldDisplayHeight > 0f ? HeldDisplayHeight : fallbackDisplayHeight;
    }

    public float GetHeldRotationDegrees()
    {
        return HeldRotationDegrees;
    }

    public Vector2 GetHeldTextureOffset()
    {
        return HeldTextureOffset;
    }

    public void SetHeldVisualTuning(float displayHeight, float rotationDegrees, Vector2 textureOffset)
    {
        HeldDisplayHeight = Mathf.Max(1f, displayHeight);
        HeldRotationDegrees = rotationDegrees;
        HeldTextureOffset = textureOffset;
    }

    public void ApplyCoating(ItemCoatingData coating)
    {
        AppliedCoating = AppliedItemCoatingData.Create(coating);
    }

    public void ClearCoating()
    {
        AppliedCoating = null;
    }
}
