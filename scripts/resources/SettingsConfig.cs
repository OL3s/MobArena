using Godot;

namespace MobArena.Scripts.Resources;

public partial class SettingsConfig : Resource
{
	public enum PrimaryInputMode
	{
		None,
		Keyboard,
		Touch,
		Gamepad
	}

	[Export]
	public bool AutoDetectPrimaryInput { get; set; } = true;

	[Export]
	public PrimaryInputMode DefaultPrimaryInput { get; set; } = PrimaryInputMode.Keyboard;
}
