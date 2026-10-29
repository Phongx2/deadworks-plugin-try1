using DeadworksManaged.Api;
using System;
using System.Collections.Generic;

public static class Effects
{
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
            Chat.PrintToChat(controller, "你抽到了");
            Chat.PrintToChat(controller, effect.DisplayName);
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
