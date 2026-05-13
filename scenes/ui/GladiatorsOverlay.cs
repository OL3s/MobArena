using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;

namespace MobArena.Scenes.UI;

public partial class GladiatorsOverlay : Control
{
	private const string GladiatorCardScenePath = "res://scenes/components/ui/GladiatorCard.tscn";

    private HBoxContainer _gladiatorRow;

    public override void _Ready()
    {
        _gladiatorRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ScrollContainer/GladiatorRow");
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;
        PopulateGladiators();
    }

    private void PopulateGladiators()
	{
		var saveNode = SaveNode.Get();
		var gladiatorCardScene = ResourceLoader.Load<PackedScene>(GladiatorCardScenePath);
		if (saveNode == null || gladiatorCardScene == null)
			return;

		foreach (var gladiatorData in saveNode.CompanyRunData.Gladiators)
		{
			var card = gladiatorCardScene.Instantiate<GladiatorCard>();
            _gladiatorRow.AddChild(card);
            card.Configure(gladiatorData);
        }
    }
}
