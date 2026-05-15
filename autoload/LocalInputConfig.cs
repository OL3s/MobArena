using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources;

namespace MobArena.Scripts;

public partial class LocalInputConfig : Node
{
	public const int MaxLocalPlayers = 4;

	[Export]
	public Texture2D XboxAIcon { get; private set; }

	[Export]
	public Texture2D XboxBIcon { get; private set; }

	[Export]
	public Texture2D MouseIcon { get; private set; }

	[Export]
	public Texture2D EnterIcon { get; private set; }

	[Export]
	public Texture2D BackspaceIcon { get; private set; }

	[Export]
	public Texture2D PhoneIcon { get; private set; }

	[Export]
	public Array<LocalInputControllerConfig> ControllerSetups { get; private set; } = new();

	public Texture2D JoinPromptIcon => XboxAIcon;
	public Texture2D KeyboardJoinPromptIcon => EnterIcon;
	public Texture2D TouchJoinPromptIcon => PhoneIcon;
	public Texture2D LeavePromptIcon => IsConsoleLikePlatform() ? XboxBIcon : BackspaceIcon;
	public string JoinPromptLabel => ControllerSetups.Count >= MaxLocalPlayers ? "All 4 local slots filled" : "Press A to join";
	public string LeavePromptLabel => IsMobilePlatform() ? "Leave on phone: not set yet" : IsConsoleLikePlatform() ? "Press B to leave" : "Press Backspace to leave";
	public bool HasControllerSetups => ControllerSetups.Count > 0;
	public bool HasKeyboardPlayer => HasControllerKind(LocalInputControllerConfig.ControllerKind.Keyboard);
	public bool HasTouchPlayer => HasControllerKind(LocalInputControllerConfig.ControllerKind.Touch);
	public bool CanJoin => ControllerSetups.Count < MaxLocalPlayers;

	public static LocalInputConfig Get()
	{
		var sceneTree = Engine.GetMainLoop() as SceneTree;
		return sceneTree?.Root?.GetNodeOrNull<LocalInputConfig>("/root/LocalInputConfig");
	}

	public override void _Ready()
	{
		InitializeCurrentControllerSetups();
	}

	public override void _ExitTree()
	{
		ControllerSetups.Clear();
	}

	public void InitializeCurrentControllerSetups()
	{
		ControllerSetups.Clear();

		var settingsConfig = SaveNode.Get()?.SettingsConfig ?? new SettingsConfig();
		if (settingsConfig.AutoDetectPrimaryInput)
		{
			AddAutoDetectedPrimaryControllerSetup();
			AddConnectedGamepads();
			return;
		}

		AddDefaultPrimaryControllerSetup(settingsConfig.DefaultPrimaryInput);
	}

	public void ClearControllerSetups()
	{
		ControllerSetups.Clear();
	}

	public bool TryJoinGamepad(int deviceId)
	{
		if (!CanJoin || HasGamepadPlayer(deviceId))
			return false;

		ControllerSetups.Add(LocalInputControllerConfig.Create(
			$"Gamepad {ControllerSetups.Count + 1}",
			LocalInputControllerConfig.ControllerKind.Gamepad,
			deviceId,
			XboxAIcon));
		return true;
	}

	public bool TryJoinKeyboard()
	{
		if (!CanJoin || HasKeyboardPlayer)
			return false;

		ControllerSetups.Add(LocalInputControllerConfig.Create("Keyboard", LocalInputControllerConfig.ControllerKind.Keyboard, -1, MouseIcon));
		return true;
	}

	public bool TryJoinTouch()
	{
		if (!CanJoin || HasTouchPlayer)
			return false;

		ControllerSetups.Add(LocalInputControllerConfig.Create("Touch", LocalInputControllerConfig.ControllerKind.Touch, -1, PhoneIcon));
		return true;
	}

	public bool TryLeaveGamepad(int deviceId)
	{
		for (var index = ControllerSetups.Count - 1; index >= 0; index--)
		{
			var controllerSetup = ControllerSetups[index];
			if (controllerSetup.Kind != LocalInputControllerConfig.ControllerKind.Gamepad || controllerSetup.DeviceId != deviceId)
				continue;

			ControllerSetups.RemoveAt(index);
			RenumberGamepads();
			return true;
		}

		return false;
	}

	public bool TryLeaveKeyboard()
	{
		return TryLeaveFirst(LocalInputControllerConfig.ControllerKind.Keyboard);
	}

	public bool TryLeaveTouch()
	{
		return TryLeaveFirst(LocalInputControllerConfig.ControllerKind.Touch);
	}

	public bool HasGamepadPlayer()
	{
		return HasControllerKind(LocalInputControllerConfig.ControllerKind.Gamepad);
	}

	public bool HasGamepadPlayer(int deviceId)
	{
		foreach (var controllerSetup in ControllerSetups)
		{
			if (controllerSetup.Kind == LocalInputControllerConfig.ControllerKind.Gamepad && controllerSetup.DeviceId == deviceId)
				return true;
		}

		return false;
	}

	public bool HasKeyboardSetup()
	{
		return HasKeyboardPlayer;
	}

	public bool HasTouchSetup()
	{
		return HasTouchPlayer;
	}

	public Texture2D GetLeavePromptIcon(LocalInputControllerConfig controllerSetup)
	{
		return controllerSetup.Kind == LocalInputControllerConfig.ControllerKind.Gamepad ? XboxBIcon : BackspaceIcon;
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

	private void AddDefaultPrimaryControllerSetup(SettingsConfig.PrimaryInputMode primaryInputMode)
	{
		switch (primaryInputMode)
		{
			case SettingsConfig.PrimaryInputMode.Keyboard:
				TryJoinKeyboard();
				break;
			case SettingsConfig.PrimaryInputMode.Touch:
				TryJoinTouch();
				break;
			case SettingsConfig.PrimaryInputMode.Gamepad:
				AddFirstConnectedGamepad();
				break;
			case SettingsConfig.PrimaryInputMode.None:
			default:
				break;
		}
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

			if (HasGamepadPlayer(joypadId))
				continue;

			ControllerSetups.Add(LocalInputControllerConfig.Create(
				$"Gamepad {ControllerSetups.Count + 1}",
				LocalInputControllerConfig.ControllerKind.Gamepad,
				joypadId,
				XboxAIcon));
		}
	}

	private bool TryLeaveFirst(LocalInputControllerConfig.ControllerKind kind)
	{
		for (var index = ControllerSetups.Count - 1; index >= 0; index--)
		{
			if (ControllerSetups[index].Kind != kind)
				continue;

			ControllerSetups.RemoveAt(index);
			RenumberGamepads();
			return true;
		}

		return false;
	}

	private bool HasControllerKind(LocalInputControllerConfig.ControllerKind kind)
	{
		foreach (var controllerSetup in ControllerSetups)
		{
			if (controllerSetup.Kind == kind)
				return true;
		}

		return false;
	}

	private void RenumberGamepads()
	{
		var gamepadNumber = 1;
		foreach (var controllerSetup in ControllerSetups)
		{
			if (controllerSetup.Kind != LocalInputControllerConfig.ControllerKind.Gamepad)
				continue;

			controllerSetup.ControllerName = $"Gamepad {gamepadNumber}";
			gamepadNumber++;
		}
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
