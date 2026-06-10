using Godot;

namespace MobArena.Scripts.Resources;

public partial class SettingsConfig : Resource
{
	[Export]
	public bool DevEnabled { get; set; } = true;

	[Export]
	public bool IsDemo { get; set; } = false;

	[Export]
	public bool ShowRuntimeTags { get; set; } = false;

	[Export]
	public bool SkipTutorial { get; set; }

	[Export(PropertyHint.Range, "0.1,1,0.05")]
	public float LowHealthWarningRatio { get; set; } = 0.6f;

	[Export(PropertyHint.Range, "0,0.95,0.01")]
	public float ArenaMoveDeadzone { get; set; } = 0.3f;

	[Export(PropertyHint.Range, "1,4,1")]
	public int ArenaAutoAssignCount { get; set; } = 1;
}
