using Godot;

namespace MobArena.Scripts.Resources;

public partial class LocalInputControllerConfig : Resource
{
	public enum ControllerKind
	{
		Keyboard,
		Touch,
		Gamepad
	}

	[Export]
	public string ControllerName { get; set; } = "Keyboard";

	[Export]
	public ControllerKind Kind { get; set; } = ControllerKind.Keyboard;

	[Export]
	public int DeviceId { get; set; } = -1;

	[Export]
	public Texture2D Icon { get; set; }

	[Export]
	public bool IsJoined { get; set; } = true;

	public static LocalInputControllerConfig Create(string controllerName, ControllerKind kind, int deviceId, Texture2D icon, bool isJoined = true)
	{
		return new LocalInputControllerConfig
		{
			ControllerName = controllerName,
			Kind = kind,
			DeviceId = deviceId,
			Icon = icon,
			IsJoined = isJoined
		};
	}
}
