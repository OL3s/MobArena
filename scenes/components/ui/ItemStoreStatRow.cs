using Godot;

namespace MobArena.Scenes.Components.UI;

public partial class ItemStoreStatRow : HBoxContainer
{
    private Label _label;
    private Label _value;
    private string _pendingLabel = "Label";
    private string _pendingValue = "Value";

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _value = GetNode<Label>("Value");
        RefreshUi();
    }

    public void Configure(string label, string value)
    {
        _pendingLabel = label;
        _pendingValue = value;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _label.Text = _pendingLabel;
        _value.Text = _pendingValue;
    }
}
