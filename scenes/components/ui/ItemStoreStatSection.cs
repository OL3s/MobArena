using Godot;

namespace MobArena.Scenes.Components.UI;

public partial class ItemStoreStatSection : VBoxContainer
{
    private Label _titleLabel;
    private VBoxContainer _rows;

    public override void _Ready()
    {
        _titleLabel = GetNode<Label>("TitleLabel");
        _rows = GetNode<VBoxContainer>("Rows");
    }

    public void Configure(string title)
    {
        if (!IsNodeReady())
            return;

        _titleLabel.Text = title;
    }

    public void AddRow(Control row)
    {
        _rows?.AddChild(row);
    }
}
