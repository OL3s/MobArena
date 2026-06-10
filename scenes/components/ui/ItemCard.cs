using Godot;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Items;
using System.Collections.Generic;

namespace MobArena.Scenes.Components.UI;

public partial class ItemCard : PanelContainer
{
    public enum CardMode
    {
        Purchase,
        Equipment
    }

    private const string ConditionIconPath = "res://assets/ui/items/condition.svg";
    private const string DragIconPath = "res://assets/ui/items/drag_hand.svg";
    private const string ArmorTypeIconPath = "res://assets/ui/items/type_armor.svg";
    private const string MainHandTypeIconPath = "res://assets/ui/items/type_main_hand.svg";
    private const string TwoHandedTypeIconPath = "res://assets/ui/items/type_two_handed.svg";
    private const string OffHandTypeIconPath = "res://assets/ui/items/type_off_hand.svg";
    private const string UnknownIconPath = "res://assets/ui/icons/question_mark.svg";
    private const int MaxAttackTypeIcons = 5;

    [Signal]
    public delegate void BuyPressedEventHandler(ItemData item);

    [Signal]
    public delegate void DragRequestedEventHandler(ItemData item);

    private ItemData _item;
    private CardMode _mode;
    private bool _canBuy = true;
    private TextureRect _itemIcon;
    private TextureRect _typeIcon;
    private Label _nameLabel;
    private TextureRect _conditionIcon;
    private HBoxContainer _attackTypeRow;
    private ProgressBar _conditionBar;
    private Label _goldLabel;
    private Button _buyButton;
    private Button _dragButton;

    public override void _Ready()
    {
        _itemIcon = GetNode<TextureRect>("MarginContainer/Layout/IconPanel/ItemIcon");
        _typeIcon = GetNode<TextureRect>("MarginContainer/Layout/IconPanel/TypeBadge/TypeIcon");
        _nameLabel = GetNode<Label>("MarginContainer/Layout/NameLabel");
        _attackTypeRow = GetNode<HBoxContainer>("MarginContainer/Layout/AttackTypeRow");
        _conditionIcon = GetNode<TextureRect>("MarginContainer/Layout/ConditionRow/Icon");
        _conditionBar = GetNode<ProgressBar>("MarginContainer/Layout/ConditionRow/Bar");
        _goldLabel = GetNode<Label>("MarginContainer/Layout/GoldRow/GoldLabel");
        _buyButton = GetNode<Button>("MarginContainer/Layout/BuyButton");
        _dragButton = GetNode<Button>("MarginContainer/Layout/DragButton");

        _conditionIcon.Texture = ResourceLoader.Load<Texture2D>(ConditionIconPath);
        _dragButton.Icon = ResourceLoader.Load<Texture2D>(DragIconPath);
        _buyButton.Pressed += OnBuyPressed;
        _dragButton.ButtonDown += OnDragButtonDown;

        RefreshUi();
    }

    public override void _ExitTree()
    {
        if (_buyButton != null)
            _buyButton.Pressed -= OnBuyPressed;

        if (_dragButton != null)
            _dragButton.ButtonDown -= OnDragButtonDown;
    }

    public void Configure(ItemData item, CardMode mode, bool canBuy = true)
    {
        _item = item;
        _mode = mode;
        _canBuy = canBuy;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _itemIcon.Texture = _item?.UiIcon;
        _typeIcon.Texture = GetTypeIcon(_item);
        _nameLabel.Text = _item?.DisplayName ?? "Item";
        _goldLabel.Text = (_item?.Cost ?? 0).ToString();
        RefreshAttackTypeRow();

        var condition = Mathf.Clamp(_item?.Condition ?? 0f, 0f, 1f);
        _conditionBar.MaxValue = 1.0;
        _conditionBar.Value = condition;
        _conditionBar.AddThemeStyleboxOverride("fill", new StyleBoxFlat { BgColor = GetConditionColor(condition) });
        _conditionBar.AddThemeStyleboxOverride("background", new StyleBoxFlat { BgColor = new Color(0.12f, 0.1f, 0.08f, 1f) });

        _buyButton.Visible = _mode == CardMode.Purchase;
        _buyButton.Disabled = !_canBuy;
        _dragButton.Visible = _mode == CardMode.Equipment;
    }

    private void OnBuyPressed()
    {
        if (_item != null)
            EmitSignal(SignalName.BuyPressed, _item);
    }

    private void OnDragButtonDown()
    {
        if (_item != null)
            EmitSignal(SignalName.DragRequested, _item);
    }

    private static Texture2D GetTypeIcon(ItemData item)
    {
        var iconPath = item switch
        {
            ArmorItemData => ArmorTypeIconPath,
            MainHandItemData mainHand => mainHand.IsTwoHanded ? TwoHandedTypeIconPath : MainHandTypeIconPath,
            OffHandItemData => OffHandTypeIconPath,
            _ => UnknownIconPath
        };

        return ResourceLoader.Load<Texture2D>(iconPath);
    }

    private void RefreshAttackTypeRow()
    {
        foreach (var child in _attackTypeRow.GetChildren())
            child.QueueFree();

        if (_item is not DamageItemData damageItem || damageItem.MainAction?.Effect == null)
        {
            _attackTypeRow.Hide();
            return;
        }

        var effects = new List<ArenaCombatEffectData>();
        CollectAttackTypes(damageItem.MainAction.Effect, effects, new HashSet<ArenaCombatEffectData>());
        if (effects.Count <= 0)
        {
            _attackTypeRow.Hide();
            return;
        }

        _attackTypeRow.Show();
        foreach (var effect in effects)
        {
            var icon = new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>(effect.AttackTypeIconPath),
                CustomMinimumSize = new Vector2(22f, 22f),
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                TooltipText = effect.AttackTypeLabel
            };
            _attackTypeRow.AddChild(icon);
        }
    }

    private static void CollectAttackTypes(ArenaCombatEffectData effect, List<ArenaCombatEffectData> effects, HashSet<ArenaCombatEffectData> visited)
    {
        if (effect == null || effects.Count >= MaxAttackTypeIcons || !visited.Add(effect))
            return;

        effects.Add(effect);
        CollectAttackTypes(effect.OnHitEffect, effects, visited);
        CollectAttackTypes(effect.OnExpireEffect, effects, visited);
    }

    private static Color GetConditionColor(float condition)
    {
        if (condition >= 0.7f)
            return new Color(0.24f, 0.78f, 0.32f);

        if (condition >= 0.35f)
            return new Color(0.92f, 0.72f, 0.18f);

        return new Color(0.9f, 0.22f, 0.16f);
    }
}
