using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class CompanyLogo : Control
{
    [Signal]
    public delegate void PressedEventHandler();

    [Export]
    public CompanyLogoData LogoData { get; set; } = CompanyLogoData.CreateDefault();

    private TextureRect _shield;
    private TextureRect _logo;

    public override void _Ready()
    {
        _shield = GetNode<TextureRect>("Shield");
        _logo = GetNode<TextureRect>("Logo");
        MouseFilter = MouseFilterEnum.Stop;
        GuiInput += OnGuiInput;
        ApplyData();
    }

    public override void _ExitTree()
    {
        GuiInput -= OnGuiInput;

        if (LogoData != null)
            LogoData.LogoChanged -= ApplyData;
    }

    public void SetLogoData(CompanyLogoData logoData)
    {
        if (LogoData != null)
            LogoData.LogoChanged -= ApplyData;

        LogoData = logoData ?? CompanyLogoData.CreateDefault();
        LogoData.LogoChanged += ApplyData;
        ApplyData();
    }

    public void ApplyData()
    {
        if (!IsNodeReady()
            || !GodotObject.IsInstanceValid(_shield)
            || !GodotObject.IsInstanceValid(_logo)
            || LogoData == null)
            return;

        LogoData.ApplyTo(_shield, _logo);
    }

    private void OnGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }
            || inputEvent is InputEventScreenTouch { Pressed: true })
        {
            GetViewport()?.SetInputAsHandled();
            EmitSignal(SignalName.Pressed);
        }
    }
}
