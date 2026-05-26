using Godot;

namespace MobArena.Scripts.Resources;

public partial class LocalInputControllerConfig : Resource
{
	public enum ControllerKind
	{
		Keyboard,
		Mouse,
		Touch,
		Gamepad
	}

	[Export]
	public ControllerKind Kind { get; set; } = ControllerKind.Keyboard;

	[Export]
	public int DeviceId { get; set; } = -1;

	[Export]
	public Texture2D Icon { get; set; }

	[Export]
	public bool IsJoined { get; set; } = true;

	public string DisplayName => GetDisplayName(Kind, DeviceId);

	public static LocalInputControllerConfig Create(ControllerKind kind, int deviceId, Texture2D icon, bool isJoined = true)
	{
		return new LocalInputControllerConfig
		{
			Kind = kind,
			DeviceId = deviceId,
			Icon = icon,
			IsJoined = isJoined
		};
	}

	public static string GetDisplayName(ControllerKind kind, int deviceId)
	{
		return kind == ControllerKind.Gamepad
			? $"{kind} {deviceId}"
			: kind.ToString();
	}
}
