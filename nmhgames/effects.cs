using DeadworksManaged.Api;
using DeadworksManaged.Api.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using ITimer = DeadworksManaged.Api.ITimer;






public static class Effects
{
    private static void SendDrawNotification(CCitadelPlayerController controller, EffectInfo effect)
    {
        Chat.PrintToChat(controller, "你抽到了");
        Chat.PrintToChat(controller, $"{effect.DisplayName} (持续 {effect.Duration} 秒)");
    }

    // 把 ITimer timer 改成 DeadworksManaged.Api.ITimer timer
public static void Execute(EffectInfo effect, CCitadelPlayerPawn pawn, ITimer timer)
    {
        if (pawn == null) return;
        
        var controller = pawn.Controller;
        if (controller == null) return;

        
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		
		// ================= 效果 1：加速 =================
        if (effect.Id == "speed_boost")
        {
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration); 

            pawn.AddModifier("upgrade_fleetfoot_boots/modifier_intrinsic_base", kv: kv,
            abilityValues: new() { 
                ["MoveWhileShootingSpeedPenaltyReductionPercent"] = 200f,
                ["MoveWhileZoomedSpeedPenaltyReductionPercent"] = 200f,
                ["SlideScale"] = 299f,
                ["BulletResist"] = 15f,
                ["BaseAttackDamagePercent"] = 15f,
            });

            pawn.AddModifier("upgrade_fleetfoot_boots/modifier_fleetfoot_boots", kv: kv,
            abilityValues: new() { 
                ["ActiveBonusMoveSpeed"] = 10f,
                ["SlowResistancePercent"] = 200f,
            });

            pawn.AddModifier("modifier_hero_testing_fast_movespeed", kv);

            SendDrawNotification(controller, effect);
        }












		
        // ================= 效果 2：1v1 斗殴 =================
        else if (effect.Id == "1v1")
        {
            var enemies = Players.GetAll()
                .Where(c => c != null && c.IsValid && c.Slot != controller.Slot)
                .Select(c => c.GetHeroPawn())
                .Where(p => p != null && p.IsValid && p.TeamNum != pawn.TeamNum)
                .ToList();

            var playerB = enemies.Count > 0 ? enemies[Random.Shared.Next(enemies.Count)] : null;
            var controllerB = playerB?.Controller;
            string nameB = controllerB?.PlayerName ?? "未知敌人";

            Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);
            
            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "⚠️ 做好准备",
                DescriptionLocstring = $"{controller.PlayerName} 与 {nameB} 将要发起决斗！"
            };
            NetMessages.Send(msg, RecipientFilter.All);
            
            Chat.PrintToChatAll($"⚠️ 做好准备，{controller.PlayerName} 将对 {nameB} 发起决斗！");

            timer.Once(3.Seconds(), () =>
            {
                Console.WriteLine("[1v1] 决斗开始，施加时空扭曲");
                if (pawn != null && pawn.IsValid && pawn.LifeState == LifeState.Alive)
                {
                    using var kvA = new KeyValues3();
                    kvA.SetFloat("duration", 1.0f);
                    pawn.AddModifier("modifier_chrono_swap_bubble_move", kvA);
                }
                if (playerB != null && playerB.IsValid && playerB.LifeState == LifeState.Alive)
                {
                    using var kvB = new KeyValues3();
                    kvB.SetFloat("duration", 1.0f);
                    playerB.AddModifier("modifier_chrono_swap_bubble_move", kvB);
                }

                timer.Once(1.Seconds(), () =>
                {
                    Console.WriteLine("[1v1] 移除时空扭曲");
                    if (pawn != null && pawn.IsValid) pawn.RemoveModifier("modifier_chrono_swap_bubble_move");
                    if (playerB != null && playerB.IsValid) playerB.RemoveModifier("modifier_chrono_swap_bubble_move");
                });
            });

        }
        // ================= 未知效果保底 =================















		
        else
        {
            Console.WriteLine($"[Effects] 未知的效果ID: {effect.Id}");
        }
    }
}
