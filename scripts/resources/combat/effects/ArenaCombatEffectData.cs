using Godot;

namespace MobArena.Scripts.Resources.Combat.Effects;

[GlobalClass]
public partial class ArenaCombatEffectData : Resource
{
	public virtual string AttackTypeLabel => "Effect";

	public virtual string AttackTypeIconPath => "res://assets/ui/icons/question_mark.svg";

	[Export(PropertyHint.File, "*.tscn")]
	public string ScenePath { get; private set; } = string.Empty;

    [Export]
    public ArenaCombatApplyData Apply { get; private set; }

    [Export]
    public ArenaCombatEffectData OnHitEffect { get; private set; }

    [Export]
    public ArenaCombatEffectData OnExpireEffect { get; private set; }

	[Export(PropertyHint.File, "*.tscn")]
	public string OnHitScenePath { get; private set; } = string.Empty;

	[Export(PropertyHint.File, "*.tscn")]
	public string OnExpireScenePath { get; private set; } = string.Empty;

    [Export(PropertyHint.Range, "0.01,30,0.01")]
    public float LifetimeSeconds { get; private set; } = 0.16f;

    [Export(PropertyHint.Range, "1,100,1")]
    public int MaxHits { get; private set; } = 1;

	[Export]
	public bool CanHitSameTargetMultipleTimes { get; private set; }

	public PackedScene Scene => LoadPackedScene(ScenePath);

	public PackedScene OnHitScene => LoadPackedScene(OnHitScenePath);

	public PackedScene OnExpireScene => LoadPackedScene(OnExpireScenePath);

	private static PackedScene LoadPackedScene(string scenePath)
	{
		return string.IsNullOrWhiteSpace(scenePath) ? null : ResourceLoader.Load<PackedScene>(scenePath);
	}

    public override string ToString()
    {
        var sceneLabel = string.IsNullOrWhiteSpace(ScenePath) ? "Scene=None" : ScenePath.GetFile().GetBaseName();
        return $"{AttackTypeLabel}:{sceneLabel}";
    }

    public string ToStringExtended()
    {
        var sceneLabel = string.IsNullOrWhiteSpace(ScenePath) ? "Scene=None" : ScenePath.GetFile().GetBaseName();
        var applyLabel = Apply == null ? "Apply=None" : Apply.ToStringExtended();
        return $"{GetType().Name}[{sceneLabel}, Lifetime={LifetimeSeconds:0.##}, MaxHits={MaxHits}, MultiHit={CanHitSameTargetMultipleTimes}, {applyLabel}, OnHitEffect={OnHitEffect != null}, OnExpireEffect={OnExpireEffect != null}, OnHitScene={!string.IsNullOrWhiteSpace(OnHitScenePath)}, OnExpireScene={!string.IsNullOrWhiteSpace(OnExpireScenePath)}]";
    }
}
