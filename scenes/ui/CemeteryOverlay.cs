using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;

namespace MobArena.Scenes.UI;

public partial class CemeteryOverlay : Control
{
    private const string GladiatorCardScenePath = "res://scenes/components/ui/GladiatorCard.tscn";

    private HBoxContainer _cemeteryRow;
    private Label _emptyLabel;

    public override void _Ready()
    {
        _cemeteryRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ScrollContainer/CemeteryRow");
        _emptyLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/EmptyLabel");
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;

        PopulateCemetery();
    }

    private void PopulateCemetery()
    {
        var cemetery = SaveNode.Get().CompanyRunData.Cemetery;
        _emptyLabel.Visible = cemetery.Count <= 0;

        var gladiatorCardScene = ResourceLoader.Load<PackedScene>(GladiatorCardScenePath);
        if (gladiatorCardScene == null)
            return;

        foreach (var gladiatorData in cemetery)
        {
            var card = gladiatorCardScene.Instantiate<GladiatorCard>();
            _cemeteryRow.AddChild(card);
            card.Configure(gladiatorData);
        }
    }
}
