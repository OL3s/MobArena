using Godot;

namespace MobArena.Scripts.Resources;

public partial class CompletedCompanyRecord : Resource
{
    [Export]
    public CompanyLogoData CompanyLogoData { get; private set; } = CompanyLogoData.CreateDefault();

    [Export]
    public CompanyCareerData CompanyCareerData { get; private set; } = new();

    [Export]
    public int FinalFame { get; private set; }

    public string CompanyName => CompanyLogoData?.CompanyName ?? "Unknown Company";

    public static CompletedCompanyRecord Create(CompanyLogoData logoData, CompanyCareerData careerData, int finalFame)
    {
        var record = new CompletedCompanyRecord();
        record.SetData(logoData, careerData, finalFame);
        return record;
    }

    public void SetData(CompanyLogoData logoData, CompanyCareerData careerData, int finalFame)
    {
        CompanyLogoData = logoData?.CreateCopy() ?? CompanyLogoData.CreateDefault();
        CompanyCareerData = careerData?.CreateCopy() ?? new CompanyCareerData();
        FinalFame = Mathf.Max(finalFame, 0);
    }
}
