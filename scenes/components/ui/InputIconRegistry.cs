using Godot;
using MobArena.Scripts.Resources;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.UI;

public static class InputIconRegistry
{
    public enum InputIconKind
    {
        KeyboardEnter,
        KeyboardBackspace,
        KeyboardMoveUp,
        MouseLeftButton,
        XboxButtonA,
        XboxButtonB,
        PhoneTouch
    }

    private static readonly Dictionary<InputIconKind, string> InputIcons = new()
    {
        [InputIconKind.KeyboardEnter] = "res://assets/ui/input_icons/keyboard_enter.svg",
        [InputIconKind.KeyboardBackspace] = "res://assets/ui/input_icons/keyboard_backspace.svg",
        [InputIconKind.KeyboardMoveUp] = "res://assets/ui/input_icons/keyboard_key_w.svg",
        [InputIconKind.MouseLeftButton] = "res://assets/ui/input_icons/mouse_left_button.svg",
        [InputIconKind.XboxButtonA] = "res://assets/ui/input_icons/xbox_button_a.svg",
        [InputIconKind.XboxButtonB] = "res://assets/ui/input_icons/xbox_button_b.svg",
        [InputIconKind.PhoneTouch] = "res://assets/ui/input_icons/phone_touch.svg"
    };

    public static Texture2D LoadInputIcon(InputIconKind kind)
    {
        return UiIconLoader.LoadIcon(InputIcons.GetValueOrDefault(kind, UiIconLoader.FallbackIconPath));
    }

    public static Texture2D LoadJoinPromptIcon(LocalInputControllerConfig.ControllerKind kind)
    {
        return kind switch
        {
            LocalInputControllerConfig.ControllerKind.Keyboard => LoadInputIcon(InputIconKind.KeyboardEnter),
            LocalInputControllerConfig.ControllerKind.Mouse => LoadInputIcon(InputIconKind.MouseLeftButton),
            LocalInputControllerConfig.ControllerKind.Touch => LoadInputIcon(InputIconKind.PhoneTouch),
            LocalInputControllerConfig.ControllerKind.Gamepad => LoadInputIcon(InputIconKind.XboxButtonA),
            _ => UiIconLoader.LoadFallbackIcon()
        };
    }
}
