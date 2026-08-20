using DeadworksManaged.Api;
using System.Numerics;

namespace WaikaPlugin;

public class WaikaPlugin : DeadworksPluginBase
{
    public override string Name => "Waika";

    private bool _isLavaActive = false;
    private IHandle? _lavaTimer = null;
    private readonly HashSet<string> _welcomedPlayers = new HashSet<string>();

    // ========== /t team 相关字段 ==========
    private bool _isTeamModeActive = false;
    private bool _teamModeTriggered = false;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Waika] 热重载完成！" : "[Waika] 已加载！");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Waika] 已卸载！");
        StopLava();
        StopTeamMode();
        _welcomedPlayers.Clear();
    }

    // ========== 监听玩家选择英雄事件 ==========
    [GameEventHandler("player_hero_changed")]
    public HookResult OnPlayerHeroChanged(PlayerHeroChangedEvent args)
    {
        var pawn = args.Userid?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        var controller = GetControllerFromPawn(pawn);
        if (controller == null) return HookResult.Continue;

        string playerKey = controller.PlayerSteamId.ToString();
        if (_welcomedPlayers.Contains(playerKey))
            return HookResult.Continue;

        _welcomedPlayers.Add(playerKey);

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "欢迎游玩本服务器！插件是由匿名黑用AI写的",
            DescriptionLocstring = "哔哩哔哩@不爱搞事情的匿名黑，点点关注谢谢喵！"
        };
        NetMessages.Send(msg, RecipientFilter.Single(controller.Slot));

        Console.WriteLine($"[Waika] 欢迎玩家: {controller.PlayerName}");

        return HookResult.Continue;
    }

    // ========== 辅助：从 Pawn 获取 Controller ==========
    private CCitadelPlayerController? GetControllerFromPawn(CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return null;
        foreach (var controller in Players.GetAll())
        {
            if (controller.GetHeroPawn() == pawn)
                return controller;
        }
        return null;
    }

    // ========== /t fight ==========
    private void StartFight()
    {
        Console.WriteLine("[Waika] 执行 Fight 模式");

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "⚠️ 时空异常",
            DescriptionLocstring = "纽约出现异常，即将发生时空扭曲！"
        };
        NetMessages.Send(msg, RecipientFilter.All);

        Timer.Once(5.Seconds(), () =>
        {
            Console.WriteLine("[Waika] 时空扭曲生效");

            foreach (var pawn in Players.GetAllPawns())
            {
                if (pawn == null || !pawn.IsValid) continue;
                if (pawn.LifeState != LifeState.Alive) continue;

                using var kv = new KeyValues3();
                kv.SetFloat("duration", 1.0f);
                pawn.AddModifier("modifier_chrono_swap_bubble_move", kv);
            }

            Timer.Once(1.Seconds(), () =>
            {
                Console.WriteLine("[Waika] 移除时空扭曲效果");
                foreach (var pawn in Players.GetAllPawns())
                {
                    if (pawn == null || !pawn.IsValid) continue;
                    pawn.RemoveModifier("modifier_chrono_swap_bubble_move");
                }
            });
        });
    }

    // ========== lava 相关 ==========
    private void StartLava()
    {
        if (_isLavaActive)
        {
            Console.WriteLine("[Waika] Lava 模式已在运行中");
            return;
        }

        Console.WriteLine("[Waika] 启动 Lava 模式");
        _isLavaActive = true;

        var startMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🌋 熔岩模式已启动",
            DescriptionLocstring = "站在地面上会受到灼烧伤害！"
        };
        NetMessages.Send(startMsg, RecipientFilter.All);

        _lavaTimer = Timer.Every(500.Milliseconds(), () =>
        {
            if (!_isLavaActive)
            {
                _lavaTimer?.Cancel();
                _lavaTimer = null;
                return;
            }

            foreach (var pawn in Players.GetAllPawns())
            {
                if (pawn == null || !pawn.IsValid) continue;
                if (pawn.LifeState != LifeState.Alive) continue;

                if (pawn.IsOnGround)
                {
                    float maxHealth = pawn.GetMaxHealth();
                    float damage = Math.Max(1f, maxHealth * 0.05f);
                    pawn.Hurt(damage, attacker: null, inflictor: null, ability: null, damageType: 8);
                }
            }
        });
    }

    private void StopLava()
    {
        if (!_isLavaActive) return;

        Console.WriteLine("[Waika] 停止 Lava 模式");
        _isLavaActive = false;
        _lavaTimer?.Cancel();
        _lavaTimer = null;

        var stopMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "⏹️ 熔岩模式已停止",
            DescriptionLocstring = "地面灼烧效果已关闭"
        };
        NetMessages.Send(stopMsg, RecipientFilter.All);
    }

    // ========== /t team ==========
    private void StartTeamMode()
    {
        Console.WriteLine("[Waika] 启动 Team 模式");
        _isTeamModeActive = true;
        _teamModeTriggered = false;

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "⚔️ 有难同当",
            DescriptionLocstring = "如果你的队友死亡了，整个队伍的所有人都会死亡！"
        };
        NetMessages.Send(msg, RecipientFilter.All);

        Timer.Once(1.Seconds(), () =>
        {
            if (!_isTeamModeActive) return;
            Console.WriteLine("[Waika] Team 模式监听已启动");
            CCitadelPlayerController.PrintToConsoleAll("[Waika] 有难同当效果已激活！");
        });
    }

    private void StopTeamMode()
    {
        if (!_isTeamModeActive && !_teamModeTriggered) return;

        Console.WriteLine($"[Waika] 停止 Team 模式 (触发状态: {_teamModeTriggered})");
        _isTeamModeActive = false;
        _teamModeTriggered = false;
    }

    // ========== Team 模式的死亡事件处理 ==========
    [GameEventHandler("player_death")]
    public HookResult OnPlayerDeathForTeamMode(GameEvent ev)
    {
        // 检查模式是否激活且未触发过
        if (!_isTeamModeActive || _teamModeTriggered) return HookResult.Continue;

        // 获取死亡玩家的 Pawn
        var victim = ev.GetPlayerPawn("userid")?.As<CCitadelPlayerPawn>();
        if (victim == null) return HookResult.Continue;

        int victimTeam = victim.TeamNum;
        Console.WriteLine($"[Waika] 检测到玩家死亡，队伍: {victimTeam}");

        // 获取所有玩家
        var allPawns = Players.GetAllPawns().ToList();

        // 找出同队伍的其他存活玩家（使用 LifeState）
        var teammatesToKill = allPawns
            .Where(p => p != null && p.IsValid && p.LifeState == LifeState.Alive && p.TeamNum == victimTeam && p != victim)
            .ToList();

        if (teammatesToKill.Count > 0)
        {
            Console.WriteLine($"[Waika] 队伍 {victimTeam} 有 {teammatesToKill.Count} 名队友被连带死亡");

            foreach (var teammate in teammatesToKill)
            {
                teammate.Hurt(999999f, attacker: null, inflictor: null, ability: null, damageType: 0);
                Console.WriteLine($"[Waika] 队友已死亡");
            }

            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "💀 有难同当",
                DescriptionLocstring = $"队伍 {victimTeam} 的队友被连带死亡！"
            };
            NetMessages.Send(msg, RecipientFilter.All);
        }

        // 效果只触发一次
        _teamModeTriggered = true;
        StopTeamMode();

        return HookResult.Continue;
    }

    // ========== /m 命令 ==========
    [Command("m", Description = "切换自己身上的 modifier: /m <modifier名称>")]
    public void CmdToggleModifier(CCitadelPlayerController caller, string modifierName)
    {
        if (caller == null) return;

        var pawn = caller.GetHeroPawn();
        if (pawn == null)
        {
            caller.PrintToConsole("[Waika] 无法获取英雄实体");
            return;
        }

        bool hasModifier = pawn.ModifierProp?.HasModifier(modifierName) ?? false;

        if (hasModifier)
        {
            pawn.RemoveModifier(modifierName);
            caller.PrintToConsole($"[Waika] 已移除 modifier: {modifierName}");
            Console.WriteLine($"[Waika] {caller.PlayerName} 移除了 modifier: {modifierName}");
        }
        else
        {
            using var kv = new KeyValues3();
            kv.SetFloat("duration", 2.0f);
            pawn.AddModifier(modifierName, kv);
            caller.PrintToConsole($"[Waika] 已添加 modifier: {modifierName} (持续 2 秒)");
            Console.WriteLine($"[Waika] {caller.PlayerName} 添加了 modifier: {modifierName}");
        }
    }

    // ========== /t 命令 ==========
    [Command("t", Description = "功能: /t lava (开关) | /t fight | /t team")]
    public void CmdToggle(CCitadelPlayerController caller, string feature)
    {
        string playerName = caller?.PlayerName ?? "Server Console";
        Console.WriteLine($"[Waika] {playerName} 执行了命令: /t {feature ?? "null"}");

        if (string.IsNullOrEmpty(feature))
        {
            Console.WriteLine("[Waika] 错误: 缺少功能参数");
            if (caller != null) caller.PrintToConsole("[Waika] 请指定功能: /t lava, /t fight, /t team");
            return;
        }

        string feat = feature.ToLower().Trim();

        if (feat == "lava")
        {
            if (_isLavaActive)
            {
                StopLava();
                if (caller != null) caller.PrintToConsole("[Waika] Lava 模式已停止");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 地面灼烧效果已停止");
            }
            else
            {
                StartLava();
                if (caller != null) caller.PrintToConsole("[Waika] Lava 模式已启动");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 地面灼烧效果已启动！站在地面上会受到伤害");
            }
        }
        else if (feat == "fight")
        {
            StartFight();
            if (caller != null) caller.PrintToConsole("[Waika] 时空扭曲已触发");
            CCitadelPlayerController.PrintToConsoleAll("[Waika] 时空扭曲已触发！5秒后生效");
        }
        else if (feat == "team")
        {
            if (_isTeamModeActive)
            {
                StopTeamMode();
                if (caller != null) caller.PrintToConsole("[Waika] Team 模式已停止");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 有难同当效果已停止");
            }
            else
            {
                StartTeamMode();
                if (caller != null) caller.PrintToConsole("[Waika] Team 模式已启动");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 有难同当效果已启动！队友死亡将连带全队");
            }
        }
        else
        {
            string msg = $"[Waika] 未知功能: {feature}。可用功能: lava, fight, team";
            Console.WriteLine(msg);
            if (caller != null) caller.PrintToConsole(msg);
        }
    }
}
