using DeadworksManaged.Api;
using DeadworksManaged.Api.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;







public static class Effects
{
	private static void SendDrawNotification(CCitadelPlayerController controller, EffectInfo effect)
    {
        // 第一行：固定提示
        Chat.PrintToChat(controller, "你抽到了");
        
        // 第二行：效果名 + 持续时长
        Chat.PrintToChat(controller, $"{effect.DisplayName} ，(持续 {effect.Duration} 秒)");
    }

public static void Execute(EffectInfo effect, CCitadelPlayerPawn pawn, ITimer timer)
    {
        if (pawn == null) return;
        var controller = pawn.Controller;
        if (controller == null) return;

        if (effect.Id == "speed_boost")
        {



	
    // 参数变成了 EffectInfo
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
            Chat.PrintToChatAll($"⚠️ 做好准备，我将对 {nameB} 发起决斗！");

            // 🌟 使用传进来的 timer，而不是直接写 Timer.Once
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



















		        else if (effect.Id == "1v1")
        {
            // 获取执行者（玩家 A）
            var playerA = pawn;
            var controllerA = controller;

            // 1. 尝试在敌方队伍随机找一位玩家（玩家 B）
            var enemies = Players.GetAll()
                .Where(c => c != null && c.IsValid && c.Slot != controllerA.Slot)
                .Select(c => c.GetHeroPawn())
                .Where(p => p != null && p.IsValid && p.TeamNum != playerA.TeamNum)
                .ToList();

            // 没找到敌方也继续执行，playerB 为 null
            var playerB = enemies.Count > 0 ? enemies[Random.Shared.Next(enemies.Count)] : null;
            var controllerB = playerB?.Controller;
            string nameB = controllerB?.PlayerName ?? "未知敌人";

            // 2. 播放全局音效
            Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

            // 3. 给所有玩家显示 HUD
            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "⚠️ 做好准备",
                DescriptionLocstring = $"{controllerA.PlayerName} 与 {nameB} 将要发起决斗！"
            };
            NetMessages.Send(msg, RecipientFilter.All);

            // 4. 给执行者发所有人可见的聊天（这里用控制台广播模拟所有人可见，或者用 PrintToChatAll）
            Chat.PrintToChatAll($"⚠️ 做好准备，我将对 {nameB} 发起决斗！");

            // 5. 延迟 3 秒后执行时空扭曲（逐 tick 机制由 Modifier 自己处理，这里只负责施加和移除）
            Timer.Once(3.Seconds(), () =>
            {
                Console.WriteLine("[1v1] 决斗开始，施加时空扭曲效果");

                // 如果 playerA 存活，给他挂 Buff
                if (playerA != null && playerA.IsValid && playerA.LifeState == LifeState.Alive)
                {
                    using var kvA = new KeyValues3();
                    kvA.SetFloat("duration", 1.0f);
                    playerA.AddModifier("modifier_chrono_swap_bubble_move", kvA);
                }

                // 如果 playerB 存在且存活，给他挂 Buff
                if (playerB != null && playerB.IsValid && playerB.LifeState == LifeState.Alive)
                {
                    using var kvB = new KeyValues3();
                    kvB.SetFloat("duration", 1.0f);
                    playerB.AddModifier("modifier_chrono_swap_bubble_move", kvB);
                }

                // 再过 1 秒移除 buff
                Timer.Once(1.Seconds(), () =>
                {
                    Console.WriteLine("[1v1] 移除时空扭曲效果");
                    
                    if (playerA != null && playerA.IsValid)
                        playerA.RemoveModifier("modifier_chrono_swap_bubble_move");
                    
                    if (playerB != null && playerB.IsValid)
                        playerB.RemoveModifier("modifier_chrono_swap_bubble_move");
                });
            });

        }















		
        // 以后加新效果：
        // else if (effect.Id == "give_gold")
        // {
        //     ...
        // }
        else
        {
            Console.WriteLine($"[Effects] 未知的效果ID: {effect.Id}");
        }
    }
}
