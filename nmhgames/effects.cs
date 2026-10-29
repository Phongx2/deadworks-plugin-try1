using DeadworksManaged.Api;
using System;
using System.Collections.Generic;

public static class Effects
{
	private static void SendDrawNotification(CCitadelPlayerController controller, EffectInfo effect)
    {
        // 第一行：固定提示
        Chat.PrintToChat(controller, "你抽到了");
        
        // 第二行：效果名 + 持续时长
        Chat.PrintToChat(controller, $"{effect.DisplayName} ，(持续 {effect.Duration} 秒)");
    }





	
    // 参数变成了 EffectInfo
    public static void Execute(EffectInfo effect, CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return;
        
        // 提前获取 controller 并判空，解决警告
        var controller = pawn.Controller;
        if (controller == null) return;

        // 使用 effect.Id 进行判断
        if (effect.Id == "speed_boost")
        {
            // 1. 设置时长 (使用 effect.Duration 替代写死的 10.0f)
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration); 

            // 2. 套用你测试好的格式
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

            // 测试用的移速 Modifier (如果你的 kv 是给上面两个用的，这里可以直接传，或者单独 new 一个)
            pawn.AddModifier("modifier_hero_testing_fast_movespeed", kv);

            // 聊天提示 (使用 effect.DisplayName)
            SendDrawNotification(controller, effect);
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
