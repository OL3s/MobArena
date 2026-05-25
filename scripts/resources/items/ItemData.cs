using Godot;
using System.Collections.Generic;

namespace MobArena.Scripts.Resources.Items;

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
    public Texture2D HeldTexture { get; private set; }

    [Export]
    public int Cost { get; private set; } = 1;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Condition { get; private set; } = 1f;

    public Texture2D GetHeldTexture()
    {
        return HeldTexture ?? UiIcon;
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
