using Godot;
using Godot.Collections;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Actions;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Arena.Combat.Effects;

public sealed class ArenaCombatEffectContext
{
    public ArenaCombatant Source { get; init; }
    public ArenaCombatTeam SourceTeam { get; init; } = ArenaCombatTeam.Neutral;
    public ItemData SourceItem { get; init; }
    public CombatDamageData ItemDamage { get; init; }
    public ArenaCombatActionData Action { get; init; }
    public ArenaCombatEffectData Effect { get; init; }
    public Vector2 Direction { get; init; } = Vector2.Right;
    public float BuildupScalar { get; init; } = 1f;
    public int ChainDepth { get; init; }
    public int MaxChainDepth { get; init; } = ArenaCombatActionData.DefaultMaxChainDepth;

    public string ActionName => string.IsNullOrWhiteSpace(Action?.DisplayName) ? "UnnamedAction" : Action.DisplayName;

    public ArenaCombatEffectContext WithEffect(ArenaCombatEffectData effect)
    {
        return new ArenaCombatEffectContext
        {
            Source = Source,
            SourceTeam = SourceTeam,
            SourceItem = SourceItem,
            ItemDamage = ItemDamage,
            Action = Action,
            Effect = effect,
            Direction = Direction,
            BuildupScalar = BuildupScalar,
            ChainDepth = ChainDepth + 1,
            MaxChainDepth = MaxChainDepth
        };
    }

    public float ScaleRange(float value)
    {
        return Action?.Buildup?.ScaleRange == true ? value * GetClampedBuildupScalar() : value;
    }

    public float ScaleSpeed(float value)
    {
        return Action?.Buildup?.ScaleSpeed == true ? value * GetClampedBuildupScalar() : value;
    }

    public CombatDamageData ScaleDamage(CombatDamageData damage)
    {
        if (damage == null || Action?.Buildup?.ScaleDamage != true)
            return damage;

        var scaledDamage = new CombatDamageData();
        foreach (var entry in damage.Entries ?? new Array<CombatDamageEntryData>())
        {
            if (entry == null)
                continue;

            var scaledEntry = new CombatDamageEntryData();
            scaledEntry.Set("Type", (int)entry.Type);
            scaledEntry.Set("Damage", Mathf.RoundToInt(entry.Damage * GetClampedBuildupScalar()));
            scaledDamage.Entries.Add(scaledEntry);
        }

        return scaledDamage;
    }

    private float GetClampedBuildupScalar()
    {
        return Mathf.Clamp(BuildupScalar, ArenaCombatBuildupData.MinScalar, ArenaCombatBuildupData.MaxScalar);
    }

    public override string ToString()
    {
        var sourceName = Source?.Name ?? "NoSource";
        return $"Context[Action={ActionName}, Source={sourceName}, Team={SourceTeam}, Direction={Direction}, Buildup={BuildupScalar:0.##}, Chain={ChainDepth}/{MaxChainDepth}]";
    }
}
