using DeadworksManaged.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using ITimer = DeadworksManaged.Api.ITimer;

// 定义一个结构，代表一种效果
public struct EffectInfo
{
    public string Id;
    public string DisplayName;
    public float Duration;
    public int Level;

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

        var msgTeam = new CCitadelUserMsg_ChatMsg
        {
            PlayerSlot = controller.Slot,
            Text = text,
            AllChat = false
        };

        NetMessages.Send(msgTeam, RecipientFilter.All);
    }
    
    
    
    
    
    
    // ==========================================
    // 核心数据池：增删改查只需改这里🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟🌟
    // ==========================================
    public static readonly EffectInfo[] Pool = new EffectInfo[]
    {
        new EffectInfo("speed_boost", "加速", 60.0f,1),
        

        //new EffectInfo("UAV", "无人侦察机", 30.0f, 2), // 🌟 新加的 UAV
        new EffectInfo("yoshiban", "大手的力量", 10.0f, 1), 
        new EffectInfo("1v1", "斗殴", 3.0f,1), 
        
        
        new EffectInfo("infbullet", "无限子弹", 60.0f, 1), 

        
        
        new EffectInfo("unstop", "霸体", 30.0f, 1), // 你可以自己调整时长和等级
        new EffectInfo("undie", "幸免遇难", 30.0f, 1), // 🌟 新加的 undie
        
       

        



        
    };


 // ==========================================
    // 鹅蛋统一入口：/rand 0 走这里（按孵化时间 level 抽）
    // ==========================================
    public static void ChoukaByLevel(CCitadelPlayerPawn pawn, int level, ITimer timer)
    {
        if (pawn == null) return;

        // /rand 1 时，鹅蛋也走纯随机（先随机等级再在该等级抽）
        if (IsRandomMode)
        {
            Chouka(pawn, timer);
            return;
        }

        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";

        var levelPool = Pool.Where(e => e.Level == level).ToArray();

        if (levelPool.Length == 0)
        {
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

        int index = Random.Shared.Next(levelPool.Length);
        var effect = levelPool[index];

        Console.WriteLine($"[抽卡] {playerName} 触发了 {level} 级蛋，poolLen={levelPool.Length}, index={index}, 抽到了: [{effect.Level}级] {effect.DisplayName} ({effect.Id})");

        Effects.Execute(effect, pawn, timer);
    }

    // ==========================================
    // /rand 1：先随机等级，再在该等级里抽
    // ==========================================
    public static void Chouka(CCitadelPlayerPawn pawn, ITimer timer)
    {
        if (pawn == null) return;
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";

        int rolledLevel = RollLevel();
        Console.WriteLine($"[抽卡] {playerName} 抽到了等级: {rolledLevel}");

        var levelPool = Pool.Where(e => e.Level == rolledLevel).ToArray();

        if (levelPool.Length == 0)
        {
            NetMessages.Send(new CCitadelUserMsg_ChatMsg
            {
                PlayerSlot = pawn.Controller.Slot,
                Text = $"❌ 抽到了 {rolledLevel} 级，但该等级下暂无效果配置！",
                AllChat = false
            }, RecipientFilter.All);

            Console.WriteLine($"[抽卡] 等级 {rolledLevel} 的效果池为空，取消执行。");
            return;
        }

        int index = Random.Shared.Next(levelPool.Length);
        var effect = levelPool[index];

        Console.WriteLine($"[抽卡] {playerName} 抽到了: [{effect.Level}级] {effect.DisplayName} ({effect.Id}), poolLen={levelPool.Length}, index={index}");

        Effects.Execute(effect, pawn, timer);
    }

    // ==========================================
    // 等级概率计算
    // ==========================================
    public static int RollLevel()
    {
        int roll = Random.Shared.Next(100);
        int level;

        if (roll < 50) level = 1;
        else if (roll < 80) level = 2;
        else if (roll < 95) level = 3;
        else level = 4;

        Console.WriteLine($"[抽卡测试] 随机数 Roll: {roll}, 判定等级: {level}");

        return level;
    }

    // 打印列表
    public static void PrintList(Caller caller)
    {
        var controller = caller.Player;

        if (controller != null)
        {
            controller.PrintToConsole("========== 效果列表 ==========");
            for (int i = 0; i < Pool.Length; i++)
            {
                controller.PrintToConsole($"[{i + 1}] {Pool[i].DisplayName} ({Pool[i].Id})");
            }
        }
        else
        {
            Console.WriteLine("========== 效果列表 ==========");
            for (int i = 0; i < Pool.Length; i++)
            {
                Console.WriteLine($"[{i + 1}] {Pool[i].DisplayName} ({Pool[i].Id})");
            }
        }
    }

    // 根据序号执行
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
        Effects.Execute(effect, pawn, timer);
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

    [Command("l", Description = "列出所有效果，或使用 /l <序号> [持续时长] 执行对应效果")]
    public void CmdListEffects(Caller caller, int? index = null, float? duration = null)
    {
        if (index == null)
        {
            EffectList.PrintList(caller);
            return;
        }

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

        var originalOut = Console.Out;
        try
        {
            Console.SetOut(System.IO.TextWriter.Null);

            for (int i = 0; i < times; i++)
            {
                int level = EffectList.RollLevel();
                if (stats.ContainsKey(level)) stats[level]++;
            }
        }
        finally
        {
            Console.SetOut(originalOut);
        }

        string resultMsg = $"\n========== 抽卡测试结果 ({times}次) ==========\n" +
                           $"1级: {stats[1]} 次\n" +
                           $"2级: {stats[2]} 次\n" +
                           $"3级: {stats[3]} 次\n" +
                           $"4级: {stats[4]} 次\n" +
                           $"==============================================";

        if (controller != null)
        {
            controller.PrintToConsole(resultMsg);
        }
        else
        {
            Console.WriteLine(resultMsg);
        }
    }

    [Command("rand", Description = "设置抽卡模式：1=纯随机，0=按蛋孵化时间等级")]
    public void CmdRandMode(Caller caller, int mode)
    {
        if (mode == 1)
        {
            EffectList.IsRandomMode = true;
            caller.Reply("✅ 已切换为：纯随机抽卡模式 (先随机等级，再在该等级里抽)。");
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


    [Command("win", Description = "激活 Bebop 死亡判定监听器")]
public void CmdWin(Caller caller)
{
    if (!GameRules.IsValid)
    {
        caller.Reply("❌ 当前没有有效的比赛规则实体，无法激活监听。");
        return;
    }

    this.StartWalkerWatcher();
    caller.Reply("✅ 已激活 Bebop 死亡判定监听器。");
}




private IHandle? _bebopWatcherTimer;
private readonly HashSet<int> _aliveWalkers = new();
private readonly Dictionary<int, int> _walkerTeams = new();

public void StartWalkerWatcher()
{
    _bebopWatcherTimer?.Cancel();
    _aliveWalkers.Clear();
    _walkerTeams.Clear();

    foreach (var e in Entities.ByDesignerName("npc_boss_tier2"))
    {
        if (e == null || !e.IsValid) continue;
        _aliveWalkers.Add(e.EntityIndex);
        _walkerTeams[e.EntityIndex] = e.TeamNum;
    }

    Console.WriteLine($"[WalkerWatcher] 初始化完成，当前存活 Walker 数量: {_aliveWalkers.Count}");

    _bebopWatcherTimer = Timer.Every(1.Seconds(), () =>
    {
        if (!GameRules.IsValid) return;

        var currentAlive = new HashSet<int>();
        foreach (var e in Entities.ByDesignerName("npc_boss_tier2"))
        {
            if (e == null || !e.IsValid) continue;
            currentAlive.Add(e.EntityIndex);
            _walkerTeams[e.EntityIndex] = e.TeamNum;
        }

        foreach (var deadIndex in _aliveWalkers)
        {
            if (currentAlive.Contains(deadIndex)) continue;
            if (!_walkerTeams.TryGetValue(deadIndex, out int deadTeam)) continue;

            // 按你测试的映射：Walker 属于 2 队死了判 1 队赢，反之亦然
            int winner = deadTeam == 2 ? 1 : 2;
            GameRules.WinningTeam = winner;

            Console.WriteLine($"[WalkerWatcher] Walker (EntityIndex={deadIndex}, Team={deadTeam}) 死亡，判定 Team={winner} 获胜。");

            _bebopWatcherTimer?.Cancel();
            _bebopWatcherTimer = null;
            return;
        }

        _aliveWalkers.Clear();
        foreach (var idx in currentAlive) _aliveWalkers.Add(idx);
    });
}



    
}












