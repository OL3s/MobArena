using Godot;

namespace MobArena.Scenes.UI;

public partial class GoldCostLine : HBoxContainer
{
    private Label _label;
    private HBoxContainer _goldValue;
    private TextureRect _goldIcon;
    private Label _value;
    private Label _plainValue;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _goldValue = GetNode<HBoxContainer>("GoldValue");
        _goldIcon = GetNode<TextureRect>("GoldValue/GoldIcon");
        _value = GetNode<Label>("GoldValue/Value");
        _plainValue = GetNode<Label>("PlainValue");
    }

    public void Configure(string label, string value, Texture2D goldIcon = null, Color? valueColor = null)
    {
        if (!IsNodeReady())
            return;

        _label.Text = label;
        _goldValue.Visible = goldIcon != null;
        _plainValue.Visible = goldIcon == null;
        _goldIcon.Texture = goldIcon;
        _value.Text = value;
        _plainValue.Text = value;
        _value.Modulate = valueColor ?? Colors.White;
        _plainValue.Modulate = valueColor ?? Colors.White;
    }
}
