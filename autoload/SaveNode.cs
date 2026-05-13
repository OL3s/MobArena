using Godot;
using System;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class SaveNode : Node
{
    [Export]
    public bool HasCompany { get; set; }

    [Export]
    public CompanyLogoData CompanyLogoData { get; private set; } = CompanyLogoData.CreateDefault();

    [Export]
    public CompanyCareerData CompanyCareerData { get; private set; } = new();

    [Export]
    public CompanyRunData CompanyRunData { get; private set; } = new();

	[Export]
	public TownTimeState TownTimeState { get; private set; } = new();

	[Export]
	public SettingsConfig SettingsConfig { get; private set; } = new();

    public static SaveNode Get()
    {
        var sceneTree = Engine.GetMainLoop() as SceneTree;
        return sceneTree?.Root?.GetNodeOrNull<SaveNode>("/root/SaveNode");
    }

    public void Save()
    {
        throw new NotImplementedException();
    }

    public void Load()
    {
        throw new NotImplementedException();
    }
}
