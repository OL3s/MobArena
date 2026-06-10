using Godot;

namespace MobArena.Scenes.UI;

public partial class StoreDetailRow : HBoxContainer
{
    private Label _label;
    private Label _value;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _value = GetNode<Label>("Value");
    }

    public void Configure(string label, string value)
    {
        if (!IsNodeReady())
            return;

        _label.Text = label;
        _value.Text = value;
    }
}
