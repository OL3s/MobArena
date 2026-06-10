using Godot;

namespace MobArena.Scenes.UI;

public partial class CompletedCompanyRecordButton : Button
{
    [Signal]
    public delegate void RecordPressedEventHandler(int index);

    private int _index = -1;

    public override void _Ready()
    {
        Pressed += OnPressed;
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
    }

    public void Configure(int index, string companyName, int finalFame)
    {
        _index = index;
        Text = $"{index + 1}. {companyName}\nFame {finalFame}";
    }

    private void OnPressed()
    {
        EmitSignal(SignalName.RecordPressed, _index);
    }
}
