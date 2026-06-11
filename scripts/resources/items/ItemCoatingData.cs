using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public abstract partial class ItemCoatingData : ItemData
{
    public ItemCoatingData CreateRuntimeCopy()
    {
        return CreateRuntimeCopy<ItemCoatingData>();
    }

    public static ItemCoatingData LoadRuntimeCopy(string resourcePath)
    {
        return LoadRuntimeCopy<ItemCoatingData>(resourcePath);
    }
}
