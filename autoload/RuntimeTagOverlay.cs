using Godot;
using System.Collections.Generic;

namespace MobArena.Scripts;

public partial class RuntimeTagOverlay : CanvasLayer
{
    private Label _tagLabel;
    private string _lastTagText = string.Empty;

    public string TagText => _lastTagText;

    public override void _Ready()
    {
        _tagLabel = GetNode<Label>("Panel/MarginContainer/TagLabel");
        ProcessMode = ProcessModeEnum.Always;
        RefreshTags();
    }

    public override void _Process(double delta)
    {
        RefreshTags();
    }

    private void RefreshTags()
    {
        var tagText = BuildTagText();
        if (_lastTagText == tagText)
            return;

        _lastTagText = tagText;
        Visible = !string.IsNullOrWhiteSpace(tagText);
        if (_tagLabel != null)
            _tagLabel.Text = tagText;
    }

    private static string BuildTagText()
    {
        var tags = new List<string>();
        var saveNode = SaveNode.Get();
        var settings = saveNode?.SettingsConfig;

        if (settings?.ShowRuntimeTags != true)
            return string.Empty;

        if (settings?.IsDemo == true)
            tags.Add("Demo");
        if (settings?.DebugEnabled == true)
            tags.Add("Debug");
        if (OS.IsDebugBuild())
            tags.Add("(dev)");

        return string.Join("\n", tags);
    }
}
