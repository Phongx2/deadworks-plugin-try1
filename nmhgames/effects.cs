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
    kv.SetFloat("duration", 30.0f); // 持续 30 秒

    // 这里直接传入字典
    pawn.AddModifier(
        "modifier_fleetfoot_boots",
        new Dictionary<string, float> {
            ["ActiveBonusMoveSpeed"] = 15.0f 
        },
        kv // 把 kv 传在最后
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
