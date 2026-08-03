using DeadworksManaged.Api;
using System.Numerics;

namespace RollTheDicePlugin;

public class RollTheDicePlugin : DeadworksPluginBase {
    public override string Name => "Roll The Dice";

    private static readonly Random _rng = new();

    public override void OnLoad(bool isReload) => Console.WriteLine(isReload ? "RTD reloaded!" : "RTD loaded!");
    public override void OnUnload() => Console.WriteLine("RTD unloaded!");

    public override void OnPrecacheResources() {
        Precache.AddResource("particles/upgrades/mystical_piano_hit.vpcf");
    }

    [Command("rtd", Description = "Roll a random effect on yourself")]
    public void CmdRollTheDice(CCitadelPlayerController caller) {
        var pawn = caller.GetHeroPawn();
        if (pawn == null) return;

        var effects = new (string Name, Action<CCitadelPlayerPawn> Apply)[] {
            ("Mystical Piano Strike", ApplyPianoStrike)
        };

        var roll = effects[_rng.Next(effects.Length)];

        var msg = new CCitadelUserMsg_HudGameAnnouncement {
            TitleLocstring = "ROLL THE DICE",
            DescriptionLocstring = roll.Name
        };
        NetMessages.Send(msg, RecipientFilter.Single(caller.EntityIndex - 1));

        roll.Apply(pawn);
    }

    private void ApplyPianoStrike(CCitadelPlayerPawn pawn) {
        pawn.EmitSound("Mystical.Piano.AOE.Warning");
        Timer.Once(1700.Milliseconds(), () => {
            var particle = CParticleSystem.Create("particles/upgrades/mystical_piano_hit.vpcf")
                .AtPosition(pawn.Position + Vector3.UnitZ * 100)
                .StartActive(true)
                .Spawn();

            pawn.EmitSound("Mystical.Piano.AOE.Explode");
            using var kv = new KeyValues3();
            kv.SetFloat("duration", 3.0f);
            pawn.AddModifier("modifier_citadel_knockdown", kv);

            if (particle != null) {
                Timer.Once(5.Seconds(), () => particle.Destroy());
            }
        });
    }
}
