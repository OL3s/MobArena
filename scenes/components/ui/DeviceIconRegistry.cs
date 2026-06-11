using Godot;
using MobArena.Scripts.Resources;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.UI;

public static class DeviceIconRegistry
{
    public enum DeviceIconKind
    {
        Keyboard,
        Mouse,
        Phone,
        Console,
        Pc
    }

    private static readonly Dictionary<LocalInputControllerConfig.ControllerKind, string> ControllerDeviceIcons = new()
    {
        [LocalInputControllerConfig.ControllerKind.Keyboard] = "res://assets/ui/input_icons/device_keyboard.svg",
        [LocalInputControllerConfig.ControllerKind.Mouse] = "res://assets/ui/input_icons/device_mouse.svg",
        [LocalInputControllerConfig.ControllerKind.Touch] = "res://assets/ui/input_icons/device_phone.svg",
        [LocalInputControllerConfig.ControllerKind.Gamepad] = "res://assets/ui/input_icons/device_console.svg"
    };

    private static readonly Dictionary<DeviceIconKind, string> DeviceIcons = new()
    {
        [DeviceIconKind.Keyboard] = "res://assets/ui/input_icons/device_keyboard.svg",
        [DeviceIconKind.Mouse] = "res://assets/ui/input_icons/device_mouse.svg",
        [DeviceIconKind.Phone] = "res://assets/ui/input_icons/device_phone.svg",
        [DeviceIconKind.Console] = "res://assets/ui/input_icons/device_console.svg",
        [DeviceIconKind.Pc] = "res://assets/ui/input_icons/device_pc.svg"
    };

    public static Texture2D LoadDeviceIcon(LocalInputControllerConfig.ControllerKind kind)
    {
        return UiIconLoader.LoadIcon(ControllerDeviceIcons.GetValueOrDefault(kind, UiIconLoader.FallbackIconPath));
    }

    public static Texture2D LoadDeviceIcon(DeviceIconKind kind)
    {
        return UiIconLoader.LoadIcon(DeviceIcons.GetValueOrDefault(kind, UiIconLoader.FallbackIconPath));
    }
}
