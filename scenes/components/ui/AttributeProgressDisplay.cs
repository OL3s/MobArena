using Godot;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.Components.UI;

public partial class AttributeProgressDisplay : Control
{
    private Control _fill;
    private Label _label;

    public override void _Ready()
    {
        _fill = GetNode<Control>("Line/Fill");
        _label = GetNode<Label>("Label");
    }

    public void Configure(GladiatorLevelData levelData, GladiatorLevelData.AttributeKind attributeKind)
    {
        if (!IsNodeReady())
            return;

        var level = levelData?.GetAttributeLevel(attributeKind) ?? 1;
        var progress = levelData?.GetAttributeLevelProgress(attributeKind) ?? 0f;
        Configure(GetAttributeAbbreviation(attributeKind), level, progress);
    }

    public void Configure(string attributeName, int level, float progress)
    {
        if (!IsNodeReady())
            return;

        progress = Mathf.Clamp(progress, 0f, 1f);
        _label.Text = $"{attributeName} {Mathf.Max(1, level)}";
        _fill.AnchorRight = progress;
        _fill.OffsetRight = 0f;
    }

    private static string GetAttributeAbbreviation(GladiatorLevelData.AttributeKind attributeKind)
    {
        return attributeKind switch
        {
            GladiatorLevelData.AttributeKind.Agility => "Agi",
            GladiatorLevelData.AttributeKind.Vitality => "Vit",
            GladiatorLevelData.AttributeKind.Endurance => "End",
            _ => "Str"
        };
    }
}
