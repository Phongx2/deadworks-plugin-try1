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
    // 如果你那个把 kv 放最后的语法不报错，你就直接这么写。
    // 如果报错，就用下面“备选”里的写法。
    using var kv = new KeyValues3();
    kv.SetFloat("duration", 30.0f); 

    pawn.AddModifier("upgrade_fleetfoot_boots/modifier_fleetfoot_boots", new Dictionary<string, float> {
        ["ActiveBonusMoveSpeed"] = 15.0f, // 把 3f 改成了 15f，不然太慢了感觉不出来
        ["SlowResistancePercent"] = 40f
    }, kv); 
    
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
