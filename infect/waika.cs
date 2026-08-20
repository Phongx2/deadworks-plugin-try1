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

    // ========== /t cheat 相关字段 ==========
    private bool _isCheatModeActive = false;
    private HashSet<CCitadelPlayerController> _cheatPlayers = new HashSet<CCitadelPlayerController>();
    private HashSet<CCitadelPlayerController> _cheatUsed = new HashSet<CCitadelPlayerController>();

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Waika] 热重载完成！" : "[Waika] 已加载！");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Waika] 已卸载！");
        StopLava();
        StopTeamMode();
        StopCheatMode();
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

    // ========== 获取前方向量 ==========
    private Vector3 GetForwardVector(Vector3 angles)
    {
        float pitch = angles.X * MathF.PI / 180f;
        float yaw = angles.Y * MathF.PI / 180f;
        
        return new Vector3(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Cos(pitch) * MathF.Sin(yaw),
            -MathF.Sin(pitch)
        );
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
            DescriptionLocstring = "站在地面上会受到每0.5秒2.5%最大生命值灼烧伤害！"
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
                    float damage = Math.Max(1f, maxHealth * 0.025f);
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
        if (!_isTeamModeActive || _teamModeTriggered) return HookResult.Continue;

        var victim = ev.GetPlayerPawn("userid")?.As<CCitadelPlayerPawn>();
        if (victim == null) return HookResult.Continue;

        int victimTeam = victim.TeamNum;
        Console.WriteLine($"[Waika] 检测到玩家死亡，队伍: {victimTeam}");

        var allPawns = Players.GetAllPawns().ToList();

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

        _teamModeTriggered = true;
        StopTeamMode();

        return HookResult.Continue;
    }

    // ========== /t swap ==========
    private void StartSwap()
    {
        Console.WriteLine("[Waika] 执行 Swap 模式");

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🔄 风水轮流转",
            DescriptionLocstring = "所有人将会交换队伍！"
        };
        NetMessages.Send(msg, RecipientFilter.All);

        Timer.Once(3.Seconds(), () =>
        {
            Console.WriteLine("[Waika] 开始交换队伍");

            foreach (var controller in Players.GetAll())
            {
                if (controller == null) continue;

                var pawn = controller.GetHeroPawn();
                if (pawn == null || !pawn.IsValid) continue;

                int currentTeam = pawn.TeamNum;
                int newTeam;

                if (currentTeam == 2)
                    newTeam = 3;
                else if (currentTeam == 3)
                    newTeam = 2;
                else
                    continue;

                using var kv = new KeyValues3();
                kv.SetInt("team", newTeam);
                pawn.AddModifier("citadel_change_team", kv);

                var pawnRef = pawn;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.RemoveModifier("citadel_change_team");
                        Console.WriteLine($"[Waika] {controller.PlayerName} 队伍 -> {newTeam}");
                    }
                });
            }

            var doneMsg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "🔄 队伍已交换",
                DescriptionLocstring = "风水轮流转，大家已交换队伍！"
            };
            NetMessages.Send(doneMsg, RecipientFilter.All);
        });
    }

    // ========== /t cheat ==========
    private void StartCheatMode()
    {
        if (_isCheatModeActive)
        {
            Console.WriteLine("[Waika] Cheat 模式已在运行中");
            return;
        }

        Console.WriteLine("[Waika] 启动 Cheat 模式");

        var allControllers = Players.GetAll().ToList();
        if (allControllers.Count < 2)
        {
            Console.WriteLine("[Waika] 玩家数量不足，需要至少2人");
            if (allControllers.Count > 0 && allControllers[0] != null)
            {
                allControllers[0].PrintToConsole("[Waika] 玩家数量不足，需要至少2人");
            }
            return;
        }

        var team2Players = allControllers.Where(c => c.GetHeroPawn()?.TeamNum == 2).ToList();
        var team3Players = allControllers.Where(c => c.GetHeroPawn()?.TeamNum == 3).ToList();

        if (team2Players.Count == 0 || team3Players.Count == 0)
        {
            Console.WriteLine("[Waika] 需要两个队伍都有人");
            if (allControllers.Count > 0 && allControllers[0] != null)
            {
                allControllers[0].PrintToConsole("[Waika] 需要两个队伍都有人");
            }
            return;
        }

        _isCheatModeActive = true;
        _cheatPlayers.Clear();
        _cheatUsed.Clear();

        var random = new Random();

        var selectedTeam2 = team2Players[random.Next(team2Players.Count)];
        var selectedTeam3 = team3Players[random.Next(team3Players.Count)];

        _cheatPlayers.Add(selectedTeam2);
        _cheatPlayers.Add(selectedTeam3);

        Console.WriteLine($"[Waika] 选中的内鬼: {selectedTeam2.PlayerName} (Team 2), {selectedTeam3.PlayerName} (Team 3)");

        foreach (var player in allControllers)
        {
            if (player == null) continue;

            var msg = new CCitadelUserMsg_HudGameAnnouncement();

            if (player == selectedTeam2 || player == selectedTeam3)
            {
                msg.TitleLocstring = "🔪 你成为了内鬼！！！";
                msg.DescriptionLocstring = "你的近战攻击现在可以秒杀队友，击杀成功后自动加入敌方队伍";
            }
            else
            {
                msg.TitleLocstring = "⚠️ 我们中出了一个叛徒";
                msg.DescriptionLocstring = "小心你的背后";
            }

            NetMessages.Send(msg, RecipientFilter.Single(player.Slot));
        }

        Console.WriteLine("[Waika] Cheat 模式监听已启动");
    }

    private void StopCheatMode()
    {
        if (!_isCheatModeActive) return;

        Console.WriteLine("[Waika] 停止 Cheat 模式");
        _isCheatModeActive = false;
        _cheatPlayers.Clear();
        _cheatUsed.Clear();
    }

    // ========== 监听近战攻击（内鬼专用） ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbilityForCheat(GameEvent ev)
    {
        if (!_isCheatModeActive) return HookResult.Continue;

        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        var controller = GetControllerFromPawn(pawn);
        if (controller == null) return HookResult.Continue;

        if (!_cheatPlayers.Contains(controller)) return HookResult.Continue;
        if (_cheatUsed.Contains(controller)) return HookResult.Continue;

        string abilityName = ev.GetString("abilityname", "");
        if (!abilityName.StartsWith("ability_melee")) return HookResult.Continue;

        Console.WriteLine($"[Waika] 内鬼 {controller.PlayerName} 使用了近战攻击");

        var attacker = pawn;
        var attackerController = controller;
        Timer.Once(100.Milliseconds(), () =>
        {
            if (attacker == null || !attacker.IsValid) return;
            if (attackerController == null) return;
            if (_cheatUsed.Contains(attackerController)) return;

            var teammates = Players.GetAllPawns()
                .Where(p => p != null && p.IsValid && p.TeamNum == attacker.TeamNum && p != attacker)
                .ToList();

            Vector3 attackerPos = attacker.Position;
            Vector3 forward = GetForwardVector(attacker.EyeAngles);
            float meleeRange = 180f;
            float angleThreshold = 0.1f;

            foreach (var victim in teammates)
            {
                if (victim == null || !victim.IsValid) continue;

                float distance = Vector3.Distance(attackerPos, victim.Position);
                if (distance > meleeRange) continue;

                Vector3 toTarget = victim.Position - attackerPos;
                Vector3 normalizedToTarget = Vector3.Normalize(toTarget);
                float dotProduct = Vector3.Dot(forward, normalizedToTarget);

                if (dotProduct < angleThreshold) continue;

                var victimController = GetControllerFromPawn(victim);
                if (victimController == null) continue;

                Console.WriteLine($"[Waika] 内鬼 {attackerController.PlayerName} 近战命中了队友 {victimController.PlayerName}");

                _cheatUsed.Add(attackerController);

                victim.Hurt(999999f, attacker: null, inflictor: null, ability: null, damageType: 0);
                Console.WriteLine($"[Waika] 队友 {victimController.PlayerName} 被秒杀");

                int newTeam = (attacker.TeamNum == 2) ? 3 : 2;
                using var kv = new KeyValues3();
                kv.SetInt("team", newTeam);
                attacker.AddModifier("citadel_change_team", kv);

                var pawnRef = attacker;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.RemoveModifier("citadel_change_team");
                        Console.WriteLine($"[Waika] {attackerController.PlayerName} -> Team {newTeam}");
                    }
                });

                var msg = new CCitadelUserMsg_HudGameAnnouncement
                {
                    TitleLocstring = "🔪 内鬼暴露！",
                    DescriptionLocstring = $"{attackerController.PlayerName} 击杀了队友，已叛变到敌方队伍！"
                };
                NetMessages.Send(msg, RecipientFilter.All);

                break;
            }
        });

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
    [Command("t", Description = "功能: /t lava (开关) | /t fight | /t team | /t swap | /t cheat")]
    public void CmdToggle(CCitadelPlayerController caller, string feature)
    {
        string playerName = caller?.PlayerName ?? "Server Console";
        Console.WriteLine($"[Waika] {playerName} 执行了命令: /t {feature ?? "null"}");

        if (string.IsNullOrEmpty(feature))
        {
            Console.WriteLine("[Waika] 错误: 缺少功能参数");
            if (caller != null) caller.PrintToConsole("[Waika] 请指定功能: /t lava, /t fight, /t team, /t swap, /t cheat");
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
        else if (feat == "swap")
        {
            StartSwap();
            if (caller != null) caller.PrintToConsole("[Waika] 队伍交换已触发");
            CCitadelPlayerController.PrintToConsoleAll("[Waika] 队伍交换已触发！3秒后执行");
        }
        else if (feat == "cheat")
        {
            if (_isCheatModeActive)
            {
                StopCheatMode();
                if (caller != null) caller.PrintToConsole("[Waika] Cheat 模式已停止");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 内鬼模式已停止");
            }
            else
            {
                StartCheatMode();
                if (caller != null) caller.PrintToConsole("[Waika] Cheat 模式已启动");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 内鬼模式已启动！小心你的背后");
            }
        }
        else
        {
            string msg = $"[Waika] 未知功能: {feature}。可用功能: lava, fight, team, swap, cheat";
            Console.WriteLine(msg);
            if (caller != null) caller.PrintToConsole(msg);
        }
    }
}
