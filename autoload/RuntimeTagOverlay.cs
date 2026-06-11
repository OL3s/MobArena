using Godot;
using System.Collections.Generic;

namespace MobArena.Scripts;

public partial class RuntimeTagOverlay : CanvasLayer
{
    private Label _tagLabel;
    private SaveNode _saveNode;
    private string _lastTagText = string.Empty;

    public string TagText => _lastTagText;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        if (_saveNode != null)
            _saveNode.RuntimeTagsChanged += RefreshTags;

        _tagLabel = GetNode<Label>("Panel/MarginContainer/TagLabel");
        ProcessMode = ProcessModeEnum.Always;
        RefreshTags();
    }

    public override void _ExitTree()
    {
        if (_saveNode != null)
            _saveNode.RuntimeTagsChanged -= RefreshTags;
    }

    private void RefreshTags()
    {
        var tagText = BuildTagText();
        Visible = !string.IsNullOrWhiteSpace(tagText);

        if (_lastTagText == tagText)
            return;

        _lastTagText = tagText;
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
        if (settings?.DevEnabled == true)
            tags.Add("Dev");
        if (OS.IsDebugBuild())
            tags.Add("(debug build)");

        return string.Join("\n", tags);
    }
}
