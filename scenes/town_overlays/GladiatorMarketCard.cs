using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.TownOverlays;

public partial class GladiatorMarketCard : VBoxContainer
{
    [Signal]
    public delegate void HirePressedEventHandler(GladiatorData gladiator);

    [Export]
    public PackedScene GladiatorCardScene { get; set; }

    private GladiatorCard _card;
    private TextureRect _goldIcon;
    private Label _priceLabel;
    private Button _hireButton;
    private GladiatorData _gladiator;
    private Texture2D _goldTexture;
    private int _price;
    private bool _canHire;

    public override void _Ready()
    {
        _goldIcon = GetNode<TextureRect>("ActionRow/GoldIcon");
        _priceLabel = GetNode<Label>("ActionRow/PriceLabel");
        _hireButton = GetNode<Button>("ActionRow/HireButton");
        _hireButton.Pressed += OnHirePressed;

        _card = GladiatorCardScene?.Instantiate<GladiatorCard>();
        if (_card != null)
            AddChild(_card);
        else
            GD.PushError("Gladiator card scene is missing or has the wrong root script.");

        MoveChild(GetNode("ActionRow"), GetChildCount() - 1);
        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_hireButton != null)
            _hireButton.Pressed -= OnHirePressed;
    }

    public void Configure(GladiatorData gladiator, Texture2D goldIcon, int price, bool canHire)
    {
        _gladiator = gladiator;
        _goldTexture = goldIcon;
        _price = price;
        _canHire = canHire;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _card?.Configure(_gladiator);
        _goldIcon.Texture = _goldTexture;
        _priceLabel.Text = _price.ToString();
        _hireButton.Disabled = !_canHire;
    }

    private void OnHirePressed()
    {
        if (_gladiator != null)
            EmitSignal(SignalName.HirePressed, _gladiator);
    }
}
