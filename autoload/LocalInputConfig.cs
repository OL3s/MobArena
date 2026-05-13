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
	public string LeavePromptLabel => IsMobilePlatform() && GetSettingsConfig().DefaultPrimaryInput == SettingsConfig.PrimaryInputMode.Touch ? "Leave on phone: not set yet" : IsConsoleLikePlatform() ? "Press B to leave" : "Press Backspace to leave";
	public SettingsConfig SettingsConfig => GetSettingsConfig();

	public static LocalInputConfig Get()
	{
		var sceneTree = Engine.GetMainLoop() as SceneTree;
		return sceneTree?.Root?.GetNodeOrNull<LocalInputConfig>("/root/LocalInputConfig");
	}

	public override void _Ready()
	{
		RefreshConnectedControllerSetups();
	}

	public void RefreshConnectedControllerSetups()
	{
		ControllerSetups.Clear();
		var settingsConfig = GetSettingsConfig();

		var primaryInputMode = settingsConfig.AutoDetectPrimaryInput
			? GetAutoDetectedPrimaryInputMode()
			: settingsConfig.DefaultPrimaryInput;

		AddPrimaryControllerSetup(primaryInputMode);

		foreach (var joypadId in Input.GetConnectedJoypads())
		{
			if (ControllerSetups.Count >= MaxLocalPlayers)
				break;

			if (ControllerSetups.Count > 0
				&& ControllerSetups[0].Kind == LocalInputControllerConfig.ControllerKind.Gamepad
				&& ControllerSetups[0].DeviceId == joypadId)
				continue;

			ControllerSetups.Add(LocalInputControllerConfig.Create(
				$"Gamepad {ControllerSetups.Count + 1}",
				LocalInputControllerConfig.ControllerKind.Gamepad,
				joypadId,
				XboxAIcon));
		}
	}

	private void AddPrimaryControllerSetup(SettingsConfig.PrimaryInputMode primaryInputMode)
	{
		switch (primaryInputMode)
		{
			case SettingsConfig.PrimaryInputMode.Gamepad:
				var joypads = Input.GetConnectedJoypads();
				if (joypads.Count > 0)
				{
					ControllerSetups.Add(LocalInputControllerConfig.Create("Gamepad 1", LocalInputControllerConfig.ControllerKind.Gamepad, joypads[0], XboxAIcon));
					return;
				}

				ControllerSetups.Add(LocalInputControllerConfig.Create("Gamepad", LocalInputControllerConfig.ControllerKind.Gamepad, -1, XboxAIcon, false));
				return;
			case SettingsConfig.PrimaryInputMode.Touch:
				ControllerSetups.Add(LocalInputControllerConfig.Create("Touch", LocalInputControllerConfig.ControllerKind.Touch, -1, PhoneIcon));
				return;
			default:
				ControllerSetups.Add(LocalInputControllerConfig.Create("Keyboard", LocalInputControllerConfig.ControllerKind.Keyboard, -1, MouseIcon));
				return;
		}
	}

	private static SettingsConfig.PrimaryInputMode GetAutoDetectedPrimaryInputMode()
	{
		if (IsConsoleLikePlatform())
			return SettingsConfig.PrimaryInputMode.Gamepad;

		if (IsMobilePlatform())
			return Input.GetConnectedJoypads().Count > 0
				? SettingsConfig.PrimaryInputMode.Gamepad
				: SettingsConfig.PrimaryInputMode.Touch;

		return SettingsConfig.PrimaryInputMode.Keyboard;
	}

	private static SettingsConfig GetSettingsConfig()
	{
		return SaveNode.Get()?.SettingsConfig ?? new SettingsConfig();
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
