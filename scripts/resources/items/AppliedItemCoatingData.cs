using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public partial class AppliedItemCoatingData : Resource
{
    [Export]
    public ItemCoatingData Coating { get; private set; }

    [Export]
    public int MaxDurability { get; private set; } = 100;

    [Export]
    public int Durability { get; private set; } = 100;

    public string DisplayName => Coating?.DisplayName ?? "No coating";

    public bool IsActive => Coating != null && Durability > 0 && MaxDurability > 0;

    public float GetCondition()
    {
        return MaxDurability <= 0 ? 0f : Mathf.Clamp((float)Durability / MaxDurability, 0f, 1f);
    }

    public static AppliedItemCoatingData Create(ItemCoatingData coating)
    {
        if (coating == null)
            return null;

        return new AppliedItemCoatingData
        {
            Coating = coating.CreateRuntimeCopy(),
            MaxDurability = Mathf.Max(0, coating.MaxDurability),
            Durability = Mathf.Clamp(coating.Durability, 0, Mathf.Max(0, coating.MaxDurability))
        };
    }
}
