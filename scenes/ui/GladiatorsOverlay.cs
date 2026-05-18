using Godot;
using System.Collections.Generic;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class GladiatorsOverlay : Control
{
	private const string GladiatorCardScenePath = "res://scenes/components/ui/GladiatorCard.tscn";

    private HBoxContainer _gladiatorRow;
    private CompanyRunData _companyRunData;
    private readonly List<GladiatorCard> _gladiatorCards = new();

    public override void _Ready()
    {
        _companyRunData = SaveNode.Get().CompanyRunData;
        _gladiatorRow = GetNode<HBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/ScrollContainer/GladiatorRow");
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;
        _companyRunData.RunChanged += RefreshGladiatorCards;
        PopulateGladiators();
    }

    public override void _ExitTree()
    {
        if (_companyRunData != null)
            _companyRunData.RunChanged -= RefreshGladiatorCards;
    }

    private void PopulateGladiators()
	{
		var gladiatorCardScene = ResourceLoader.Load<PackedScene>(GladiatorCardScenePath);
		if (_companyRunData == null || gladiatorCardScene == null)
			return;

		foreach (var gladiatorData in _companyRunData.Gladiators)
		{
			var card = gladiatorCardScene.Instantiate<GladiatorCard>();
            _gladiatorRow.AddChild(card);
            _gladiatorCards.Add(card);
            card.Configure(gladiatorData);
        }
    }

    private void RefreshGladiatorCards()
    {
        if (_companyRunData == null)
            return;

        var count = Mathf.Min(_gladiatorCards.Count, _companyRunData.Gladiators.Count);
        for (var index = 0; index < count; index++)
        {
            _gladiatorCards[index].Configure(_companyRunData.Gladiators[index]);
        }
    }
}
