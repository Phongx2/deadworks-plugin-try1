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
    public static bool IsRandomMode = false; 

    
    public static void SendDrawNotification(CCitadelPlayerController controller, EffectInfo effect)
    {
        string text = $"你抽到了\n{effect.DisplayName} (持续 {effect.Duration} 秒)";

        // 构造消息，AllChat = false 表示发到队伍频道
        var msgTeam = new CCitadelUserMsg_ChatMsg
        {
            PlayerSlot = controller.Slot,
            Text = text,
            AllChat = false
        };

        // 广播给全体，引擎会根据 AllChat = false 自动显示到对应队伍的频道里
        NetMessages.Send(msgTeam, RecipientFilter.All);
    }
    
    
    
    
    
    
    
    
    // ==========================================
    // 核心数据池：增删改查只需改这里🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟
    // ==========================================
    public static readonly EffectInfo[] Pool = new EffectInfo[]
    {
        new EffectInfo("speed_boost", "加速", 60.0f,1),
        new EffectInfo("1v1", "斗殴", 3.0f,1), 
        new EffectInfo("undie", "幸免遇难", 30.0f, 3), // 🌟 新加的 undie
        new EffectInfo("UAV", "无人侦察机", 30.0f, 2), // 🌟 新加的 UAV
        new EffectInfo("infbullet", "无限子弹", 60.0f, 4), 
        new EffectInfo("unstop", "霸体", 30.0f, 4), // 你可以自己调整时长和等级
        new EffectInfo("yoshiban", "大手的力量", 10.0f, 2), 
        new EffectInfo("double_curse", "双人诅咒", 30.0f, 3), // 示例：8秒，3级
        new EffectInfo("wallhack", "YOSHI之父赐予我视野", 60.0f, 2), // 示例：5秒，4级卡

        



        
    };



    // ==========================================
    // 🌟 新增：根据固定等级抽卡（专供鹅蛋使用）
    // ==========================================
    public static void ChoukaByLevel(CCitadelPlayerPawn pawn, int level, ITimer timer)
    {
        if (pawn == null) return;
        
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";

        // 1. 筛选出对应等级的池子
        var levelPool = Pool.Where(e => e.Level == level).ToArray();

        if (levelPool.Length == 0)
        {
            // 如果这个等级没写效果，兜底提示
            if (pawn.Controller != null)
            {
                NetMessages.Send(new CCitadelUserMsg_ChatMsg
                {
                    PlayerSlot = pawn.Controller.Slot,
                    Text = $"❌ 触发了 {level} 级效果，但该等级下暂无效果配置！",
                    AllChat = false
                }, RecipientFilter.All);
            }
            Console.WriteLine($"[抽卡] 等级 {level} 的效果池为空，取消执行。");
            return;
        }

        // 2. 从筛选后的池子里随机抽一个
        var effect = levelPool[_random.Next(levelPool.Length)];
        
        Console.WriteLine($"[抽卡] {playerName} 触发了 {level} 级蛋，抽到了: [{effect.Level}级] {effect.DisplayName} ({effect.Id})");
        
        // 3. 执行效果
        Effects.Execute(effect, pawn, timer); 
    }



    

    

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
            NetMessages.Send(new CCitadelUserMsg_ChatMsg
                {
                    PlayerSlot = pawn.Controller.Slot,
                    Text = $"❌ 抽到了 {rolledLevel} 级，但该等级下暂无效果配置！",
                    AllChat = false
                }, RecipientFilter.All);




            
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
        
        if (roll < 50) level = 1;       // 0-74: 1级
        else if (roll < 80) level = 2;  // 75-94: 2级
        else if (roll < 95) level = 3;  // 95-98: 3级
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


        [Command("tr", Description = "测试抽卡等级概率。用法: /tr [测试次数]")]
    public void CmdTestRoll(Caller caller, int? count = null)
    {
        var controller = caller.Player;
        int times = count ?? 1;

        if (times > 10000) times = 10000;
        if (times <= 0) return;

        var stats = new Dictionary<int, int> { { 1, 0 }, { 2, 0 }, { 3, 0 }, { 4, 0 } };

        // 🌟 核心：暂存原本的服务器控制台输出流
        var originalOut = Console.Out;
        try
        {
            // 把控制台输出重定向到一个空写入器（相当于静音）
            Console.SetOut(System.IO.TextWriter.Null);

            // 循环执行抽卡（此时里面再怎么 Console.WriteLine 都不会输出到后台）
            for (int i = 0; i < times; i++)
            {
                int level = EffectList.RollLevel(); 
                if (stats.ContainsKey(level)) stats[level]++;
            }
        }
        finally
        {
            // 🌟 无论循环是否报错，最后一定要恢复原版的控制台输出，否则整个插件的日志都会消失
            Console.SetOut(originalOut);
        }

        // 打印最终统计结果
        string resultMsg = $"\n========== 抽卡测试结果 ({times}次) ==========\n" +
                           $"1级: {stats[1]} 次\n" +
                           $"2级: {stats[2]} 次\n" +
                           $"3级: {stats[3]} 次\n" +
                           $"4级: {stats[4]} 次\n" +
                           $"==============================================";

        // 只发到执行者的控制台
        if (controller != null)
        {
            controller.PrintToConsole(resultMsg);
        }
        else
        {
            Console.WriteLine(resultMsg);
        }
    }






        [Command("rand", Description = "设置抽卡模式：1=随机，0=按时间等级")]
    public void CmdRandMode(Caller caller, int mode)
    {
        if (mode == 1)
        {
            EffectList.IsRandomMode = true;
            caller.Reply("✅ 已切换为：纯随机抽卡模式 (无视蛋的等级)。");
        }
        else if (mode == 0)
        {
            EffectList.IsRandomMode = false;
            caller.Reply("✅ 已切换为：按蛋孵化的时间等级抽卡。");
        }
        else
        {
            caller.Reply("❌ 参数错误，请使用 /rand 1 或 /rand 0");
        }
    }







    
}












