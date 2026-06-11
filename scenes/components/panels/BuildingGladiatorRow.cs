using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.Panels;

public partial class BuildingGladiatorRow : PanelContainer
{
    [Signal]
    public delegate void DragRequestedEventHandler(GladiatorData gladiator);

    private Button _portraitButton;
    private Label _nameLabel;
    private VBoxContainer _details;
    private GladiatorData _gladiator;

    public VBoxContainer Details => _details ??= GetNodeOrNull<VBoxContainer>("MarginContainer/Row/Content/Details");

    public override void _Ready()
    {
        _portraitButton = GetNode<Button>("MarginContainer/Row/PortraitButton");
        _nameLabel = GetNode<Label>("MarginContainer/Row/Content/NameLabel");
        _details = GetNode<VBoxContainer>("MarginContainer/Row/Content/Details");
        _portraitButton.ButtonDown += OnPortraitButtonDown;
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_portraitButton != null)
            _portraitButton.ButtonDown -= OnPortraitButtonDown;
    }

    public void Configure(GladiatorData gladiator, bool showName)
    {
        _gladiator = gladiator;
        if (IsNodeReady())
        {
            _nameLabel.Visible = showName;
            RefreshUi();
        }
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _portraitButton.Icon = _gladiator?.GetUiIconTexture();
        _portraitButton.TooltipText = _gladiator == null ? string.Empty : $"Drag {_gladiator.GladiatorName}";
        _nameLabel.Text = _gladiator?.GladiatorName ?? "Gladiator";
    }

    private void OnPortraitButtonDown()
    {
        if (_gladiator != null)
            EmitSignal(SignalName.DragRequested, _gladiator);
    }
}
