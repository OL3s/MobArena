using Godot;
using MobArena.Scripts;

namespace MobArena.Scenes.TownOverlays;

public partial class MarketOverlay : Control
{
    private const string GladiatorMarketOverlayScene = "res://scenes/town_overlays/gladiator_market_overlay.tscn";
    private const string RationsOverlayScene = "res://scenes/town_overlays/rations_overlay.tscn";
    private const string BlacksmithOverlayScene = "res://scenes/town_overlays/blacksmith_overlay.tscn";

    public override void _Ready()
    {
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/GladiatorsButton").Pressed += OnGladiatorsPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/RationsButton").Pressed += OnRationsPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/Actions/BlacksmithButton").Pressed += OnBlacksmithPressed;
        GetNode<Button>("CenterContainer/Panel/MarginContainer/Layout/CloseButton").Pressed += QueueFree;
    }

    private void OnGladiatorsPressed()
    {
        OpenOverlay(GladiatorMarketOverlayScene);
    }

    private void OnRationsPressed()
    {
        OpenOverlay(RationsOverlayScene);
    }

    private void OnBlacksmithPressed()
    {
        OpenOverlay(BlacksmithOverlayScene);
    }

    private void OpenOverlay(string scenePath)
    {
        var globalOverlay = GlobalOverlay.Get();
        var overlayScene = ResourceLoader.Load<PackedScene>(scenePath);
        if (globalOverlay == null || overlayScene == null)
            return;

        QueueFree();
        globalOverlay.AddOverlay(overlayScene);
    }
}
