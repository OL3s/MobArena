using Godot;
using MobArena.Scripts.Resources.Combat;
using MobArena.Scripts.Resources.Combat.Actions;
using MobArena.Scripts.Resources.Combat.Effects;
using MobArena.Scripts.Resources.Items;

namespace MobArena.Scenes.Components.Arena.Effects;

public sealed class ArenaCombatEffectContext
{
    public ArenaCombatant Source { get; init; }
    public ArenaCombatTeam SourceTeam { get; init; } = ArenaCombatTeam.Neutral;
    public ItemData SourceItem { get; init; }
    public CombatDamageData ItemDamage { get; init; }
    public ArenaCombatActionData Action { get; init; }
    public ArenaCombatEffectData Effect { get; init; }
    public Vector2 Direction { get; init; } = Vector2.Right;
}
