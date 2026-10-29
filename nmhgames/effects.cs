using DeadworksManaged.Api;
using DeadworksManaged.Api.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using ITimer = DeadworksManaged.Api.ITimer;
using System.Numerics;

// ==========================================
// 1. 坐标工具与点位配置
// ==========================================
public static class MapPoints
{
    public static Vector3 ParsePos(string posStr)
    {
        var parts = posStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && float.TryParse(parts[0], out var x) && float.TryParse(parts[1], out var y) && float.TryParse(parts[2], out var z))
            return new Vector3(x, y, z);
        return new Vector3(0, 0, 0);
    }

    public static readonly string[] TeamAPoints = {
        "-591.781250 -697.187500 11.843750",
        "-637.343750 -316.406250 -2.250000",
        "-646.750000 -61.281250 40.062500",
        "-617.093750 336.000000 -2.406250",
        "-621.312500 699.218750 4.343750"
    };
    public static readonly string[] TeamBPoints = {
        "611.562500 656.562500 7.437500",
        "607.687500 288.625000 0.718750",
        "656.031250 44.125000 9.500000",
        "632.281250 -318.250000 11.531250",
        "603.906250 -717.437500 -1.250000"
    };
}

// ==========================================
// 2. 运行时状态与事件监听插件
// ==========================================
public class EffectRuntimePlugin : DeadworksPluginBase
{
    public override string Name => "Effect Runtime Plugin";
    
    public static EffectRuntimePlugin Instance { get; private set; }

    public static CCitadelPlayerPawn ActivePlayerA { get; private set; }
    public static CCitadelPlayerPawn ActivePlayerB { get; private set; }

    // 记录决斗双方的初始坐标
    public static Vector3 PlayerAOriginalPos { get; private set; }
    public static Vector3 PlayerBOriginalPos { get; private set; }

    // 记录观战者的初始坐标
    public static readonly Dictionary<CCitadelPlayerPawn, Vector3> OriginalPositions = new();
    public static readonly HashSet<CCitadelPlayerPawn> WatchingDeadPlayers = new();

    private IHandle _duelMonitorTimer;

    public override void OnLoad(bool isReload)
    {
        Instance = this;
        Console.WriteLine("[EffectRuntimePlugin] 已加载 1v1 运行时逻辑");
    }

    public void Prepare1v1(CCitadelPlayerPawn playerA, CCitadelPlayerPawn playerB)
    {
        ActivePlayerA = playerA;
        ActivePlayerB = playerB;
        
        // 记录决斗双方坐标
        PlayerAOriginalPos = playerA?.Position ?? new Vector3(0, 0, 0);
        PlayerBOriginalPos = playerB?.Position ?? new Vector3(0, 0, 0);

        OriginalPositions.Clear();
        WatchingDeadPlayers.Clear();

        int teamAIndex = 0;
        int teamBIndex = 0;

        foreach (var c in Players.GetAll())
        {
            var p = c.GetHeroPawn();
            if (p == null || !p.IsValid) continue;
            if (p == playerA || p == playerB) continue;

            var originalPos = p.Position;
            OriginalPositions[p] = originalPos;

            if (p.LifeState != LifeState.Alive)
            {
                WatchingDeadPlayers.Add(p);
                continue;
            }

            if (p.TeamNum == playerA.TeamNum)
            {
                var pointStr = MapPoints.TeamAPoints[teamAIndex % MapPoints.TeamAPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamAIndex++;
            }
            else
            {
                var pointStr = MapPoints.TeamBPoints[teamBIndex % MapPoints.TeamBPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamBIndex++;
            }

            // 施加两个 Modifier 限制
            p.AddModifier("citadel_ability_tengu_urn/immobilize");
            p.AddModifier("modifier_citadel_in_hideout_zone");
        }

        // 启动轮询检测
        _duelMonitorTimer?.Cancel();
        _duelMonitorTimer = Timer.Every(1.Seconds(), () =>
        {
            CheckDuelStatus();
        });
    }

    private void CheckDuelStatus()
    {
        if (ActivePlayerA == null || ActivePlayerB == null) return;
        if (!ActivePlayerA.IsValid || !ActivePlayerB.IsValid) return;

        bool isADead = ActivePlayerA.LifeState != LifeState.Alive;
        bool isBDead = ActivePlayerB.LifeState != LifeState.Alive;

        if (isADead == isBDead) return;

        _duelMonitorTimer?.Cancel();
        _duelMonitorTimer = null;

        if (isADead)
        {
            HandleDuelEnd(ActivePlayerB, ActivePlayerA);
        }
        else
        {
            HandleDuelEnd(ActivePlayerA, ActivePlayerB);
        }
    }

    private void HandleDuelEnd(CCitadelPlayerPawn winner, CCitadelPlayerPawn loser)
    {
        ActivePlayerA = null;
        ActivePlayerB = null;

        var winController = winner.Controller;
        var loseController = loser.Controller;
        if (winController == null || loseController == null) return;

        string winnerName = winController.PlayerName ?? "未知玩家";
        string loserName = loseController.PlayerName ?? "未知玩家";

        // 1. 发送决斗结果 HUD
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🏆 决斗结束",
            DescriptionLocstring = $"{winnerName} 战胜了 {loserName}！"
        };
        NetMessages.Send(msg, RecipientFilter.All);

        // 2. 嘲讽语发公屏（无名字前缀）
        string[] taunts = {
            $"跟 {loserName} 打真是简简又单单啊！！",
            $"{loserName} 不行啊",
            "拿下"
        };
        string tauntText = taunts[Random.Shared.Next(taunts.Length)];
        Chat.PrintToChatAll(tauntText);

        // 3. 奖励赢家 3200 钱
        var souls = winner.GetCurrency(ECurrencyType.EGold);
        winner.SetCurrency(ECurrencyType.EGold, souls + 3200);

        // 4. 赢家传回原位
        if (winner.IsValid)
        {
            var originalPos = winner == ActivePlayerA ? PlayerAOriginalPos : PlayerBOriginalPos;
            winner.Teleport(position: originalPos, angles: null, velocity: null);
        }

        // 5. 恢复观战者
        foreach (var kvp in OriginalPositions)
        {
            var p = kvp.Key;
            if (p != null && p.IsValid)
            {
                p.RemoveModifier("citadel_ability_tengu_urn/immobilize");
                p.RemoveModifier("modifier_citadel_in_hideout_zone");
                p.Teleport(position: kvp.Value, angles: null, velocity: null);
            }
        }
        OriginalPositions.Clear();
        WatchingDeadPlayers.Clear();
    }

    [GameEventHandler("player_respawned")]
    public HookResult OnPlayerRespawned(PlayerRespawnedEvent args)
    {
        var pawn = args.Userid?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        // 如果决斗还在进行，且此人不是决斗双方
        if (ActivePlayerA != null && ActivePlayerB != null && pawn != ActivePlayerA && pawn != ActivePlayerB)
        {
            // 延迟 5 tick 后记录位置并传送到观战点
            Timer.Once(5.Ticks(), () =>
            {
                if (pawn.IsValid && ActivePlayerA != null && ActivePlayerB != null)
                {
                    // 记录复活后的位置
                    OriginalPositions[pawn] = pawn.Position;

                    // 根据队伍传送到对应观战点
                    if (pawn.TeamNum == ActivePlayerA.TeamNum)
                    {
                        var pointStr = MapPoints.TeamAPoints[Random.Shared.Next(MapPoints.TeamAPoints.Length)];
                        pawn.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                    }
                    else
                    {
                        var pointStr = MapPoints.TeamBPoints[Random.Shared.Next(MapPoints.TeamBPoints.Length)];
                        pawn.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                    }

                    // 施加两个 Modifier
                    pawn.AddModifier("citadel_ability_tengu_urn/immobilize");
                    pawn.AddModifier("modifier_citadel_in_hideout_zone");
                }
            });
        }

        return HookResult.Continue;
    }
}

// ==========================================
// 3. 核心效果执行类
// ==========================================
public static class Effects
{
    private static void SendDrawNotification(CCitadelPlayerController controller, EffectInfo effect)
    {
        Chat.PrintToChat(controller, "你抽到了");
        Chat.PrintToChat(controller, $"{effect.DisplayName} (持续 {effect.Duration} 秒)");
    }

    public static void Execute(EffectInfo effect, CCitadelPlayerPawn pawn, ITimer timer)
    {
        if (pawn == null) return;
        var controller = pawn.Controller;
        if (controller == null) return;

        if (effect.Id == "speed_boost")
        {
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration);

            pawn.AddModifier("upgrade_fleetfoot_boots/modifier_intrinsic_base", kv: kv,
            abilityValues: new() {
                ["MoveWhileShootingSpeedPenaltyReductionPercent"] = 200f,
                ["MoveWhileZoomedSpeedPenaltyReductionPercent"] = 200f,
                ["SlideScale"] = 299f,
                ["BulletResist"] = 15f,
                ["BaseAttackDamagePercent"] = 15f,
            });

            pawn.AddModifier("upgrade_fleetfoot_boots/modifier_fleetfoot_boots", kv: kv,
            abilityValues: new() {
                ["ActiveBonusMoveSpeed"] = 10f,
                ["SlowResistancePercent"] = 200f,
            });

            pawn.AddModifier("modifier_hero_testing_fast_movespeed", kv);
            SendDrawNotification(controller, effect);
        }
        else if (effect.Id == "1v1")
        {
            var enemies = Players.GetAll()
                .Where(c => c != null && c.IsValid && c.Slot != controller.Slot)
                .Select(c => c.GetHeroPawn())
                .Where(p => p != null && p.IsValid && p.TeamNum != pawn.TeamNum)
                .ToList();

            var playerB = enemies.Count > 0 ? enemies[Random.Shared.Next(enemies.Count)] : null;
            var controllerB = playerB?.Controller;
            string nameB = controllerB?.PlayerName ?? "未知敌人";

            Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);
            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "⚠️ 做好准备",
                DescriptionLocstring = $"{controller.PlayerName} 与 {nameB} 将要发起决斗！"
            };
            NetMessages.Send(msg, RecipientFilter.All);
            Chat.PrintToChatAll($"⚠️ 做好准备，我将对 {nameB} 发起决斗！");

            EffectRuntimePlugin.Instance?.Prepare1v1(pawn, playerB);

            timer.Once(3.Seconds(), () =>
            {
                Console.WriteLine("[1v1] 决斗开始，施加时空扭曲");
                using var kvA = new KeyValues3();
                kvA.SetFloat("duration", 1.0f);
                if (pawn.IsValid) pawn.AddModifier("modifier_chrono_swap_bubble_move", kvA);

                using var kvB = new KeyValues3();
                kvB.SetFloat("duration", 1.0f);
                if (playerB != null && playerB.IsValid) playerB.AddModifier("modifier_chrono_swap_bubble_move", kvB);

                timer.Once(1.Seconds(), () =>
                {
                    Console.WriteLine("[1v1] 移除时空扭曲");
                    if (pawn.IsValid) pawn.RemoveModifier("modifier_chrono_swap_bubble_move");
                    if (playerB != null && playerB.IsValid) playerB.RemoveModifier("modifier_chrono_swap_bubble_move");
                });
            });

        
        }
        else
        {
            Console.WriteLine($"[Effects] 未知的效果ID: {effect.Id}");
        }
    }
}

// ==========================================
// 4. 调试指令插件 (/s1)
// ==========================================
public class DebugCommandPlugin : DeadworksPluginBase
{
    public override string Name => "Debug Command Plugin";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine("[DebugCommandPlugin] 调试指令插件已加载！(/s1)");
    }

    [Command("s1", Description = "全服传送到指定点位，3秒后还原")]
    public void CmdS1(Caller caller)
    {
        var tempPositions = new Dictionary<CCitadelPlayerPawn, Vector3>();
        int teamAIndex = 0;
        int teamBIndex = 0;

        foreach (var c in Players.GetAll())
        {
            var p = c.GetHeroPawn();
            if (p == null || !p.IsValid) continue;

            tempPositions[p] = p.Position;

            if (p.TeamNum == 2)
            {
                var pointStr = MapPoints.TeamAPoints[teamAIndex % MapPoints.TeamAPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamAIndex++;
            }
            else if (p.TeamNum == 3)
            {
                var pointStr = MapPoints.TeamBPoints[teamBIndex % MapPoints.TeamBPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamBIndex++;
            }

            p.AddModifier("citadel_ability_tengu_urn/immobilize");
           // p.AddModifier("modifier_citadel_in_hideout_zone");
        }

        caller.Reply("已传送所有玩家，3秒后还原...");

        Timer.Once(5.Seconds(), () =>
        {
            foreach (var kvp in tempPositions)
            {
                if (kvp.Key != null && kvp.Key.IsValid)
                {
                    kvp.Key.RemoveModifier("citadel_ability_tengu_urn/immobilize");
                   // kvp.Key.RemoveModifier("modifier_citadel_in_hideout_zone");
                    kvp.Key.Teleport(position: kvp.Value, angles: null, velocity: null);
                }
            }
            caller.Reply("坐标已还原！");
        });
    }

[Command("e", Description = "给自己施加指定的 Modifier。用法: /e <ModifierName>")]
    public void CmdApplyModifier(Caller caller, string modifierName)
    {
        // 1. 确保是玩家执行
        var pawn = caller.Player?.GetHeroPawn();
        if (pawn == null || !pawn.IsValid)
        {
            caller.Reply("你必须在游戏内且有英雄才能使用此命令。");
            return;
        }

        // 2. 确保输入不为空
        if (string.IsNullOrWhiteSpace(modifierName))
        {
            caller.Reply("请提供 Modifier 名称，例如: /e modifier_hero_testing_fast_movespeed");
            return;
        }

        // 3. 施加 Modifier
        try
        {
            pawn.AddModifier(modifierName);
            caller.Reply($"已成功对你施加 Modifier: {modifierName}");
        }
        catch (Exception ex)
        {
            caller.Reply($"施加失败，请检查 Modifier 名称是否正确。错误信息: {ex.Message}");
        }
    }








    
}

 
