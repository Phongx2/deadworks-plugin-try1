using DeadworksManaged.Api;
using System;
using System.Collections.Generic;

public class ServerDefaultsPlugin : DeadworksPluginBase
{
    public override string Name => "Server Defaults";

    // 1 = 开启开发者指令，0 = 关闭
    private int dev = 0;

    public override void OnStartupServer()
    {
        // 主动推到 MatchIntro 并停在那里，等待 /r 触发
        GameRules.SetWaitingForPlayersRoster(0, 0);
        GameRules.ChangeGameState(EGameState.MatchIntro);
        Console.WriteLine($"[ServerDefaults] 开局状态已设为: {GameRules.GameState}");

        // 开启滑索开局所需配置
        GameRules.MatchStartOnAnyMap = true;
        ConVar.Find("citadel_match_intro_force_enabled")?.SetInt(1);

        StartBebopWatcher();

        // 基础设置：随处购买（不受 dev 影响）
        if (dev == 1)
        {
            ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);
            ConVar.Find("citadel_trooper_spawn_enabled")?.SetInt(0);
            ConVar.Find("citadel_start_players_on_zipline")?.SetInt(1);
        }

        ConVar.Find("citadel_active_lane")?.SetInt(4);
        ConVar.Find("citadel_player_spawn_time_max_respawn_time")?.SetInt(15);
        ConVar.Find("citadel_player_starting_gold")?.SetInt(1600);

        if (dev == 1)
        {
            Console.WriteLine("[ServerDefaults] dev=1，开启 sv_cheats 并清理地图实体...");

            Server.ExecuteCommand("sv_cheats 1");

            Timer.NextTick(() =>
            {
                Server.ExecuteCommand("citadel_force_kill troopers");
                Server.ExecuteCommand("citadel_force_kill guardians");
                //Server.ExecuteCommand("citadel_force_kill walkers");
                Server.ExecuteCommand("citadel_force_kill base_guardians");

                Timer.NextTick(() =>
                {
                    //Server.ExecuteCommand("sv_cheats 0");
                    Server.ExecuteCommand("citadel_hero_testing_enabled 1");
                    Console.WriteLine("[ServerDefaults] 清理完毕，sv_cheats 已关闭。");
                });
            });
        }
    }

    public override void OnUnload()
    {
        _bebopWatcherTimer?.Cancel();
        _bebopWatcherTimer = null;
        _aliveWalkers.Clear();
        _walkerTeams.Clear();

        // 取消“永远等待”的覆盖，恢复引擎原生行为
        GameRules.SetWaitingForPlayersRoster(0, 0);
        // 文档要求卸载时还原，否则跨地图保留
        GameRules.MatchStartOnAnyMap = false;
    }

    [Command("r", Description = "立即开始比赛")]
    public void CmdStart(Caller caller)
    {
        if (!GameRules.IsValid)
        {
            caller.Reply("❌ 游戏规则尚未就绪。");
            return;
        }

        // MatchIntro -> PreGameWait，等 15 秒
        GameRules.ChangeGameState(EGameState.PreGameWait);
        GameRules.SetGameStateEndTime(GlobalVars.CurTime + 15f);
        Console.WriteLine($"[ServerDefaults] {caller.Name} 触发开始，进入 PreGameWait（15 秒）。");

        Timer.Once(15.Seconds(), () =>
        {
            if (!GameRules.IsValid) return;
            GameRules.ChangeGameState(EGameState.GameInProgress);
            Console.WriteLine("[ServerDefaults] 进入 GameInProgress，滑索开局生效。");
        });

        caller.Reply("✅ 比赛将在 15 秒后开始！");
    }

    private IHandle? _bebopWatcherTimer;
    private readonly HashSet<int> _aliveWalkers = new();
    private readonly Dictionary<int, int> _walkerTeams = new();

    public void StartBebopWatcher()
    {
        _bebopWatcherTimer?.Cancel();
        _aliveWalkers.Clear();
        _walkerTeams.Clear();

        foreach (var e in Entities.ByDesignerName("npc_boss_tier2"))
        {
            if (e == null || !e.IsValid) continue;
            if (e.LifeState != LifeState.Alive) continue;
            _aliveWalkers.Add(e.EntityIndex);
            _walkerTeams[e.EntityIndex] = e.TeamNum;
        }

        Console.WriteLine($"[WalkerWatcher] 初始化完成，当前存活 Walker 数量: {_aliveWalkers.Count}");

        // 如果还没刷出来，每秒重试，直到拿到存活 Walker 再进入正式监听
        if (_aliveWalkers.Count == 0)
        {
            _bebopWatcherTimer = Timer.Every(1.Seconds(), () =>
            {
                foreach (var e in Entities.ByDesignerName("npc_boss_tier2"))
                {
                    if (e == null || !e.IsValid) continue;
                    if (e.LifeState != LifeState.Alive) continue;
                    _aliveWalkers.Add(e.EntityIndex);
                    _walkerTeams[e.EntityIndex] = e.TeamNum;
                }

                if (_aliveWalkers.Count > 0)
                {
                    Console.WriteLine($"[WalkerWatcher] Walker 已刷出，开始正式监听，数量: {_aliveWalkers.Count}");
                    _bebopWatcherTimer?.Cancel();
                    _bebopWatcherTimer = null;
                    StartBebopWatcher(); // 重新走一遍，这次能进入正式监听
                }
            });
            return;
        }

        // 原有正式监听逻辑
        _bebopWatcherTimer = Timer.Every(1.Seconds(), () =>
        {
            if (!GameRules.IsValid) return;

            var currentAlive = new HashSet<int>();
            foreach (var e in Entities.ByDesignerName("npc_boss_tier2"))
            {
                if (e == null || !e.IsValid) continue;
                if (e.LifeState != LifeState.Alive) continue;
                currentAlive.Add(e.EntityIndex);
                _walkerTeams[e.EntityIndex] = e.TeamNum;
            }

            // 上一秒活着、这一秒不在存活集合里 = 刚死
            foreach (var deadIndex in _aliveWalkers)
            {
                if (currentAlive.Contains(deadIndex)) continue;
                if (!_walkerTeams.TryGetValue(deadIndex, out int deadTeam)) continue;

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





    private bool _stageLocked = false;
private EGameState _lockedStage = EGameState.Invalid;

[Command("stage", Description = "切换到指定阶段并锁死：/stage 0-11")]
public void CmdStage(Caller caller, int value)
{
    if (value < 0 || value > 11)
    {
        caller.Reply("❌ 阶段编号必须是 0-11。");
        return;
    }

    if (!GameRules.IsValid)
    {
        caller.Reply("❌ 游戏规则尚未就绪。");
        return;
    }

    var target = (EGameState)value;
    _lockedStage = target;
    _stageLocked = true;

    GameRules.ChangeGameState(target);
    caller.Reply($"✅ 已切换到 {target} (0x{value:X})，并锁定，不会自动推进。");
    Console.WriteLine($"[Stage] {caller.Name} 切换到 {target} (0x{value:X})，已锁定。");
}

[Command("unstage", Description = "解除阶段锁定，允许引擎正常推进")]
public void CmdUnstage(Caller caller)
{
    _stageLocked = false;
    _lockedStage = EGameState.Invalid;
    caller.Reply("✅ 阶段锁定已解除，引擎可以正常推进。");
}
}
