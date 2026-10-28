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
    using var kv = new KeyValues3();
    kv.SetFloat("duration", 30.0f); 
    
    // 使用 SpeedBoost modifier，并覆写具体的移速加成数值
    pawn.AddModifier(
        "modifier_citadel_speed_boost", 
        kv: kv,
        abilityValues: new() { ["m_flMoveSpeedBoost"] = 25.0f } // 3 m/s，可按需调整
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
