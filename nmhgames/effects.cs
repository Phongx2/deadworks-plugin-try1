using DeadworksManaged.Api;
using System;

public static class Effects
{
    public static void Execute(string effectName, CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return;
        
        // 提前获取 controller 并判空，解决警告
        var controller = pawn.Controller;
        if (controller == null) return;

        switch (effectName)
        {
            case EffectList.SpeedBoost:
            {
                // 用大括号包裹整个 case 解决 CS8647
                using var kv = new KeyValues3();
                kv.SetFloat("duration", 30.0f); 
                
                // 把 kv 也改成命名参数 kv: kv，解决 CS1744
                pawn.AddModifier(
                    "ability_familiar_attach/speed",
                    kv: kv,
                    abilityValues: new() { ["BonusMoveSpeed"] = 12.0f } 
                );
                
                Chat.PrintToChat(controller, "你抽到了");
                Chat.PrintToChat(controller, EffectList.SpeedBoostName);
                break;
            }

            default:
                Console.WriteLine($"[Effects] 未知的效果名称: {effectName}");
                break;
        }
    }
}
