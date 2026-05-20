using Godot;

namespace MobArena.Scripts.Resources;

public partial class SettingsConfig : Resource
{
    [Export]
    public bool DebugEnabled { get; set; } = true;

	[Export(PropertyHint.Range, "0.1,1,0.05")]
	public float LowHealthWarningRatio { get; set; } = 0.6f;
}
