using Godot;

namespace MobArena.Scripts.Resources.Items;

[GlobalClass]
public abstract partial class ItemData : Resource
{
    [Export]
    public string DisplayName { get; private set; } = "Item";

    [Export(PropertyHint.MultilineText)]
    public string Description { get; private set; } = string.Empty;

    [Export]
    public Texture2D Icon { get; private set; }

    [Export]
    public int Cost { get; private set; } = 1;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float Condition { get; private set; } = 1f;

    public T CreateRuntimeCopy<T>() where T : ItemData
    {
        return Duplicate(true) as T;
    }

    public static T LoadRuntimeCopy<T>(string resourcePath) where T : ItemData
    {
        var template = ResourceLoader.Load<T>(resourcePath);
        return template?.CreateRuntimeCopy<T>();
    }
}
