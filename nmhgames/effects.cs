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
kv.SetFloat("duration", 60f);

// 3) 关键：key 必须是这个 modifier 自己读取的属性名
var mod = pawn.AddModifier(
    "upgrade_fleetfoot_boots/modifier_fleetfoot_boots",
    new Dictionary<string, float>
    {
        ["ActiveBonusMoveSpeed"]   = 15f,    // 默认 3.0，先别写太夸张（引擎有移速上限，20 可能被 clamp）
        ["SlowResistancePercent"]  = 100f,  // 默认 40
    },
    kv);
    
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
