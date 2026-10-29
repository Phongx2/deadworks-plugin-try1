using DeadworksManaged.Api;
using System;
using System.Collections.Generic;

// 定义一个结构，代表一种效果
public struct EffectInfo
{
    public string Id;         // 变量名 (如 speed_boost)
    public string DisplayName; // 中文名 (如 加速)
    public float Duration;     // 持续时间(秒)

    public EffectInfo(string id, string displayName, float duration)
    {
        Id = id;
        DisplayName = displayName;
        Duration = duration;
    }
}





public static class EffectList
{
    // ==========================================
    // 核心数据池：增删改查只需改这里
    // ==========================================
    public static readonly EffectInfo[] Pool = new EffectInfo[]
    {
        new EffectInfo("speed_boost", "加速", 10.0f),
        // 以后加新效果：
        // new EffectInfo("give_gold", "获得金钱", 0f),
    };








    

    private static readonly Random _random = new Random();

    // 随机抽卡
    public static void Chouka(CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return;
        
        var effect = Pool[_random.Next(Pool.Length)];
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";
        
        Console.WriteLine($"[抽卡] {playerName} 抽到了: {effect.DisplayName} ({effect.Id})");
        
        // 传入整个 EffectInfo 结构给执行器
        Effects.Execute(effect, pawn);
    }

    // 打印列表
    public static void PrintList(Caller caller)
    {
        caller.Reply("========== 效果列表 ==========");
        for (int i = 0; i < Pool.Length; i++)
        {
            caller.Reply($"[{i + 1}] {Pool[i].DisplayName} ({Pool[i].Id})");
        }
    }

    // 根据序号执行
    public static bool ExecuteByIndex(int index, Caller caller)
    {
        int arrayIndex = index - 1;
        if (arrayIndex < 0 || arrayIndex >= Pool.Length) return false;

        var pawn = caller.Player?.GetHeroPawn();
        if (pawn == null)
        {
            caller.Reply("你必须在游戏内且有英雄才能测试效果。");
            return true;
        }

        var effect = Pool[arrayIndex];
        caller.Reply($"正在对你执行: {effect.DisplayName}");
        Effects.Execute(effect, pawn);
        return true;
    }
}
