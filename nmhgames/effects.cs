using DeadworksManaged.Api;
using System;

public static class Effects
{
    /// <summary>
    /// 根据效果名称执行对应的逻辑
    /// </summary>using DeadworksManaged.Api;
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
    /// <param name="effectName">效果变量名</param>
    /// <param name="pawn">使用蛋的玩家对象（如果是全局效果，可能用不到具体位置，但能用来播报）</param>
    public static void Execute(string effectName, CCitadelPlayerPawn pawn)
    {
        // 保底判空，防止玩家在抽卡瞬间断开连接导致服务器崩溃
        if (pawn == null) return;
        
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";

        switch (effectName)
        {
            // ==========================================
            // 个人效果
            // ==========================================
            case EffectList.GiveGold:
                // 给玩家增加 5000 灵魂
                var souls = pawn.GetCurrency(ECurrencyType.EGold);
                pawn.SetCurrency(ECurrencyType.EGold, souls + 5000);
                Chat.PrintToChat(pawn.Controller, "🎉 抽卡成功：获得 5000 灵魂！");
                break;

            case EffectList.GiveItem:
                // 给玩家发一个冲刺鞋（注意：如果已经有了，AddItem 会返回 null，但不会报错）
                pawn.AddItem("upgrade_sprint_booster");
                Chat.PrintToChat(pawn.Controller, "🎉 抽卡成功：获得冲刺鞋！");
                break;

            case EffectList.HealFull:
                // 给玩家回满血
                pawn.Heal(pawn.GetMaxHealth());
                Chat.PrintToChat(pawn.Controller, "🎉 抽卡成功：生命值已回满！");
                break;


            // ==========================================
            // 全局效果
            // ==========================================
            case EffectList.SpawnBoss:
                // 在玩家当前位置刷一个 Boss（利用 pawn 的位置，体现了“默认获取使用对象”）
                var boss = CBaseEntity.CreateByDesignerName("npc_boss_tier3");
                if (boss != null)
                {
                    var ekv = new CEntityKeyValues();
                    ekv.SetVector("origin", pawn.Position);
                    boss.Spawn(ekv);
                }
                CCitadelPlayerController.PrintToConsoleAll($"[抽卡] 💀 {playerName} 抽到了地狱难度：召唤了一个 Boss！");
                break;

            case EffectList.GlobalAnnounce:
                // 纯全局广播，不需要特定逻辑，只需要使用 playerName
                Chat.PrintToChatAll($"[抽卡] 📢 恭喜 {playerName} 抽到了大喇叭！全体起立！");
                break;

            // ==========================================
            // 保底处理
            // ==========================================
            default:
                Console.WriteLine($"[Effects] 警告：未知的效果名称 '{effectName}'，请检查 effect_list.cs 和 effects.cs 是否同步。");
                break;
        }
    }
}
