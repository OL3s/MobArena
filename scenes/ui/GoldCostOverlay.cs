using Godot;
using System.Collections.Generic;
using MobArena.Scripts;
using MobArena.Scenes.Components.Town;
using MobArena.Scripts.Resources;

namespace MobArena.Scenes.UI;

public partial class GoldCostOverlay : Control
{
    private SaveNode _saveNode;
    private CompanyRunData _runData;
    private TownPhaseState _phaseState;
    private Label _phaseLabel;
    private PanelContainer _gladiatorPanel;
    private PanelContainer _buildingPanel;
    private VBoxContainer _gladiatorRows;
    private VBoxContainer _buildingRows;
    private VBoxContainer _resultRows;
    private Texture2D _goldIcon;

    public override void _Ready()
    {
        _saveNode = SaveNode.Get();
        _runData = _saveNode.CompanyRunData;
        _phaseState = _saveNode.TownPhaseState;

        _phaseLabel = GetNode<Label>("CenterContainer/PopupPanel/MarginContainer/Content/PhaseLabel");
        _gladiatorPanel = GetNode<PanelContainer>("CenterContainer/PopupPanel/MarginContainer/Content/CostColumns/GladiatorPanel");
        _buildingPanel = GetNode<PanelContainer>("CenterContainer/PopupPanel/MarginContainer/Content/CostColumns/BuildingPanel");
        _gladiatorRows = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/CostColumns/GladiatorPanel/GladiatorRows");
        _buildingRows = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/CostColumns/BuildingPanel/BuildingRows");
        _resultRows = GetNode<VBoxContainer>("CenterContainer/PopupPanel/MarginContainer/Content/CostColumns/ResultPanel/ResultRows");
        _goldIcon = GetNode<TextureRect>("CenterContainer/PopupPanel/MarginContainer/Content/Header/GoldIcon").Texture;
        GetNode<Button>("CenterContainer/PopupPanel/MarginContainer/Content/CloseButton").Pressed += QueueFree;

        RefreshUi();
    }

    private void RefreshUi()
    {
        _phaseLabel.Text = $"Current phase: {_phaseState.GetPhaseLabel()}";

        ClearRows(_gladiatorRows);
        ClearRows(_buildingRows);
        ClearRows(_resultRows);

        var sources = GetPhaseGoldCostSources();
        var gladiatorLines = new List<PhaseGoldCostLine>();
        var buildingLines = new List<PhaseGoldCostLine>();
        if (sources.Count <= 0)
        {
            foreach (var line in _runData.GetCurrentPhaseGoldCostLines())
            {
                if (line.Timing == PhaseGoldCostTiming.NightToDay)
                    gladiatorLines.Add(line);
                else
                    buildingLines.Add(line);
            }
        }
        else
        {
            foreach (var source in sources)
            {
                foreach (var line in source.GetPhaseGoldCostLines(_runData, _phaseState))
                {
                    if (source.PhaseGoldCostSection == "Gladiators")
                        gladiatorLines.Add(line);
                    else
                        buildingLines.Add(line);
                }
            }
        }

        var gladiatorTotal = AddCostPanelRows(_gladiatorRows, gladiatorLines, "Gladiator Cost");
        var buildingTotal = AddCostPanelRows(_buildingRows, buildingLines, "Building Cost");
        RefreshCostPanel(_gladiatorPanel, _gladiatorRows, gladiatorTotal);
        RefreshCostPanel(_buildingPanel, _buildingRows, buildingTotal);
        RefreshResultPanel(gladiatorTotal + buildingTotal);
    }

    private List<IPhaseGoldCostSource> GetPhaseGoldCostSources()
    {
        var sources = new List<IPhaseGoldCostSource>();
        foreach (var node in GetTree().GetNodesInGroup(RosterYard.PhaseGoldCostSourceGroup))
        {
            if (node is IPhaseGoldCostSource source)
                sources.Add(source);
        }

        sources.Sort((left, right) =>
        {
            var order = left.PhaseGoldCostDisplayOrder.CompareTo(right.PhaseGoldCostDisplayOrder);
            return order != 0 ? order : string.CompareOrdinal(left.PhaseGoldCostSection, right.PhaseGoldCostSection);
        });
        return sources;
    }

    private int AddCostPanelRows(VBoxContainer rows, List<PhaseGoldCostLine> lines, string title)
    {
        var total = 0;
        AddHeader(rows, title);
        foreach (var line in lines)
        {
            var cost = line.GetCostForPhase(_phaseState);
            if (cost <= 0)
                continue;

            AddCostRow(rows, line.Label, cost);
            total += cost;
        }

        return total;
    }

    private void RefreshCostPanel(PanelContainer panel, VBoxContainer rows, int total)
    {
        panel.Visible = total > 0;
        if (total <= 0)
        {
            ClearRows(rows);
            return;
        }

        AddBottomSpacer(rows);
        rows.AddChild(new HSeparator());
        AddGoldCostRow(rows, "Total", total);
    }

    private void RefreshResultPanel(int total)
    {
        var result = _runData.Gold - total;
        AddHeader(_resultRows, "Payment Result");
        AddCostRow(_resultRows, "Treasury", _runData.Gold);
        AddCostRow(_resultRows, "Cost", total);
        AddBottomSpacer(_resultRows);
        _resultRows.AddChild(new HSeparator());
        AddGoldCostRow(_resultRows, "After Payment", result, result >= 0 ? Colors.White : new Color(1f, 0.45f, 0.35f));
    }

    private void AddHeader(VBoxContainer rows, string text)
    {
        var label = new Label
        {
            Text = text,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        label.AddThemeFontSizeOverride("font_size", 16);
        rows.AddChild(label);
    }

    private void AddGoldCostRow(VBoxContainer rows, string label, int cost)
    {
        AddGoldCostRow(rows, label, cost, Colors.White);
    }

    private void AddGoldCostRow(VBoxContainer rows, string label, int cost, Color valueColor)
    {
        var row = CreateCostRow(label);
        var valueBox = new HBoxContainer
        {
            CustomMinimumSize = new Vector2(92, 0),
            Alignment = BoxContainer.AlignmentMode.End
        };
        valueBox.AddThemeConstantOverride("separation", 4);
        valueBox.AddChild(new TextureRect
        {
            CustomMinimumSize = new Vector2(20, 20),
            Texture = _goldIcon,
            ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered
        });
        valueBox.AddChild(new Label
        {
            Text = cost.ToString(),
            HorizontalAlignment = HorizontalAlignment.Right,
            Modulate = valueColor
        });
        row.AddChild(valueBox);
        rows.AddChild(row);
    }

    private void AddCostRow(VBoxContainer rows, string label, int cost)
    {
        var row = CreateCostRow(label);
        row.AddChild(new Label
        {
            Text = cost.ToString(),
            CustomMinimumSize = new Vector2(80, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        });
        rows.AddChild(row);
    }

    private static HBoxContainer CreateCostRow(string label)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);

        row.AddChild(new Label
        {
            Text = label,
            SizeFlagsHorizontal = SizeFlags.ExpandFill
        });

        return row;
    }

    private static void ClearRows(VBoxContainer rows)
    {
        foreach (var child in rows.GetChildren())
            child.QueueFree();
    }

    private static void AddBottomSpacer(VBoxContainer rows)
    {
        rows.AddChild(new Control
        {
            SizeFlagsVertical = SizeFlags.ExpandFill
        });
    }
}
