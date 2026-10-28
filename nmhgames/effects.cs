using DeadworksManaged.Api;
using System;

public static class Effects
{
    public static void Execute(string effectName, CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return;

        switch (effectName)
        {
            case EffectList.SpeedBoost:
                // 应用加速逻辑 (这里保留之前的 Modifier 方案)
                using var kv = new KeyValues3();
                kv.SetFloat("duration", 30.0f); 
                pawn.AddModifier(
                    "ability_familiar_attach/speed",
                    kv,
                    abilityValues: new() { ["BonusMoveSpeed"] = 12.0f } 
                );
                
                // 拆分成两句，并使用变量
                Chat.PrintToChat(pawn.Controller, "你抽到了");
                Chat.PrintToChat(pawn.Controller, EffectList.SpeedBoostName);
                break;

            default:
                Console.WriteLine($"[Effects] 未知的效果名称: {effectName}");
                break;
        }
    }
}
