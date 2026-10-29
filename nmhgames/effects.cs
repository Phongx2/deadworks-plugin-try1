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

    // 2. 套用上面的格式
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

    
    pawn.AddModifier("modifier_hero_testing_fast_movespeed", kv);


    
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
