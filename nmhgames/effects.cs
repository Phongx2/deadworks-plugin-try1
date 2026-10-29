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
    // 1. 设置时长
    using var kv = new KeyValues3();
    kv.SetFloat("duration", 10.0f); 

    // 2. 使用命名参数传递（注意 abilityValues: 这个前缀）
    pawn.AddModifier("ability_doorman_bomb/debuff", kv: kv,
    abilityValues: new() { ["SlowPercent"] = 100.0f });
    
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
