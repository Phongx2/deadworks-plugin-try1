using DeadworksManaged.Api;
using System.Numerics;
using DeadworksManaged.Api.Sounds;


namespace WaikaPlugin;

public class WaikaPlugin : DeadworksPluginBase
{
    public override string Name => "Waika";

    private bool _isLavaActive = false;
    private IHandle? _lavaTimer = null;
    private readonly HashSet<string> _welcomedPlayers = new HashSet<string>();

    // ========== /t team 相关字段 ==========
private bool _isTeamModeActive = false;
private bool _team2Triggered = false;  // Team 2 是否已触发
private bool _team3Triggered = false;  // Team 3 是否已触发

    // ========== /t cheat 相关字段 ==========
    private bool _isCheatModeActive = false;
    private HashSet<CCitadelPlayerController> _cheatPlayers = new HashSet<CCitadelPlayerController>();
    private HashSet<CCitadelPlayerController> _cheatUsed = new HashSet<CCitadelPlayerController>();

    // ========== /ks 执行时间配置 ==========
private readonly int _ksDurationMinutes = 2;        // 开关类型功能的持续时间（分钟）
private readonly int _ksDurationExtraSeconds = 30;  // 一次性功能额外等待时间（秒）



     // ========== /t hg 相关字段 ==========
    private bool _isHgActive = false;

    // ========== /t air 相关字段 ==========
private bool _isAirActive = false;
private IHandle? _airResetTimer = null;  // 新增：用于重置跳跃/冲刺计数的计时器



// ========== 所有 /t 功能方法名数组（按顺序） ==========
private readonly string[] _tCommands = new string[]
{
    "lava",
    "fight",
    "team",
    "swap",
    "cheat",
    "hg",
    "air"
};




// ========== /ks 相关字段 ==========
private List<string> _shuffledCommands = new List<string>();  // 打乱后的命令列表
private int _currentCommandIndex = 0;  // 当前执行到的索引
private IHandle? _ksTimer = null;  // 主计时器
private bool _isKsRunning = false;  // 是否正在运行









public override void OnLoad(bool isReload)
{
    Console.WriteLine(isReload ? "[Waika] 热重载完成！" : "[Waika] 已加载！");
    
    // ========== 重置 /ks 状态 ==========
    _isKsRunning = false;
    _currentCommandIndex = 0;
    if (_ksTimer != null)
    {
        _ksTimer.Cancel();
        _ksTimer = null;
    }
    // ========== 重置结束 ==========
}

public override void OnUnload()
{
    Console.WriteLine("[Waika] 已卸载！");
    
    StopLava();
    StopTeamMode();
    StopCheatMode();
    StopHg();
    StopAir();  // StopAir 会取消 _airResetTimer
    
    _isKsRunning = false;
    _currentCommandIndex = 0;
    if (_ksTimer != null)
    {
        _ksTimer.Cancel();
        _ksTimer = null;
    }
    
    _welcomedPlayers.Clear();
}






// ========== Rest() 方法 ==========
private void Rest()
{
    Console.WriteLine("[Waika] 执行 Rest()");
    var msg = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "⚠️ 有什么奇怪的事情要发生了",
        DescriptionLocstring = "30s后将出现异常"
    };
    NetMessages.Send(msg, RecipientFilter.All);
}











   // ========== /t hg ==========
private void StartHg()
{
    if (_isHgActive)
    {
        StopHg();
        return;
    }

    Console.WriteLine("[Waika] 启动 HG 模式");
    _isHgActive = true;

    ConVar.Find("sv_gravity")?.SetInt(9000);
    Console.WriteLine("[Waika] sv_gravity -> 9000");

Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

    var msg = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "🌌 神秘天体经过纽约上空",
        DescriptionLocstring = "重力出现异常-超重！"
    };
    NetMessages.Send(msg, RecipientFilter.All);
}

private void StopHg()
{
    if (!_isHgActive) return;

    Console.WriteLine("[Waika] 关闭 HG 模式");
    _isHgActive = false;

    ConVar.Find("sv_gravity")?.SetInt(800);
    Console.WriteLine("[Waika] sv_gravity -> 800");

    var msg = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "🌌 神秘天体已经离开",
        DescriptionLocstring = "重力恢复"
    };
    NetMessages.Send(msg, RecipientFilter.All);
}

// ========== /t air ==========
private void StartAir()
{
    if (_isAirActive)
    {
        StopAir();
        return;
    }

    Console.WriteLine("[Waika] 启动 AIR 模式");
    _isAirActive = true;

    // 修改 ConVar
    ConVar.Find("sv_gravity")?.SetInt(5);
    Console.WriteLine("[Waika] sv_gravity -> 5");
    ConVar.Find("sv_airaccelerate")?.SetInt(-10);
    Console.WriteLine("[Waika] sv_airaccelerate -> -10");

    // ========== 启动每 100ms 重置跳跃/冲刺计数 ==========
    _airResetTimer = Timer.Every(100.Milliseconds(), () =>
    {
        if (!_isAirActive)
        {
            _airResetTimer?.Cancel();
            _airResetTimer = null;
            return;
        }

        foreach (var pawn in Players.GetAllPawns())
        {
            if (pawn == null || !pawn.IsValid) continue;

            var abilities = pawn.AbilityComponent?.Abilities;
            if (abilities == null) continue;

            foreach (var ability in abilities)
            {
                if (ability == null) continue;

                // 重置跳跃计数
                var jump = ability.As<CCitadel_Ability_Jump>();
                if (jump != null && jump.ConsecutiveAirJumps > 0)
                {
                    jump.ConsecutiveAirJumps = 0;
                }

                // 重置冲刺计数
                var dash = ability.As<CCitadel_Ability_Dash>();
                if (dash != null && dash.ConsecutiveAirDashes > 0)
                {
                    dash.ConsecutiveAirDashes = 0;
                }
            }
        }
    });
    // ========== 重置逻辑结束 ==========

Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

    var msg = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "🌫️ 仪式吸引了未知存在",
        DescriptionLocstring = "失重，可无限使用耐力，WSAD控制反方向加速！"
    };
    NetMessages.Send(msg, RecipientFilter.All);
}




private void StopAir()
{
    if (!_isAirActive) return;

    Console.WriteLine("[Waika] 关闭 AIR 模式");
    _isAirActive = false;
    
    // ========== 停止计时器 ==========
    _airResetTimer?.Cancel();
    _airResetTimer = null;
    // ========== 停止结束 ==========

    // 恢复 ConVar
    ConVar.Find("sv_gravity")?.SetInt(800);
    Console.WriteLine("[Waika] sv_gravity -> 800");
    ConVar.Find("sv_airaccelerate")?.SetInt(10);
    Console.WriteLine("[Waika] sv_airaccelerate -> 10");

    var msg = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "🌫️ 未知存在觉得没意思",
        DescriptionLocstring = "纽约恢复正常了"
    };
    NetMessages.Send(msg, RecipientFilter.All);
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


Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

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

Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

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
    _team2Triggered = false;
    _team3Triggered = false;

    Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

    var msg = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "⚔️ 有难同当",
        DescriptionLocstring = "如果你的队友死亡了，整个队伍的所有人都会死亡！（每个队伍只能触发一次）"
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
    if (!_isTeamModeActive) return;

    Console.WriteLine("[Waika] 停止 Team 模式");
    _isTeamModeActive = false;
    _team2Triggered = false;
    _team3Triggered = false;
}

    // ========== Team 模式的死亡事件处理 ==========
[GameEventHandler("player_death")]
public HookResult OnPlayerDeathForTeamMode(GameEvent ev)
{
    if (!_isTeamModeActive) return HookResult.Continue;

    var victim = ev.GetPlayerPawn("userid")?.As<CCitadelPlayerPawn>();
    if (victim == null) return HookResult.Continue;

    int victimTeam = victim.TeamNum;
    Console.WriteLine($"[Waika] 检测到玩家死亡，队伍: {victimTeam}");

    // ========== 检查该队伍是否已触发过 ==========
    bool teamTriggered = (victimTeam == 2) ? _team2Triggered : _team3Triggered;
    if (teamTriggered)
    {
        Console.WriteLine($"[Waika] 队伍 {victimTeam} 已触发过，跳过");
        return HookResult.Continue;
    }
    // ========== 检查结束 ==========

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

        Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "💀 有难同当",
            DescriptionLocstring = $"队伍 {victimTeam} 的队友被连带死亡！"
        };
        NetMessages.Send(msg, RecipientFilter.All);
    }

    // ========== 标记该队伍已触发 ==========
    if (victimTeam == 2)
        _team2Triggered = true;
    else if (victimTeam == 3)
        _team3Triggered = true;
    Console.WriteLine($"[Waika] 队伍 {victimTeam} 已标记为触发");

    // ========== 检查是否两个队伍都已触发 ==========
    if (_team2Triggered && _team3Triggered)
    {
        Console.WriteLine("[Waika] 两个队伍都已触发，停止 Team 模式");
        StopTeamMode();
    }
    // ========== 检查结束 ==========

    return HookResult.Continue;
}

    // ========== /t swap ==========
    private void StartSwap()
{
    Console.WriteLine("[Waika] 执行 Swap 模式");

    Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

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

            int currentTeam = controller.TeamNum;
            int newTeam;

            if (currentTeam == 2)
                newTeam = 3;
            else if (currentTeam == 3)
                newTeam = 2;
            else
                continue;

            var pawn = controller.GetHeroPawn();

            // ========== 判断玩家状态 ==========
            if (pawn != null && pawn.IsValid && pawn.LifeState == LifeState.Alive)
            {
                // 活着的玩家：使用 modifier 换队伍
                using var kv = new KeyValues3();
                kv.SetInt("team", newTeam);
                pawn.AddModifier("citadel_change_team", kv);

                var pawnRef = pawn;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.RemoveModifier("citadel_change_team");
                        Console.WriteLine($"[Waika] {controller.PlayerName} 队伍 -> {newTeam} (modifier)");
                    }
                });
            }
            else
            {
                // 死亡的玩家：直接使用 ChangeTeam
                controller.ChangeTeam(newTeam);
                Console.WriteLine($"[Waika] {controller.PlayerName} 队伍 {currentTeam} -> {newTeam} (ChangeTeam)");
            }
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
            StopCheatMode();
        }

        Console.WriteLine("[Waika] 启动 Cheat 模式");

        _cheatPlayers.Clear();
        _cheatUsed.Clear();
        _isCheatModeActive = false;

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

        var random = new Random();

        var selectedTeam2 = team2Players[random.Next(team2Players.Count)];
        var selectedTeam3 = team3Players[random.Next(team3Players.Count)];

        _cheatPlayers.Add(selectedTeam2);
        _cheatPlayers.Add(selectedTeam3);

        _isCheatModeActive = true;

        Console.WriteLine($"[Waika] 选中的内鬼: {selectedTeam2.PlayerName} (Team 2), {selectedTeam3.PlayerName} (Team 3)");

        foreach (var player in allControllers)
        {
            if (player == null) continue;

            var msg = new CCitadelUserMsg_HudGameAnnouncement();

            Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

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

    // ========== 监听近战攻击（内鬼专用 - 移除 break，可击杀多个队友） ==========
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

    Console.WriteLine($"[Waika] [重要] 内鬼 {controller.PlayerName} 使用了近战攻击！");

    var attacker = pawn;
    var attackerController = controller;
    
    var meleeAbility = pawn.AbilityComponent?.Abilities
        .FirstOrDefault(a => a?.AbilityName == abilityName);

    Timer.Once(100.Milliseconds(), () =>
    {
        if (!_isCheatModeActive) return;
        if (attacker == null || !attacker.IsValid) return;
        if (attackerController == null) return;
        if (!_cheatPlayers.Contains(attackerController)) return;
        if (_cheatUsed.Contains(attackerController)) return;

        var teammates = Players.GetAllPawns()
            .Where(p => p != null && p.IsValid && p.TeamNum == attacker.TeamNum && p != attacker)
            .ToList();

        Vector3 attackerPos = attacker.Position;
        Vector3 forward = GetForwardVector(attacker.EyeAngles);
        float meleeRange = 180f;
        float angleThreshold = 0.6428f;

        bool hasHitAny = false;  // 标记是否至少命中了一个队友

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

            Console.WriteLine($"[Waika] [重要] 内鬼 {attackerController.PlayerName} 近战命中了队友 {victimController.PlayerName}！");
            hasHitAny = true;

            // 秒杀队友
            victim.Hurt(
                damage: 999999f,
                attacker: attacker,
                inflictor: null,
                ability: meleeAbility,
                damageType: 4
            );
            Console.WriteLine($"[Waika] [DEBUG] 队友 {victimController.PlayerName} 已被 {attackerController.PlayerName} 秒杀");

            // ========== 不 break，继续检测下一个队友 ==========
        }

        // 只要命中过至少一个队友，就执行叛变逻辑
        if (hasHitAny)
        {
            // 标记内鬼已使用
            _cheatUsed.Add(attackerController);

            // 内鬼换到敌方队伍
            int newTeam = (attacker.TeamNum == 2) ? 3 : 2;
            using var kv = new KeyValues3();
            kv.SetInt("team", newTeam);
            attacker.AddModifier("citadel_change_team", kv);
            Console.WriteLine($"[Waika] [DEBUG] {attackerController.PlayerName} 正在切换到 Team {newTeam}");

            var pawnRef = attacker;
            Timer.Once(1.Seconds(), () =>
            {
                if (pawnRef != null && pawnRef.IsValid)
                {
                    pawnRef.RemoveModifier("citadel_change_team");
                    Console.WriteLine($"[Waika] [DEBUG] {attackerController.PlayerName} -> Team {newTeam} 完成");
                }
            });

            // 广播消息
            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "🔪 内鬼暴露！",
                DescriptionLocstring = $"{attackerController.PlayerName} 击杀了队友，已叛变到敌方队伍！"
            };
            NetMessages.Send(msg, RecipientFilter.All);
            Console.WriteLine($"[Waika] [DEBUG] 已广播击杀消息");
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






// ========== /dl 命令 ==========
[Command("dl", Description = "随机打乱 /t 功能顺序并输出到控制台")]
public void CmdDl(CCitadelPlayerController caller)
{
    if (caller == null) return;

    _shuffledCommands = _tCommands.ToList();
    
    var random = new Random();
    for (int i = _shuffledCommands.Count - 1; i > 0; i--)
    {
        int j = random.Next(i + 1);
        (_shuffledCommands[i], _shuffledCommands[j]) = (_shuffledCommands[j], _shuffledCommands[i]);
    }

    caller.PrintToConsole("===== /t 功能随机顺序 =====");
    for (int i = 0; i < _shuffledCommands.Count; i++)
    {
        caller.PrintToConsole($"{i + 1}. /t {_shuffledCommands[i]}");
    }
    caller.PrintToConsole("============================");
    caller.PrintToConsole("[Waika] 已准备就绪，输入 /ks 开始执行");

    Console.WriteLine($"[Waika] {caller.PlayerName} 执行了 /dl，已打乱功能顺序");
}



// ========== /ks 命令 ==========
[Command("ks", Description = "按打乱后的顺序依次执行所有 /t 功能")]
public void CmdKs(CCitadelPlayerController caller)
{
    if (caller == null) return;

    if (_isKsRunning)
    {
        caller.PrintToConsole("[Waika] /ks 正在执行中，请勿重复执行");
        return;
    }

    if (_shuffledCommands.Count == 0)
    {
        caller.PrintToConsole("[Waika] 请先执行 /dl 打乱功能顺序");
        return;
    }

    Console.WriteLine($"[Waika] {caller.PlayerName} 执行了 /ks");
    caller.PrintToConsole("[Waika] 开始执行功能序列...");

    _isKsRunning = true;
    _currentCommandIndex = 0;

    // 开始执行第一个功能
    ExecuteNextCommand();
}

// ========== 执行下一个命令 ==========
private void ExecuteNextCommand()
{
    if (_currentCommandIndex >= _shuffledCommands.Count)
    {
        // 所有命令执行完毕
        Console.WriteLine("[Waika] /ks 所有功能执行完毕");
        CCitadelPlayerController.PrintToConsoleAll("[Waika] 所有异常已结束，纽约恢复了平静");
        _isKsRunning = false;
        _currentCommandIndex = 0;
        return;
    }

    string command = _shuffledCommands[_currentCommandIndex];
    Console.WriteLine($"[Waika] 执行命令: /t {command} (索引 {_currentCommandIndex + 1}/{_shuffledCommands.Count})");

    // 先执行 Rest()
    Rest();

    // 30秒后执行具体的 /t 命令
    Timer.Once(30.Seconds(), () =>
    {
        ExecuteCommand(command);
    });
}


// ========== 执行具体的 /t 命令 ==========
private void ExecuteCommand(string command)
{
    Console.WriteLine($"[Waika] 执行 /t {command}");

    int durationSeconds = _ksDurationMinutes * 60;           // 4分钟 = 240秒
    int extraSeconds = _ksDurationExtraSeconds;              // 30秒

    switch (command)
    {
        case "lava":
            StartLava();
            Timer.Once(durationSeconds.Seconds(), () =>
            {
                StopLava();
                Console.WriteLine("[Waika] lava 已关闭");
                Timer.Once(extraSeconds.Seconds(), () =>
                {
                    _currentCommandIndex++;
                    ExecuteNextCommand();
                });
            });
            break;

        case "fight":
            StartFight();
            Timer.Once((durationSeconds + extraSeconds).Seconds(), () =>
            {
                _currentCommandIndex++;
                ExecuteNextCommand();
            });
            break;

        case "team":
            StartTeamMode();
            Timer.Once(durationSeconds.Seconds(), () =>
            {
                StopTeamMode();
                Console.WriteLine("[Waika] team 已关闭");
                Timer.Once(extraSeconds.Seconds(), () =>
                {
                    _currentCommandIndex++;
                    ExecuteNextCommand();
                });
            });
            break;

        case "swap":
            StartSwap();
            Timer.Once((durationSeconds + extraSeconds).Seconds(), () =>
            {
                _currentCommandIndex++;
                ExecuteNextCommand();
            });
            break;

        case "cheat":
            StartCheatMode();
            Timer.Once(durationSeconds.Seconds(), () =>
            {
                StopCheatMode();
                Console.WriteLine("[Waika] cheat 已关闭");
                Timer.Once(extraSeconds.Seconds(), () =>
                {
                    _currentCommandIndex++;
                    ExecuteNextCommand();
                });
            });
            break;

        case "hg":
            StartHg();
            Timer.Once(durationSeconds.Seconds(), () =>
            {
                StopHg();
                Console.WriteLine("[Waika] hg 已关闭");
                Timer.Once(extraSeconds.Seconds(), () =>
                {
                    _currentCommandIndex++;
                    ExecuteNextCommand();
                });
            });
            break;

        case "air":
            StartAir();
            Timer.Once(durationSeconds.Seconds(), () =>
            {
                StopAir();
                Console.WriteLine("[Waika] air 已关闭");
                Timer.Once(extraSeconds.Seconds(), () =>
                {
                    _currentCommandIndex++;
                    ExecuteNextCommand();
                });
            });
            break;

        default:
            Console.WriteLine($"[Waika] 未知命令: {command}，跳过");
            _currentCommandIndex++;
            ExecuteNextCommand();
            break;
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
        else if (feat == "hg")
{
    StartHg();
    if (caller != null) caller.PrintToConsole(_isHgActive ? "[Waika] 超重模式已启动" : "[Waika] 超重模式已停止");
}
else if (feat == "air")
{
    StartAir();
    if (caller != null) caller.PrintToConsole(_isAirActive ? "[Waika] 失重模式已启动" : "[Waika] 失重模式已停止");
}
        else
        {
            string msg = $"[Waika] 未知功能: {feature}。可用功能: lava, fight, team, swap, cheat, hg, air";
            Console.WriteLine(msg);
            if (caller != null) caller.PrintToConsole(msg);
        }
    }
}
