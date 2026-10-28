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
    // 1. 创建并设置持续时间
    using var kv = new KeyValues3();
    kv.SetFloat("duration", 5.0f); 

    // 2. 使用命名参数传递，确保 kv 和 属性值都被接收
    pawn.AddModifier("upgrade_fleetfoot_boots/modifier_fleetfoot_boots",kv, new Dictionary<string, float> {
    ["ActiveBonusMoveSpeed"] = 25f,
    ["SlowResistancePercent"] = 40f,
});
    
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
