using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class GladiatorDeathOverlay : Control
{
    private GladiatorData _gladiatorData;
    private Label _messageLabel;
    private GladiatorCard _gladiatorCard;

    public override void _Ready()
    {
        _messageLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/MessageLabel");
        _gladiatorCard = GetNode<GladiatorCard>("CenterContainer/PopupPanel/MarginContainer/Content/GladiatorCard");
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;

        if (_gladiatorData != null)
            RefreshUi();
    }

    public void Configure(GladiatorData gladiatorData)
    {
        _gladiatorData = gladiatorData;

        if (IsNodeReady())
            RefreshUi();
    }

    private void RefreshUi()
    {
        _messageLabel.Text = $"{_gladiatorData.GladiatorName} has died in service to the company.";
        _gladiatorCard.Configure(_gladiatorData);
    }
}
