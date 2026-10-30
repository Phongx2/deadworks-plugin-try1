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

    public static Vector3 PlayerAOriginalPos { get; private set; }
    public static Vector3 PlayerBOriginalPos { get; private set; }

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

            // 🌟 修改为你要的 Modifier
            p.AddModifier("ability_familiar_ability01/effect");
            p.AddModifier("modifier_citadel_in_hideout_zone");
        }

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

        GooseEggWatcherPlugin.Instance?.StopBanEgg();

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🏆 决斗结束",
            DescriptionLocstring = $"{winnerName} 战胜了 {loserName}！"
        };
        NetMessages.Send(msg, RecipientFilter.All);

        string[] taunts = {
            $"跟 {loserName} 打真是简简又单单啊！！",
            $"{loserName} 不行啊",
            "拿下"
        };
        





                string tauntText = taunts[Random.Shared.Next(taunts.Length)];
        var chatMsg = new CCitadelUserMsg_ChatMsg
        {
            PlayerSlot = winController.Slot,
            Text = tauntText,
            AllChat = true
        };
        NetMessages.Send(chatMsg, RecipientFilter.All);













        

        var souls = winner.GetCurrency(ECurrencyType.EGold);
        winner.SetCurrency(ECurrencyType.EGold, souls + 3200);

        if (winner.IsValid)
        {
            var originalPos = winner == ActivePlayerA ? PlayerAOriginalPos : PlayerBOriginalPos;
            winner.Teleport(position: originalPos, angles: null, velocity: null);
        }

        // 恢复观战者
        foreach (var kvp in OriginalPositions)
        {
            var p = kvp.Key;
            if (p != null && p.IsValid)
            {
                // 🌟 修改为你要的 Modifier 移除
                p.RemoveModifier("ability_familiar_ability01/effect");
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

        if (ActivePlayerA != null && ActivePlayerB != null && pawn != ActivePlayerA && pawn != ActivePlayerB)
        {
            Timer.Once(5.Ticks(), () =>
            {
                if (pawn.IsValid && ActivePlayerA != null && ActivePlayerB != null)
                {
                    OriginalPositions[pawn] = pawn.Position;

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

                    // 🌟 修改为你要的 Modifier
                    pawn.AddModifier("ability_familiar_ability01/effect");
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
            EffectList.SendDrawNotification(controller, effect);
        }
        else if (effect.Id == "1v1")
        {

    GooseEggWatcherPlugin.Instance?.StartBanEgg();
            
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


            
            var chatMsg = new CCitadelUserMsg_ChatMsg
            {
                PlayerSlot = controller.Slot,
                Text = $"⚠️ 做好准备，我将对 {nameB} 发起决斗！",
                AllChat = true
            };
            NetMessages.Send(chatMsg, RecipientFilter.All);


            

            EffectRuntimePlugin.Instance?.Prepare1v1(pawn, playerB!);

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


 else if (effect.Id == "undie")
        {
            // 设置持续时间为 60 秒
            using var kv = new KeyValues3();
            kv.SetFloat("duration", 60.0f);

            // 给玩家挂上殊死一搏的 Modifier
            pawn.AddModifier("upgrade_cheat_death/cheat_death", kv);

            // 可选：发送提示
            EffectList.SendDrawNotification(controller, effect);
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

            p.AddModifier("ability_familiar_ability01/effect");
           // p.AddModifier("modifier_citadel_in_hideout_zone");
        }

        caller.Reply("已传送所有玩家，3秒后还原...");

        Timer.Once(5.Seconds(), () =>
        {
            foreach (var kvp in tempPositions)
            {
                if (kvp.Key != null && kvp.Key.IsValid)
                {
                    kvp.Key.RemoveModifier("ability_familiar_ability01/effect");
                   // kvp.Key.RemoveModifier("modifier_citadel_in_hideout_zone");
                    kvp.Key.Teleport(position: kvp.Value, angles: null, velocity: null);
                }
            }
            caller.Reply("坐标已还原！");
        });
    }
}

// ==========================================
// 5. 动态加 Modifier 调试指令 (/e)
// ==========================================
public class SelfEffectCommandPlugin : DeadworksPluginBase
{
    public override string Name => "Self Effect Command Plugin";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine("[SelfEffectCommandPlugin] /e 命令已加载！");
    }

    [Command("e", Description = "给自己施加指定的 Modifier。用法: /e <ModifierName>")]
    public void CmdApplyModifier(Caller caller, string modifierName)
    {
        var pawn = caller.Player?.GetHeroPawn();
        if (pawn == null || !pawn.IsValid)
        {
            caller.Reply("你必须在游戏内且有英雄才能使用此命令。");
            return;
        }

        if (string.IsNullOrWhiteSpace(modifierName))
        {
            caller.Reply("请提供 Modifier 名称，例如: /e modifier_hero_testing_fast_movespeed");
            return;
        }

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







[Command("say", Description = "以你的身份向全服发送消息。用法: /say <消息>")]
    public void CmdSay(Caller caller, params string[] messageParts)
    {
        var controller = caller.Player;
        if (controller == null)
        {
            caller.Reply("你必须在游戏内才能使用此命令。");
            return;
        }

        string text = string.Join(' ', messageParts);
        if (string.IsNullOrWhiteSpace(text))
        {
            caller.Reply("请提供要发送的消息。");
            return;
        }

        // 构造聊天网络消息，指定发送者为执行者
        var chatMsg = new CCitadelUserMsg_ChatMsg
        {
            PlayerSlot = controller.Slot,
            Text = text,
            AllChat = true
        };

        // 广播给所有玩家
        NetMessages.Send(chatMsg, RecipientFilter.All);
    }













    
}
