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
    
    // 直接挂载 Fleetfoot 原版 Modifier
    // ActiveBonusMoveSpeed 按原版给 3.0f，或者你可以改大一点比如 10.0f
    pawn.AddModifier(
        "modifier_fleetfoot_boots",
        kv: kv,
        abilityValues: new() { ["ActiveBonusMoveSpeed"] = 25.0f } 
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
