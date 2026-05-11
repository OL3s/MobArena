using Godot;

namespace MobArena.Scripts.Resources;

public partial class CompanyLogoData : Resource
{
    private static readonly string[] ShieldNames = { "Scutum", "Round", "Tower" };
    private static readonly string[] ShieldPaths =
    {
        "res://assets/ui/company_shields/scutum.svg",
        "res://assets/ui/company_shields/round.svg",
        "res://assets/ui/company_shields/tower.svg"
    };

    private static readonly string[] LogoNames = { "Cross", "Sword", "Laurel" };
    private static readonly string[] LogoPaths =
    {
        "res://assets/ui/company_logos/cross.svg",
        "res://assets/ui/company_logos/sword.svg",
        "res://assets/ui/company_logos/laurel.svg"
    };

    [Signal]
    public delegate void LogoChangedEventHandler();

    [Export]
    public int ShieldIndex { get; private set; }

    [Export]
    public int LogoIndex { get; private set; }

    [Export]
    public string CompanyName { get; private set; } = "The Bronze Lions";

    public static CompanyLogoData CreateDefault()
    {
        return new CompanyLogoData();
    }

    public int GetShieldCount()
    {
        return ShieldPaths.Length;
    }

    public int GetLogoCount()
    {
        return LogoPaths.Length;
    }

    public string GetShieldName(int index)
    {
        return ShieldNames[NormalizeIndex(index, ShieldNames.Length)];
    }

    public string GetLogoName(int index)
    {
        return LogoNames[NormalizeIndex(index, LogoNames.Length)];
    }

    public Texture2D GetShieldTexture()
    {
        return ResourceLoader.Load<Texture2D>(ShieldPaths[NormalizeIndex(ShieldIndex, ShieldPaths.Length)]);
    }

    public Texture2D GetLogoTexture()
    {
        return ResourceLoader.Load<Texture2D>(LogoPaths[NormalizeIndex(LogoIndex, LogoPaths.Length)]);
    }

    public void SetShieldIndex(int index)
    {
        ShieldIndex = NormalizeIndex(index, ShieldPaths.Length);
        EmitSignal(SignalName.LogoChanged);
    }

    public void SetLogoIndex(int index)
    {
        LogoIndex = NormalizeIndex(index, LogoPaths.Length);
        EmitSignal(SignalName.LogoChanged);
    }

    public void SetCompanyName(string companyName)
    {
        CompanyName = string.IsNullOrWhiteSpace(companyName)
            ? "The Bronze Lions"
            : companyName.Trim();
        EmitSignal(SignalName.LogoChanged);
    }

    public CompanyLogoData CreateCopy()
    {
        var copy = CreateDefault();
        copy.SetShieldIndex(ShieldIndex);
        copy.SetLogoIndex(LogoIndex);
        copy.SetCompanyName(CompanyName);
        return copy;
    }

    public void CopyFrom(CompanyLogoData other)
    {
        if (other == null)
            return;

        ShieldIndex = NormalizeIndex(other.ShieldIndex, ShieldPaths.Length);
        LogoIndex = NormalizeIndex(other.LogoIndex, LogoPaths.Length);
        CompanyName = string.IsNullOrWhiteSpace(other.CompanyName)
            ? "The Bronze Lions"
            : other.CompanyName.Trim();
        EmitSignal(SignalName.LogoChanged);
    }

    public void ApplyTo(TextureRect shield, TextureRect logo)
    {
        shield.Texture = GetShieldTexture();
        logo.Texture = GetLogoTexture();
    }

    private static int NormalizeIndex(int index, int count)
    {
        return ((index % count) + count) % count;
    }
}
