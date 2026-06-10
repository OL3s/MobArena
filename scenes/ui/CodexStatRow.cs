using Godot;

namespace MobArena.Scenes.UI;

public partial class CodexStatRow : HBoxContainer
{
    private Label _label;
    private Label _value;
    private string _configuredLabel = string.Empty;
    private string _configuredValue = string.Empty;

    public override void _Ready()
    {
        _label = GetNode<Label>("Label");
        _value = GetNode<Label>("Value");
        RefreshUi();
    }

    public void Configure(string label, string value)
    {
        _configuredLabel = label;
        _configuredValue = value;
        RefreshUi();
    }

    private void RefreshUi()
    {
        if (!IsNodeReady())
            return;

        _label.Text = _configuredLabel;
        _value.Text = _configuredValue;
    }
}
