using DeadworksManaged.Api;
using System;
using System.Collections.Generic;

public class ServerDefaultsPlugin : DeadworksPluginBase
{
    public override string Name => "Server Defaults";

    // 1 = 开启开发者指令，0 = 关闭
    private int dev = 0;

    private bool _stageLocked = false;
    private EGameState _lockedStage = EGameState.Invalid;
    // 本局是否已经执行过 /r
    private bool _rTriggered = false;

    public override void OnStartupServer()
    {
        // 每张图开始时重置 /r 标记和锁定状态
        _rTriggered = false;
        _stageLocked = false;
        _lockedStage = EGameState.Invalid;

        // 确保等待条件满足，让引擎能自己从 Init 走到 PreGameWait
        GameRules.SetWaitingForPlayersRoster(0, 0);
        Console.WriteLine($"[ServerDefaults] Startup，当前状态: {GameRules.GameState}，等待引擎进入 PreGameWait 后自动锁定");

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

        _stageLocked = false;
        _lockedStage = EGameState.Invalid;
        _rTriggered = false;
    }

    [Command("r", Description = "解锁并开始比赛")]
    public void CmdStart(Caller caller)
    {
        if (!GameRules.IsValid)
        {
            caller.Reply("❌ 游戏规则尚未就绪。");
            return;
        }

        // 标记本局已执行 /r，并解锁
        _rTriggered = true;
        _stageLocked = false;
        _lockedStage = EGameState.Invalid;

        // 跳回 WaitingForPlayersToJoin，让引擎自己往后走
        GameRules.ChangeGameState(EGameState.WaitingForPlayersToJoin);
        caller.Reply("✅ 已解锁，比赛将自动推进，PreGameWait 结束后播放运镜。");
        Console.WriteLine($"[ServerDefaults] {caller.Name} 触发 /r，解锁并跳到 WaitingForPlayersToJoin。");
    }

    public override bool OnGameStateChanging(EGameState currentState, EGameState newState)
    {
        if (_stageLocked && newState != _lockedStage)
        {
            Console.WriteLine($"[Stage] 锁定中，阻止 {currentState} -> {newState}。");
            return false;
        }
        return true;
    }

    public override void OnGameStateChanged(EGameState newState)
    {
        if (newState != EGameState.PreGameWait) return;

        // 设置 10 秒倒计时
        GameRules.SetGameStateEndTime(GlobalVars.CurTime + 10f);

        if (!_rTriggered)
        {
            // 还没 /r，上锁，停在这里
            _lockedStage = EGameState.PreGameWait;
            _stageLocked = true;
            Console.WriteLine("[ServerDefaults] 未执行 /r，锁定在 PreGameWait。");
        }
        else
        {
            // 已经 /r，只设倒计时，不上锁，允许引擎推进
            Console.WriteLine("[ServerDefaults] 已执行 /r，PreGameWait 倒计时 10 秒，不锁定。");

            // PreGameWait 结束后，给所有人挂运镜 Modifier，卡 7.25 秒再推进
            Timer.Once(10.Seconds(), () =>
            {
                if (!GameRules.IsValid) return;

                // 给所有存活玩家挂上开场运镜 Modifier
                foreach (var controller in Players.GetAll())
                {
                    var pawn = controller.GetHeroPawn();
                    if (pawn == null || !pawn.IsValid) continue;
                    pawn.AddModifier("modifier_cinematic_intro_player");
                }
                Console.WriteLine("[ServerDefaults] PreGameWait 结束，已给所有玩家挂上运镜 Modifier，等待 7.25 秒。");

                Timer.Once(7250.Milliseconds(), () =>
                {
                    if (!GameRules.IsValid) return;
                    GameRules.ChangeGameState(EGameState.MatchIntro);
                    Console.WriteLine("[ServerDefaults] 运镜结束，进入 MatchIntro。");
                });
            });
        }
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
}
