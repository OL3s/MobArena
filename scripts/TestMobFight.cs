using Godot;
using MobArena.Scenes.Components.Arena;
using MobArena.Scenes.Components.Arena.CombatUi;
using MobArena.Scripts.Resources;
using MobArena.Scripts.Resources.Items;
using MobArena.Scripts.Resources.Mobs;

namespace MobArena.Scripts;

public partial class TestMobFight : Node
{
    private const string DummyMobPath = "res://resources/mobs/training_dummy.tres";
    private const string TrainingSwordPath = "res://resources/items/main_hand/training_sword.tres";

    public override void _Ready()
    {
        var player = ConfigurePlayer();
        var dummy = ConfigureDummy();
        ConfigureHud(player, dummy);
    }

    private PlayerCombatant ConfigurePlayer()
    {
        var player = GetNodeOrNull<PlayerCombatant>("World/PlayerCombatant");
        if (player == null)
            return null;

        var gladiator = GladiatorData.CreateDefault();
        gladiator.SetGladiatorName("Keyboard Tester");
        gladiator.Equipment.EquipMainHand(ResourceLoader.Load<MainHandItemData>(TrainingSwordPath));

        var controller = LocalInputControllerConfig.Create(LocalInputControllerConfig.ControllerKind.Keyboard, -1, null);
        player.ConfigureGladiator(gladiator, ArenaControlAssignmentData.Create(gladiator, controller));
        return player;
    }

    private EnemyCombatant ConfigureDummy()
    {
        var dummy = GetNodeOrNull<EnemyCombatant>("World/TrainingDummy");
        if (dummy == null)
            return null;

        dummy.ConfigureEnemy(ResourceLoader.Load<EnemyMobData>(DummyMobPath));
        return dummy;
    }

    private void ConfigureHud(PlayerCombatant player, EnemyCombatant dummy)
    {
        var hud = GetNodeOrNull<CombatHud>("CombatHud");
        if (hud == null)
            return;

        hud.SetPlayers(player);
        hud.SetBoss(dummy);
    }
}
