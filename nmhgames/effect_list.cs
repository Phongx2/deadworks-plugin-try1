using DeadworksManaged.Api;
using System;
using System.Collections.Generic;
using ITimer = DeadworksManaged.Api.ITimer;



// 定义一个结构，代表一种效果
public struct EffectInfo
{
    public string Id;          // 变量名 (如 speed_boost)
    public string DisplayName; // 中文名 (如 加速)
    public float Duration;     // 持续时间(秒)
    public int Level;          // 🌟 新增：效果等级 (1-4)

    public EffectInfo(string id, string displayName, float duration, int level = 1)
    {
        Id = id;
        DisplayName = displayName;
        Duration = duration;
        Level = level;
    }
}





public static class EffectList
{
    // ==========================================
    // 核心数据池：增删改查只需改这里
    // ==========================================
    public static readonly EffectInfo[] Pool = new EffectInfo[]
    {
        new EffectInfo("speed_boost", "加速", 10.0f,1),
        new EffectInfo("1v1", "斗殴", 3.0f,1), 








        
    };








    

  private static readonly Random _random = new Random();

    // ==========================================
    // 核心抽卡逻辑
    // ==========================================
    public static void Chouka(CCitadelPlayerPawn pawn, ITimer timer)
    {
        if (pawn == null) return;
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";

        // 🌟 第一步：按概率抽取效果等级
        int rolledLevel = RollLevel();
        Console.WriteLine($"[抽卡] {playerName} 抽到了等级: {rolledLevel}");

        // 🌟 第二步：在这个等级的效果池里筛选
        var levelPool = Pool.Where(e => e.Level == rolledLevel).ToArray();

        if (levelPool.Length == 0)
        {
            // 如果这个等级没写效果，给个兜底提示并直接返回
            Chat.PrintToChat(pawn.Controller, $"❌ 抽到了 {rolledLevel} 级，但该等级下暂无效果配置！");
            Console.WriteLine($"[抽卡] 等级 {rolledLevel} 的效果池为空，取消执行。");
            return;
        }

        // 🌟 第三步：从筛选后的池子里随机抽一个
        var effect = levelPool[_random.Next(levelPool.Length)];
        
        Console.WriteLine($"[抽卡] {playerName} 抽到了: [{effect.Level}级] {effect.DisplayName} ({effect.Id})");
        
        // 执行效果
        Effects.Execute(effect, pawn, timer); 
    }

    // ==========================================
    // 等级概率计算
    // 75% -> 1级, 20% -> 2级, 4% -> 3级, 1% -> 4级
    // ==========================================
    public static int RollLevel()
    {
        // 生成 0 到 99 的随机数
        int roll = _random.Next(100); 
        int level;
        
        if (roll < 75) level = 1;       // 0-74: 1级
        else if (roll < 95) level = 2;  // 75-94: 2级
        else if (roll < 99) level = 3;  // 95-98: 3级
        else level = 4;                 // 99: 4级

        // 🌟 输出 roll 原始数值和最终计算的等级到服务器控制台
        Console.WriteLine($"[抽卡测试] 随机数 Roll: {roll}, 判定等级: {level}");
        
        return level;
    
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
   public static bool ExecuteByIndex(int index, float? customDuration, Caller caller, ITimer timer)
    {
        int arrayIndex = index - 1;
        if (arrayIndex < 0 || arrayIndex >= Pool.Length) return false;

        var pawn = caller.Player?.GetHeroPawn();
        var controller = caller.Player;

        if (pawn == null || controller == null)
        {
            if (controller != null) controller.PrintToConsole("你必须在游戏内且有英雄才能测试效果。");
            else Console.WriteLine("你必须在游戏内且有英雄才能测试效果。");
            return true;
        }

        var effect = Pool[arrayIndex];
        if (customDuration.HasValue)
        {
            effect.Duration = customDuration.Value;
            controller.PrintToConsole($"（已自定义持续时间：{effect.Duration} 秒）");
        }

        controller.PrintToConsole($"正在对你执行: {effect.DisplayName}");
        Effects.Execute(effect, pawn, timer); // 传进去
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
        if (index == null)
        {
            EffectList.PrintList(caller);
            return;
        }

        // 🌟 传入 this.Timer
        bool success = EffectList.ExecuteByIndex(index.Value, duration, caller, this.Timer);
        if (!success)
        {
            var controller = caller.Player;
            if (controller != null) controller.PrintToConsole($"序号 {index.Value} 不存在，请输入 /l 查看列表。");
            else Console.WriteLine($"序号 {index.Value} 不存在，请输入 /l 查看列表。");
        }
    }


    [Command("tr", Description = "测试抽卡等级概率")]
    public void CmdTestRoll(Caller caller)
    {
        // 调用 RollLevel 触发控制台输出
        int result = EffectList.RollLevel();
        
        // 给执行命令的人一个反馈
        caller.Reply($"[测试] 本次抽卡等级判定为: {result} 级");
    }







    
}












