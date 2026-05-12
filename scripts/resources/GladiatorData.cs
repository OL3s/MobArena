using Godot;

namespace MobArena.Scripts.Resources;

public partial class GladiatorData : Resource
{
    private static readonly string[] PortraitPaths =
    {
        "res://assets/gladiators/gladiator_01.svg",
        "res://assets/gladiators/gladiator_02.svg",
        "res://assets/gladiators/gladiator_03.svg"
    };

    [Export]
    public string GladiatorName { get; private set; } = "Aulus";

    [Export]
    public int PortraitIndex { get; private set; }

    public static GladiatorData CreateDefault()
    {
        return new GladiatorData();
    }

    public void SetGladiatorName(string gladiatorName)
    {
        GladiatorName = string.IsNullOrWhiteSpace(gladiatorName)
            ? "Aulus"
            : gladiatorName.Trim();
    }

    public void SetPortraitIndex(int portraitIndex)
    {
        PortraitIndex = NormalizeIndex(portraitIndex, PortraitPaths.Length);
    }

    public Texture2D GetPortraitTexture()
    {
        return ResourceLoader.Load<Texture2D>(PortraitPaths[NormalizeIndex(PortraitIndex, PortraitPaths.Length)]);
    }

    private static int NormalizeIndex(int index, int count)
    {
        return ((index % count) + count) % count;
    }
}
