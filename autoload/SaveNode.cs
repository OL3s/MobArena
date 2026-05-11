using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class SaveNode : Node
{
    [Signal]
    public delegate void CompanyChangedEventHandler();

    [Export]
    public bool HasCompany { get; private set; }

    [Export]
    public CompanyLogoData CompanyLogoData { get; private set; } = CompanyLogoData.CreateDefault();

    [Export]
    public TownTimeState TownTimeState { get; private set; } = new();

    public static SaveNode Get()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        return sceneTree?.Root?.GetNodeOrNull<SaveNode>("/root/SaveNode");
    }

    public CompanyLogoData CreateEditableCompanyData()
    {
        return CompanyLogoData.CreateCopy();
    }

    public void ApplyCompanyData(CompanyLogoData companyLogoData)
    {
        CompanyLogoData.CopyFrom(companyLogoData);
        HasCompany = true;
        EmitSignal(SignalName.CompanyChanged);
    }
}
