using Godot;

namespace MobArena.Scripts.Resources;

public partial class GladiatorData : Resource
{
    private static readonly string[] DefaultNames =
    {
        "Aulus",
        "Cassia",
        "Drusus",
        "Livia",
        "Maro",
        "Sabina"
    };

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

    [Export]
    public int Health { get; private set; } = 30;

    public int MaxHealth => Level.GetMaxHealth();

    [Export]
    public int Stamina { get; private set; } = 20;

    public int MaxStamina => Level.GetMaxStamina();

    [Export]
    public GladiatorLevelData Level { get; private set; } = new();

    [Export]
    public GladiatorEquipmentData Equipment { get; private set; } = new();

    [Export]
    public GladiatorCareerData GladiatorCareer { get; private set; } = new();

    [Export]
    public int InitialCost { get; private set; } = 25;

    public static GladiatorData CreateDefault()
    {
        var random = new RandomNumberGenerator();
        random.Randomize();

        var level = GladiatorLevelData.CreateDefault(random);

        return new GladiatorData
        {
            GladiatorName = DefaultNames[random.RandiRange(0, DefaultNames.Length - 1)],
            PortraitIndex = random.RandiRange(0, PortraitPaths.Length - 1),
            Level = level,
            Health = level.GetMaxHealth(),
            Stamina = level.GetMaxStamina(),
            Equipment = GladiatorEquipmentData.CreateDefault(random),
            GladiatorCareer = new GladiatorCareerData(),
            InitialCost = random.RandiRange(20, 45)
        };
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
