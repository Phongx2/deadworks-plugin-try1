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
    // 获取执行命令的玩家控制器
    var controller = caller.Player;

    if (controller != null)
    {
        // 玩家执行：输出到玩家控制台
        controller.PrintToConsole("========== 效果列表 ==========");
        for (int i = 0; i < Pool.Length; i++)
        {
            controller.PrintToConsole($"[{i + 1}] {Pool[i].DisplayName} ({Pool[i].Id})");
        }
    }
    else
    {
        // 服务器后台执行：输出到服务器控制台
        Console.WriteLine("========== 效果列表 ==========");
        for (int i = 0; i < Pool.Length; i++)
        {
            Console.WriteLine($"[{i + 1}] {Pool[i].DisplayName} ({Pool[i].Id})");
        }
    }
}

    // 根据序号执行（增加 float? customDuration 参数）
    public static bool ExecuteByIndex(int index, float? customDuration, Caller caller)
    {
        int arrayIndex = index - 1;
        if (arrayIndex < 0 || arrayIndex >= Pool.Length) return false;

        var pawn = caller.Player?.GetHeroPawn();
        var controller = caller.Player;

        if (pawn == null || controller == null)
        {
            if (controller != null)
                controller.PrintToConsole("你必须在游戏内且有英雄才能测试效果。");
            else
                Console.WriteLine("你必须在游戏内且有英雄才能测试效果。");
            return true;
        }

        // 获取原始效果
        var effect = Pool[arrayIndex];

        // 🌟 核心修改：如果玩家填了自定义时长，就覆盖掉原本的时长
        if (customDuration.HasValue)
            {
            effect.Duration = customDuration.Value;
            controller.PrintToConsole($"（已自定义持续时间：{effect.Duration} 秒）");
            }

        controller.PrintToConsole($"正在对你执行: {effect.DisplayName}");
        
        // 传入修改过时长的 effect 给执行器
        Effects.Execute(effect, pawn);
        return true;
        }
    }












    



public class EffectCommandPlugin : DeadworksPluginBase
{
    public override string Name => "Effect Command Plugin";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine("[EffectCommandPlugin] 命令插件已加载！");
    }

    // 🌟 核心修改：增加 float? duration = null 参数
    [Command("l", Description = "列出所有效果，或使用 /l <序号> [持续时长] 执行对应效果")]
    public void CmdListEffects(Caller caller, int? index = null, float? duration = null)
    {
        // 如果没填序号，打印列表
        if (index == null)
        {
            EffectList.PrintList(caller);
            return;
        }

        // 把时长参数传进去
        bool success = EffectList.ExecuteByIndex(index.Value, duration, caller);
        if (!success)
        {
            var controller = caller.Player;
            if (controller != null)
                controller.PrintToConsole($"序号 {index.Value} 不存在，请输入 /l 查看列表。");
            else
                Console.WriteLine($"序号 {index.Value} 不存在，请输入 /l 查看列表。");
        }
    }
}














