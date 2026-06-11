using Godot;
using MobArena.Scenes.Components.UI;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class CompletedCompanyRecordButton : Button
{
    [Signal]
    public delegate void RecordPressedEventHandler(int index);

    private int _index = -1;
    private CompanyLogo _companyLogo;
    private Label _companyNameLabel;
    private Label _fameLabel;

    public override void _Ready()
    {
        EnsureNodes();
        Pressed += OnPressed;
    }

    public override void _ExitTree()
    {
        Pressed -= OnPressed;
    }

    public void Configure(int index, CompanyLogoData companyLogoData, string companyName, int finalFame)
    {
        EnsureNodes();

        _index = index;
        Text = string.Empty;
        _companyLogo.SetLogoData(companyLogoData);
        _companyNameLabel.Text = $"{index + 1}. {companyName}";
        _fameLabel.Text = $"Fame {finalFame}";
    }

    private void EnsureNodes()
    {
        _companyLogo ??= GetNode<CompanyLogo>("Content/Logo");
        _companyNameLabel ??= GetNode<Label>("Content/Text/CompanyName");
        _fameLabel ??= GetNode<Label>("Content/Text/Fame");

        _companyLogo.MouseFilter = MouseFilterEnum.Ignore;
        foreach (var child in _companyLogo.GetChildren())
        {
            if (child is Control control)
                control.MouseFilter = MouseFilterEnum.Ignore;
        }
    }

    private void OnPressed()
    {
        EmitSignal(SignalName.RecordPressed, _index);
    }
}
