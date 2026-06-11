using Godot;
using System.Collections.Generic;

namespace MobArena.Scripts.Resources.Items;

public enum ItemTypeTag
{
    Other,
    Sword,
    Greatsword,
    Spear,
    Hammer,
    Greathammer,
    Axe,
    Greataxe,
    Bow,
    Crossbow,
    Dagger,
    Shield,
    Armor,
    Honing,
    Poison
}

public enum ItemStrengthTag
{
    Other,
    Training,
    Wood,
    Bronze,
    Iron,
    Black,
    Weak,
    Standard,
    Strong,
    Light,
    Medium,
    Heavy,
    Stone
}

[GlobalClass]
public abstract partial class ItemData : Resource
{
    private static readonly Dictionary<string, ItemData> RuntimeCopyTemplates = new();

    [Export]
    public string DisplayName { get; private set; } = "Item";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [Export]
    public Texture2D UiIcon { get; private set; }

    [Export]
    public ItemTypeTag TypeTag { get; private set; } = ItemTypeTag.Other;

    [Export]
    public ItemStrengthTag StrengthTag { get; private set; } = ItemStrengthTag.Other;

    [Export]
    public int Cost { get; private set; } = 1;

    [Export]
    public int MaxDurability { get; private set; } = 100;

    [Export]
    public int Durability { get; private set; } = 100;

    public float GetCondition()
    {
        return MaxDurability <= 0 ? 0f : Mathf.Clamp((float)Durability / MaxDurability, 0f, 1f);
    }

    public T CreateRuntimeCopy<T>() where T : ItemData
    {
        return Duplicate(true) as T;
    }

    public static T LoadRuntimeCopy<T>(string resourcePath) where T : ItemData
    {
        var template = GetRuntimeCopyTemplate(resourcePath) as T;
        return template?.CreateRuntimeCopy<T>();
    }

    private static ItemData GetRuntimeCopyTemplate(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return null;

        if (RuntimeCopyTemplates.TryGetValue(resourcePath, out var cachedTemplate) && cachedTemplate != null)
            return cachedTemplate;

        var template = ResourceLoader.Load(resourcePath) as ItemData;
        if (template != null)
            RuntimeCopyTemplates[resourcePath] = template;

        return template;
    }
}
