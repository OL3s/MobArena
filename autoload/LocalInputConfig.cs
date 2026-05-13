using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class LocalInputConfig : Node
{
	public const int MaxLocalPlayers = 4;

	private static readonly Texture2D XboxAIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/xbox_button_a.svg");
	private static readonly Texture2D XboxBIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/xbox_button_b.svg");
	private static readonly Texture2D MouseIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/mouse_left_button.svg");
	private static readonly Texture2D BackspaceIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/keyboard_backspace.svg");
	private static readonly Texture2D PhoneIcon = ResourceLoader.Load<Texture2D>("res://assets/ui/input_icons/device_phone.png");

	[Export]
	public Array<LocalInputControllerConfig> ControllerSetups { get; private set; } = new();

	public Texture2D JoinPromptIcon => XboxAIcon;
	public Texture2D LeavePromptIcon => IsConsoleLikePlatform() ? XboxBIcon : BackspaceIcon;
	public string JoinPromptLabel => ControllerSetups.Count >= MaxLocalPlayers ? "All 4 local slots filled" : "Press A to join";
	public string LeavePromptLabel => IsMobilePlatform() ? "Leave on phone: not set yet" : IsConsoleLikePlatform() ? "Press B to leave" : "Press Backspace to leave";

	public static LocalInputConfig Get()
	{
		var sceneTree = Engine.GetMainLoop() as SceneTree;
		return sceneTree?.Root?.GetNodeOrNull<LocalInputConfig>("/root/LocalInputConfig");
	}

	public override void _Ready()
	{
		InitializeCurrentControllerSetups();
	}

	public void InitializeCurrentControllerSetups()
	{
		ControllerSetups.Clear();
		AddAutoDetectedPrimaryControllerSetup();
		AddConnectedGamepads();
	}

	private void AddAutoDetectedPrimaryControllerSetup()
	{
		if (IsConsoleLikePlatform())
		{
			AddFirstConnectedGamepad();
			return;
		}

		if (IsMobilePlatform())
		{
			if (!AddFirstConnectedGamepad())
				ControllerSetups.Add(LocalInputControllerConfig.Create("Touch", LocalInputControllerConfig.ControllerKind.Touch, -1, PhoneIcon));

			return;
		}

		ControllerSetups.Add(LocalInputControllerConfig.Create("Keyboard", LocalInputControllerConfig.ControllerKind.Keyboard, -1, MouseIcon));
	}

	private bool AddFirstConnectedGamepad()
	{
		var joypads = Input.GetConnectedJoypads();
		if (joypads.Count == 0)
			return false;

		ControllerSetups.Add(LocalInputControllerConfig.Create("Gamepad 1", LocalInputControllerConfig.ControllerKind.Gamepad, joypads[0], XboxAIcon));
		return true;
	}

	private void AddConnectedGamepads()
	{
		foreach (var joypadId in Input.GetConnectedJoypads())
		{
			if (ControllerSetups.Count >= MaxLocalPlayers)
				break;

			if (HasGamepadSetup(joypadId))
				continue;

			ControllerSetups.Add(LocalInputControllerConfig.Create(
				$"Gamepad {ControllerSetups.Count + 1}",
				LocalInputControllerConfig.ControllerKind.Gamepad,
				joypadId,
				XboxAIcon));
		}
	}

	private bool HasGamepadSetup(int deviceId)
	{
		foreach (var controllerSetup in ControllerSetups)
		{
			if (controllerSetup.Kind == LocalInputControllerConfig.ControllerKind.Gamepad && controllerSetup.DeviceId == deviceId)
				return true;
		}

		return false;
	}

	private static bool IsMobilePlatform()
	{
		var osName = OS.GetName();
		return OS.HasFeature("mobile") || osName is "Android" or "iOS";
	}

	private static bool IsConsoleLikePlatform()
	{
		var osName = OS.GetName();
		var isDesktop = OS.HasFeature("pc") || osName is "Windows" or "macOS" or "Linux" or "FreeBSD" or "NetBSD" or "OpenBSD" or "BSD";
		return OS.HasFeature("console") || (!isDesktop && !IsMobilePlatform() && !OS.HasFeature("web"));
	}
}
