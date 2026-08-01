using DeadworksManaged.Api;
using System.Numerics;  // ← 添加这一行

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

    public List<CCitadelPlayerController> allPlayers = new List<CCitadelPlayerController>();
    public Dictionary<int, List<string>> playerItems = new Dictionary<int, List<string>>();
    public CCitadelPlayerController? fixedSender = null;
    public IHandle? team3BuffTimer = null;
    public bool isGameRunning = false;

    public override void OnStartupServer()
    {
        Console.WriteLine($"[KSYX] ========== 设置游戏规则 ==========");
        
        ConVar.Find("citadel_allow_duplicate_heroes")?.SetInt(1);
        Console.WriteLine($"[KSYX] citadel_allow_duplicate_heroes -> 1");
        
        ConVar.Find("citadel_player_starting_gold")?.SetInt(0);
        Console.WriteLine($"[KSYX] citadel_player_starting_gold -> 0");
        
        ConVar.Find("citadel_voice_all_talk")?.SetInt(1);
        Console.WriteLine($"[KSYX] citadel_voice_all_talk -> 1");
        
        ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);
        Console.WriteLine($"[KSYX] citadel_allow_purchasing_anywhere -> 1");
        
        ConVar.Find("sv_cheats")?.SetInt(1);
        Console.WriteLine($"[KSYX] sv_cheats -> 1");
        
        ConVar.Find("citadel_trooper_spawn_enabled")?.SetInt(0);
        Console.WriteLine($"[KSYX] citadel_trooper_spawn_enabled -> 0");

        Console.WriteLine($"[KSYX] 开始清除地图单位...");
        
        foreach (var entity in Entities.ByDesignerName("npc_trooper_boss"))
        {
            entity.Remove();
            Console.WriteLine($"[KSYX] 移除 npc_trooper_boss (索引: {entity.EntityIndex})");
        }
        
        foreach (var entity in Entities.ByDesignerName("npc_boss_tier1"))
        {
            entity.Remove();
            Console.WriteLine($"[KSYX] 移除 npc_boss_tier1 (索引: {entity.EntityIndex})");
        }
        
        foreach (var entity in Entities.ByDesignerName("npc_boss_tier2"))
        {
            entity.Remove();
            Console.WriteLine($"[KSYX] 移除 npc_boss_tier2 (索引: {entity.EntityIndex})");
        }
        
        foreach (var entity in Entities.ByDesignerName("npc_boss_tier2_weak"))
        {
            entity.Remove();
            Console.WriteLine($"[KSYX] 移除 npc_boss_tier2_weak (索引: {entity.EntityIndex})");
        }
        
        foreach (var entity in Entities.ByDesignerName("npc_boss_tier3"))
        {
            entity.Remove();
            Console.WriteLine($"[KSYX] 移除 npc_boss_tier3 (索引: {entity.EntityIndex})");
        }
        
        foreach (var entity in Entities.ByDesignerName("npc_barrack_boss"))
        {
            entity.Remove();
            Console.WriteLine($"[KSYX] 移除 npc_barrack_boss (索引: {entity.EntityIndex})");
        }
        
        foreach (var entity in Entities.ByDesignerName("destroyable_building"))
        {
            entity.Remove();
            Console.WriteLine($"[KSYX] 移除 destroyable_building (索引: {entity.EntityIndex})");
        }
        
        Console.WriteLine($"[KSYX] ========== 游戏规则设置完成 ==========");
    }

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[KSYX] ========== 插件加载 ==========");
        Console.WriteLine($"[KSYX] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        Console.WriteLine($"[KSYX] ===============================");
        fixedSender = null;
        team3BuffTimer = null;
        isGameRunning = false;
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[KSYX] 插件卸载");
        team3BuffTimer?.Cancel();
        team3BuffTimer = null;
        isGameRunning = false;
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

    // ========== 监听近战攻击（使用 GameEventHandler） ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(GameEvent ev)
    {
        // 获取施法者
        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null)
        {
            Console.WriteLine($"[KSYX][DEBUG] OnPlayerUsedAbility: 无法获取施法者 Pawn");
            return HookResult.Continue;
        }

        // 获取技能名称
        string abilityName = ev.GetString("abilityname", "");
        Console.WriteLine($"[KSYX][DEBUG] OnPlayerUsedAbility: {abilityName}");

        // 只处理近战攻击（ability_melee 开头）
        if (!abilityName.StartsWith("ability_melee"))
            return HookResult.Continue;

        // 检查是否是 Team 3 玩家
        if (pawn.TeamNum != 3)
        {
            Console.WriteLine($"[KSYX][DEBUG] 施法者不是 Team 3 (当前 Team {pawn.TeamNum})，跳过");
            return HookResult.Continue;
        }

        // 获取近战类型（轻击/重击）
        string annotation = ev.GetString("annotation", "");
        Console.WriteLine($"[KSYX][DEBUG] 近战类型: {annotation}");

        // 延迟一帧检测命中的目标（伤害已经结算）
        var attacker = pawn;
        Timer.NextTick(() =>
        {
            if (attacker == null || !attacker.IsValid) return;
            
            // 检测攻击者附近是否有 Team 2 玩家被命中
            DetectMeleeHitAndInfect(attacker);
        });

        return HookResult.Continue;
    }

    // ========== 检测近战命中的目标并感染 ==========
    private void DetectMeleeHitAndInfect(CCitadelPlayerPawn attacker)
    {
        if (attacker == null || !attacker.IsValid) return;

        Console.WriteLine($"[KSYX][DEBUG] 检测近战命中目标...");

        // 获取所有 Team 2 的玩家 Pawn
        var team2Pawns = Players.GetAllPawns()
            .Where(p => p != null && p.IsValid && p.TeamNum == 2)
            .ToList();

        if (team2Pawns.Count == 0)
        {
            Console.WriteLine($"[KSYX][DEBUG] 没有 Team 2 玩家");
            return;
        }

        // 获取攻击者的位置和朝向
        Vector3 attackerPos = attacker.Position;
        Vector3 forward = GetForwardVector(attacker.EyeAngles);
        float meleeRange = 250f;  // 近战攻击范围
        float angleThreshold = 0.3f;  // 角度阈值（约 72 度扇形）

        foreach (var victim in team2Pawns)
        {
            if (victim == null || !victim.IsValid) continue;

            // 计算距离
            float distance = Vector3.Distance(attackerPos, victim.Position);
            if (distance > meleeRange)
            {
                Console.WriteLine($"[KSYX][DEBUG] 目标 {GetControllerFromPawn(victim)?.PlayerName} 距离 {distance}，超出范围");
                continue;
            }

            // 计算方向夹角
            Vector3 toTarget = victim.Position - attackerPos;
            Vector3 normalizedToTarget = Vector3.Normalize(toTarget);
            float dotProduct = Vector3.Dot(forward, normalizedToTarget);
            
            Console.WriteLine($"[KSYX][DEBUG] 目标 {GetControllerFromPawn(victim)?.PlayerName} 夹角值: {dotProduct}");

            if (dotProduct < angleThreshold)
            {
                Console.WriteLine($"[KSYX][DEBUG] 目标不在攻击扇形内，跳过");
                continue;
            }

            // ========== 命中！执行感染 ==========
            Console.WriteLine($"[KSYX][重要] Team 3 玩家 {GetControllerFromPawn(attacker)?.PlayerName} 近战命中了 Team 2 玩家 {GetControllerFromPawn(victim)?.PlayerName}！");
            
            var victimRef = victim;
            Timer.NextTick(() =>
            {
                if (victimRef != null && victimRef.IsValid)
                {
                    InfectPlayer(victimRef);
                }
            });

            break;  // 一次近战只感染一个目标
        }
    }

    // ========== 感染转化玩家（和母体生成流程一样） ==========
    private void InfectPlayer(CCitadelPlayerPawn victim)
    {
        Console.WriteLine($"[KSYX][DEBUG] InfectPlayer 被调用");

        if (victim == null)
        {
            Console.WriteLine($"[KSYX][DEBUG] victim 为 null，退出 InfectPlayer");
            return;
        }
        if (!victim.IsValid)
        {
            Console.WriteLine($"[KSYX][DEBUG] victim 无效，退出 InfectPlayer");
            return;
        }

        var victimController = GetControllerFromPawn(victim);
        if (victimController == null)
        {
            Console.WriteLine($"[KSYX][DEBUG] 无法获取 victimController，退出 InfectPlayer");
            return;
        }

        Console.WriteLine($"[KSYX][重要] 开始感染 {victimController.PlayerName}...");

        // 1. 读取装备（保存）
        Console.WriteLine($"[KSYX][DEBUG] 保存 {victimController.PlayerName} 的装备...");
        var items = new List<string>();
        var abilityComponent = victim.AbilityComponent;
        if (abilityComponent != null)
        {
            foreach (var ability in abilityComponent.Abilities)
            {
                if (ability.IsItem)
                {
                    var itemName = ability.AbilityName;
                    if (!string.IsNullOrEmpty(itemName))
                    {
                        items.Add(itemName);
                        Console.WriteLine($"[KSYX][DEBUG] 找到装备: {itemName}");
                    }
                }
            }
        }
        playerItems[victimController.Slot] = items;
        Console.WriteLine($"[KSYX][DEBUG] 共保存 {items.Count} 件装备");

        // 2. 切换队伍到 Team 3（使用 modifier）
        Console.WriteLine($"[KSYX][DEBUG] 切换队伍到 Team 3...");
        using var kv = new KeyValues3();
        kv.SetInt("team", 3);
        victim.AddModifier("citadel_change_team", kv);
        Console.WriteLine($"[KSYX][DEBUG] {victimController.PlayerName} -> Team 3");

        var pawnRef = victim;
        Timer.Once(1.Seconds(), () =>
        {
            Console.WriteLine($"[KSYX][DEBUG] 1秒延迟后移除 citadel_change_team modifier...");
            if (pawnRef != null && pawnRef.IsValid)
            {
                pawnRef.RemoveModifier("citadel_change_team");
                Console.WriteLine($"[KSYX][DEBUG] {victimController.PlayerName} -> 移除 citadel_change_team modifier");
            }
            else
            {
                Console.WriteLine($"[KSYX][DEBUG] pawnRef 无效，无法移除 modifier");
            }
        });

        // 3. 切换英雄为 Necro
        Console.WriteLine($"[KSYX][DEBUG] 切换英雄为 Necro...");
        victimController.SelectHero(Heroes.Necro);
        Console.WriteLine($"[KSYX][DEBUG] {victimController.PlayerName} -> Necro");

        // 4. 延迟 3 秒后恢复装备
        var selectedSlot = victimController.Slot;
        Timer.Once(3.Seconds(), () =>
        {
            Console.WriteLine($"[KSYX][DEBUG] 3秒延迟后恢复装备...");
            var pawn = victimController.GetHeroPawn();
            if (pawn != null && pawn.IsValid && playerItems.TryGetValue(selectedSlot, out var savedItems))
            {
                Console.WriteLine($"[KSYX][DEBUG] 开始重新给 {victimController.PlayerName} 装备...");
                foreach (var itemName in savedItems)
                {
                    pawn.AddItem(itemName);
                    Console.WriteLine($"[KSYX][DEBUG] 重新给予装备: {itemName}");
                }
                Console.WriteLine($"[KSYX][DEBUG] 共重新给予 {savedItems.Count} 件装备");
            }
            else
            {
                Console.WriteLine($"[KSYX][DEBUG] 没有找到保存的装备");
            }
        });

        // 5. 广播消息
        Console.WriteLine($"[KSYX][DEBUG] 广播感染消息...");
        var hudMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "",
            DescriptionLocstring = $"{victimController.PlayerName} 被感染成了僵尸！"
        };

        foreach (var player in allPlayers)
        {
            NetMessages.Send(hudMsg, RecipientFilter.Single(player.EntityIndex - 1));
        }

        Console.WriteLine($"[KSYX][重要] 已广播: {victimController.PlayerName} 被感染成了僵尸！");
        Console.WriteLine($"[KSYX][DEBUG] InfectPlayer 执行完毕");
    }

    // ========== 周期性给 Team 3 添加 Buff ==========
    private void StartTeam3BuffTimer()
    {
        Console.WriteLine($"[KSYX][DEBUG] StartTeam3BuffTimer 被调用");
        team3BuffTimer?.Cancel();
        Console.WriteLine($"[KSYX][DEBUG] 已取消旧计时器");

        team3BuffTimer = Timer.Every(1.Seconds(), () =>
        {
            if (!isGameRunning)
            {
                Console.WriteLine($"[KSYX][DEBUG] 游戏已结束，停止 Buff 计时器");
                team3BuffTimer?.Cancel();
                team3BuffTimer = null;
                return;
            }

            var team3Pawns = Players.GetAllPawns()
                .Where(p => p != null && p.IsValid && p.TeamNum == 3)
                .ToList();

            if (team3Pawns.Count == 0)
            {
                Console.WriteLine($"[KSYX][DEBUG] 没有 Team 3 玩家，跳过本次 Buff 添加");
                return;
            }

            Console.WriteLine($"[KSYX][DEBUG] 为 {team3Pawns.Count} 名 Team 3 玩家添加 Buff...");

            foreach (var pawn in team3Pawns)
            {
                using var kv = new KeyValues3();
                kv.SetFloat("duration", 1.1f);
                pawn.AddModifier("modifier_citadel_in_fountain", kv);
                pawn.AddModifier("modifier_citadel_disarmed", kv);
                pawn.AddModifier("modifier_citadel_silenced", kv);
            }
        });
    }

    [Command("ksyx", Description = "僵尸倒计时")]
    public void CmdKsyx(CCitadelPlayerController caller)
    {
        Console.WriteLine($"[KSYX] ========== 命令触发 ==========");
        Console.WriteLine($"[KSYX] 执行者: {(caller != null ? caller.PlayerName : "null")}");

        allPlayers = Players.GetAll().ToList();
        Console.WriteLine($"[KSYX] 在线玩家数: {allPlayers.Count}");

        if (allPlayers.Count == 0)
        {
            Console.WriteLine($"[KSYX] 没有玩家，命令终止");
            return;
        }

        fixedSender = null;
        isGameRunning = true;
        Console.WriteLine($"[KSYX] 重置固定发送者，游戏开始");

        Console.WriteLine($"[KSYX] 设置 sv_cheats = 1");
        ConVar.Find("sv_cheats")?.SetInt(1);

        Timer.Once(500.Milliseconds(), () =>
        {
            Console.WriteLine($"[KSYX] 开始发放金币...");
            
            ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);
            Console.WriteLine($"[KSYX] citadel_allow_purchasing_anywhere -> 1");

            foreach (var player in allPlayers)
            {
                var pawn = player.GetHeroPawn();
                if (pawn != null)
                {
                    pawn.SetCurrency(ECurrencyType.EGold, 32000);
                    Console.WriteLine($"[KSYX] {player.PlayerName} (槽位 {player.Slot}) -> 设置金币为 32000");
                }
                else
                {
                    Console.WriteLine($"[KSYX] {player.PlayerName} -> 没有英雄实体，无法设置金币");
                }
            }
        });

        Console.WriteLine($"[KSYX] 开始移动玩家到 Team 2...");
        foreach (var player in allPlayers)
        {
            var pawn = player.GetHeroPawn();
            if (pawn != null && pawn.TeamNum == 3)
            {
                using var kv = new KeyValues3();
                kv.SetInt("team", 2);
                pawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {player.PlayerName} (Team 3) -> Team 2");

                var pawnRef = pawn;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.RemoveModifier("citadel_change_team");
                        Console.WriteLine($"[KSYX] {player.PlayerName} -> 移除 citadel_change_team modifier");
                    }
                });
            }
            else if (pawn != null && pawn.TeamNum == 2)
            {
                Console.WriteLine($"[KSYX] {player.PlayerName} 已经是 Team 2，跳过");
            }
            else
            {
                Console.WriteLine($"[KSYX] {player.PlayerName} 没有英雄实体，跳过");
            }
        }

        int seconds = 15;
        Console.WriteLine($"[KSYX] 开始倒计时: {seconds}秒");

        SendGlobalChatMessage($"母体还有 {seconds} 秒后出现");
        Console.WriteLine($"[KSYX] 已发送初始聊天消息，固定发送者已确定");

        var timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;
            Console.WriteLine($"[KSYX] 倒计时: {seconds}秒");

            if (seconds > 0)
            {
                SendGlobalChatMessage($"母体还有 {seconds} 秒后出现");
            }
        });

        Timer.Once(15.Seconds(), () =>
        {
            Console.WriteLine($"[KSYX] ========== 倒计时结束 ==========");
            timer.Cancel();
            Console.WriteLine($"[KSYX] 计时器已取消");

            SendGlobalChatMessage("母体来了！");
            Console.WriteLine($"[KSYX] 已发送结束消息");

            Console.WriteLine($"[KSYX] 开始选择母体...");
            var team2Players = allPlayers
                .Where(p => p.GetHeroPawn() != null && p.GetHeroPawn()?.TeamNum == 2)
                .ToList();

            Console.WriteLine($"[KSYX] Team 2 玩家数: {team2Players.Count}");

            if (team2Players.Count == 0)
            {
                Console.WriteLine($"[KSYX] 没有Team 2玩家，无法选择母体");
                isGameRunning = false;
                return;
            }

            var random = new Random();
            var selected = team2Players[random.Next(team2Players.Count)];
            Console.WriteLine($"[KSYX] 选中: {selected.PlayerName}");

            var selectedPawn = selected.GetHeroPawn();
            if (selectedPawn != null)
            {
                Console.WriteLine($"[KSYX] 保存 {selected.PlayerName} 的装备...");
                var items = new List<string>();
                var abilityComponent = selectedPawn.AbilityComponent;
                if (abilityComponent != null)
                {
                    foreach (var ability in abilityComponent.Abilities)
                    {
                        if (ability.IsItem)
                        {
                            var itemName = ability.AbilityName;
                            if (!string.IsNullOrEmpty(itemName))
                            {
                                items.Add(itemName);
                                Console.WriteLine($"[KSYX] 找到装备: {itemName}");
                            }
                        }
                    }
                }
                playerItems[selected.Slot] = items;
                Console.WriteLine($"[KSYX] 共保存 {items.Count} 件装备");

                using var kv = new KeyValues3();
                kv.SetInt("team", 3);
                selectedPawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {selected.PlayerName} -> Team 3");

                var pawnRef = selectedPawn;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.RemoveModifier("citadel_change_team");
                        Console.WriteLine($"[KSYX] {selected.PlayerName} -> 移除 citadel_change_team modifier");
                    }
                });
            }

            selected.SelectHero(Heroes.Necro);
            Console.WriteLine($"[KSYX] {selected.PlayerName} -> Necro");

            var selectedSlot = selected.Slot;
            Timer.Once(3.Seconds(), () =>
            {
                Console.WriteLine($"[KSYX] 开始重新给 {selected.PlayerName} 装备...");
                var pawn = selected.GetHeroPawn();
                if (pawn != null && pawn.IsValid && playerItems.TryGetValue(selectedSlot, out var items))
                {
                    foreach (var itemName in items)
                    {
                        pawn.AddItem(itemName);
                        Console.WriteLine($"[KSYX] 重新给予装备: {itemName}");
                    }
                    Console.WriteLine($"[KSYX] 共重新给予 {items.Count} 件装备");
                }
                else
                {
                    Console.WriteLine($"[KSYX] 没有找到保存的装备");
                }
            });

            // ========== 启动 Team 3 周期性 Buff（母体出现后才启动） ==========
            Console.WriteLine($"[KSYX] 母体已出现，启动 Team 3 周期性 Buff...");
            StartTeam3BuffTimer();
            // ========== Buff 启动结束 ==========

            var hudMsg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "",
                DescriptionLocstring = $"{selected.PlayerName} 变成了母体！"
            };

            foreach (var player in allPlayers)
            {
                NetMessages.Send(hudMsg, RecipientFilter.Single(player.EntityIndex - 1));
            }

            Console.WriteLine($"[KSYX] 已广播: {selected.PlayerName} 变成了母体！");
            Console.WriteLine($"[KSYX] ========== 流程结束 ==========");
        });

        Console.WriteLine($"[KSYX] 命令执行完成，等待倒计时...");
    }

    public void SendGlobalChatMessage(string text)
    {
        if (allPlayers.Count == 0) return;

        if (fixedSender == null)
        {
            var random = new Random();
            fixedSender = allPlayers[random.Next(allPlayers.Count)];
            Console.WriteLine($"[KSYX] 固定发送者确定为: {fixedSender.PlayerName}");
        }

        var msg = new CCitadelUserMsg_ChatMsg
        {
            Text = text,
            PlayerSlot = fixedSender.Slot
        };

        NetMessages.Send(msg, RecipientFilter.All);
    }
}
