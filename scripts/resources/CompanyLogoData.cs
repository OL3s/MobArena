using Godot;
using System;

namespace MobArena.Scripts.Resources;

public partial class CompanyLogoData : Resource
{
    public const int MaxCompanyNameLength = 32;
    public const string DefaultCompanyName = "The Bronze Lions";

    public enum CompanyShieldColor
    {
        Red,
        Blue,
        Green,
        Yellow,
        Purple,
        Orange,
        Teal,
        Black,
        White
    }

    public enum CompanyLogoSize
    {
        Small,
        Medium,
        Large
    }

    public enum CompanyLogoIcon
    {
        Cross,
        Sword,
        Laurel,
        Axe,
        Helmet,
        Flame,
        Crown,
        Fist,
        Skull,
        Sunburst,
        Horseshoe
    }

    private static readonly string[] ShieldNames = { "Scutum", "Round", "Tower", "Kite", "Crest" };
    private static readonly string[] ShieldPaths =
    {
        "res://assets/ui/company_shields/scutum.svg",
        "res://assets/ui/company_shields/round.svg",
        "res://assets/ui/company_shields/tower.svg",
        "res://assets/ui/company_shields/kite.svg",
        "res://assets/ui/company_shields/crest.svg"
    };

    private static readonly string[] LogoSizeNames = { "Small", "Medium", "Large" };
    private static readonly float[] LogoSizeScales = { 0.42f, 0.52f, 0.64f };

    [Signal]
    public delegate void LogoChangedEventHandler();

    [Export]
    public int ShieldIndex { get; private set; }

    [Export]
    public int LogoIndex { get; private set; }

    [Export]
    public CompanyLogoIcon LogoIcon { get; private set; } = CompanyLogoIcon.Cross;

    [Export]
    public CompanyShieldColor ShieldColor { get; private set; } = CompanyShieldColor.Red;

    [Export]
    public CompanyLogoSize LogoSize { get; private set; } = CompanyLogoSize.Medium;

    [Export]
    public string CompanyName { get; private set; } = DefaultCompanyName;

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
        return Enum.GetValues<CompanyLogoIcon>().Length;
    }

    public int GetShieldColorCount()
    {
        return Enum.GetValues<CompanyShieldColor>().Length;
    }

    public int GetLogoSizeCount()
    {
        return LogoSizeNames.Length;
    }

    public string GetShieldName(int index)
    {
        return ShieldNames[NormalizeIndex(index, ShieldNames.Length)];
    }

    public string GetLogoName(int index)
    {
        return GetLogoIconAt(index).ToString();
    }

    public string GetShieldColorName(int index)
    {
        return GetShieldColorAt(index).ToString();
    }

    public string GetLogoSizeName(int index)
    {
        return LogoSizeNames[NormalizeIndex(index, LogoSizeNames.Length)];
    }

    public Texture2D GetShieldTexture()
    {
        return ResourceLoader.Load<Texture2D>(ShieldPaths[NormalizeIndex(ShieldIndex, ShieldPaths.Length)]);
    }

    public Texture2D GetLogoTexture()
    {
        return ResourceLoader.Load<Texture2D>(GetLogoPath(GetLogoIconAt(GetNormalizedLogoIndex())));
    }

    public Color GetShieldColor()
    {
        return ShieldColor switch
        {
            CompanyShieldColor.Red => new Color(0.72f, 0.30f, 0.24f),
            CompanyShieldColor.Blue => new Color(0.31f, 0.43f, 0.68f),
            CompanyShieldColor.Green => new Color(0.34f, 0.54f, 0.34f),
            CompanyShieldColor.Yellow => new Color(0.76f, 0.63f, 0.31f),
            CompanyShieldColor.Purple => new Color(0.48f, 0.36f, 0.62f),
            CompanyShieldColor.Orange => new Color(0.75f, 0.45f, 0.25f),
            CompanyShieldColor.Teal => new Color(0.30f, 0.58f, 0.58f),
            CompanyShieldColor.Black => new Color(0.22f, 0.22f, 0.24f),
            CompanyShieldColor.White => new Color(0.82f, 0.78f, 0.68f),
            _ => new Color(0.72f, 0.30f, 0.24f)
        };
    }

    public float GetLogoSizeScale()
    {
        return LogoSizeScales[NormalizeIndex((int)LogoSize, LogoSizeScales.Length)];
    }

    public void SetShieldIndex(int index)
    {
        ShieldIndex = NormalizeIndex(index, ShieldPaths.Length);
        EmitSignal(SignalName.LogoChanged);
    }

    public void SetLogoIndex(int index)
    {
        LogoIcon = GetLogoIconAt(index);
        LogoIndex = (int)LogoIcon;
        EmitSignal(SignalName.LogoChanged);
    }

    public void SetShieldColor(CompanyShieldColor color)
    {
        ShieldColor = GetShieldColorAt((int)color);
        EmitSignal(SignalName.LogoChanged);
    }

    public void SetLogoSize(CompanyLogoSize size)
    {
        LogoSize = (CompanyLogoSize)NormalizeIndex((int)size, LogoSizeNames.Length);
        EmitSignal(SignalName.LogoChanged);
    }

    public void SetCompanyName(string companyName)
    {
        if (!TrySetCompanyName(companyName, out var errorMessage))
        {
            GD.PushError(errorMessage);
            return;
        }

        EmitSignal(SignalName.LogoChanged);
    }

    public bool TrySetCompanyName(string companyName, out string errorMessage)
    {
        var normalizedName = NormalizeCompanyName(companyName);
        if (normalizedName.Length > MaxCompanyNameLength)
        {
            errorMessage = $"Company name must be {MaxCompanyNameLength} characters or fewer.";
            return false;
        }

        CompanyName = normalizedName;
        errorMessage = string.Empty;
        return true;
    }

    public static bool IsCompanyNameLengthValid(string companyName)
    {
        return NormalizeCompanyName(companyName).Length <= MaxCompanyNameLength;
    }

    public void RandomizeName()
    {
        SetCompanyName(CompanyNameGenerator.CreateRandomName());
    }

    public void RandomizeAll()
    {
        var rng = CreateRandomNumberGenerator();
        SetShieldIndex(rng.RandiRange(0, GetShieldCount() - 1));
        SetLogoIndex(rng.RandiRange(0, GetLogoCount() - 1));
        SetShieldColor(GetShieldColorAt(rng.RandiRange(0, GetShieldColorCount() - 1)));
        SetLogoSize((CompanyLogoSize)NormalizeIndex(rng.RandiRange(0, GetLogoSizeCount() - 1), GetLogoSizeCount()));
        SetCompanyName(CompanyNameGenerator.CreateRandomName(rng));
    }

    public void RandomizeVisuals()
    {
        var rng = CreateRandomNumberGenerator();
        SetShieldIndex(rng.RandiRange(0, GetShieldCount() - 1));
        SetLogoIndex(rng.RandiRange(0, GetLogoCount() - 1));
        SetShieldColor(GetShieldColorAt(rng.RandiRange(0, GetShieldColorCount() - 1)));
        SetLogoSize((CompanyLogoSize)NormalizeIndex(rng.RandiRange(0, GetLogoSizeCount() - 1), GetLogoSizeCount()));
    }

    public CompanyLogoData CreateCopy()
    {
        var copy = CreateDefault();
        copy.SetShieldIndex(ShieldIndex);
        copy.SetLogoIndex(LogoIndex);
        copy.SetShieldColor(ShieldColor);
        copy.SetLogoSize(LogoSize);
        copy.SetCompanyName(CompanyName);
        return copy;
    }

    public void CopyFrom(CompanyLogoData other)
    {
        if (other == null)
            return;

        ShieldIndex = NormalizeIndex(other.ShieldIndex, ShieldPaths.Length);
        SetLogoIndex(other.GetNormalizedLogoIndex());
        ShieldColor = GetShieldColorAt((int)other.ShieldColor);
        LogoSize = (CompanyLogoSize)NormalizeIndex((int)other.LogoSize, LogoSizeNames.Length);
        CompanyName = NormalizeCompanyName(other.CompanyName);
        EmitSignal(SignalName.LogoChanged);
    }

    private static string NormalizeCompanyName(string companyName)
    {
        return string.IsNullOrWhiteSpace(companyName)
            ? DefaultCompanyName
            : companyName.Trim();
    }

    public void ApplyTo(TextureRect shield, TextureRect logo)
    {
        shield.Texture = GetShieldTexture();
        shield.Modulate = GetShieldColor();
        logo.Texture = GetLogoTexture();
        logo.Modulate = Colors.White;
        ApplyLogoSize(logo);
    }

    private void ApplyLogoSize(TextureRect logo)
    {
        var halfSize = GetLogoSizeScale() * 0.5f;
        logo.AnchorLeft = 0.5f - halfSize;
        logo.AnchorTop = 0.5f - halfSize;
        logo.AnchorRight = 0.5f + halfSize;
        logo.AnchorBottom = 0.5f + halfSize;
    }

    private static int NormalizeIndex(int index, int count)
    {
        return ((index % count) + count) % count;
    }

    private static CompanyShieldColor GetShieldColorAt(int index)
    {
        var values = Enum.GetValues<CompanyShieldColor>();
        return values[NormalizeIndex(index, values.Length)];
    }

    public int GetNormalizedLogoIndex()
    {
        var logoIconIndex = (int)LogoIcon;
        return LogoIndex != logoIconIndex ? LogoIndex : logoIconIndex;
    }

    private static CompanyLogoIcon GetLogoIconAt(int index)
    {
        var values = Enum.GetValues<CompanyLogoIcon>();
        return values[NormalizeIndex(index, values.Length)];
    }

    private static string GetLogoPath(CompanyLogoIcon icon)
    {
        return icon switch
        {
            CompanyLogoIcon.Cross => "res://assets/ui/company_logos/cross.svg",
            CompanyLogoIcon.Sword => "res://assets/ui/company_logos/sword.svg",
            CompanyLogoIcon.Laurel => "res://assets/ui/company_logos/laurel.svg",
            CompanyLogoIcon.Axe => "res://assets/ui/company_logos/axe.svg",
            CompanyLogoIcon.Helmet => "res://assets/ui/company_logos/helmet.svg",
            CompanyLogoIcon.Flame => "res://assets/ui/company_logos/flame.svg",
            CompanyLogoIcon.Crown => "res://assets/ui/company_logos/crown.svg",
            CompanyLogoIcon.Fist => "res://assets/ui/company_logos/fist.svg",
            CompanyLogoIcon.Skull => "res://assets/ui/company_logos/skull.svg",
            CompanyLogoIcon.Sunburst => "res://assets/ui/company_logos/sunburst.svg",
            CompanyLogoIcon.Horseshoe => "res://assets/ui/company_logos/horseshoe.svg",
            _ => "res://assets/ui/icons/question_mark.svg"
        };
    }

    private static RandomNumberGenerator CreateRandomNumberGenerator()
    {
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        return rng;
    }
}
