using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class AssignedArenaGladiatorButton : Button
{
    [Signal]
    public delegate void DragRequestedEventHandler(GladiatorData gladiator);

    private GladiatorData _gladiator;

    public override void _Ready()
    {
        ButtonDown += OnButtonDown;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        ButtonDown -= OnButtonDown;
    }

    public void Configure(GladiatorData gladiator)
    {
        _gladiator = gladiator;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        Icon = _gladiator?.GetUiIconTexture();
        TooltipText = _gladiator != null ? $"Drag {_gladiator.GladiatorName}" : string.Empty;
    }

    private void OnButtonDown()
    {
        if (_gladiator != null)
            EmitSignal(SignalName.DragRequested, _gladiator);
    }
}
