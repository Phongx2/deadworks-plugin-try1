using DeadworksManaged.Api;
using System.Numerics;

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

    public List<CCitadelPlayerController> allPlayers = new List<CCitadelPlayerController>();
    public Dictionary<int, List<string>> playerItems = new Dictionary<int, List<string>>();
    public CCitadelPlayerController? fixedSender = null;
    public IHandle? team3BuffTimer = null;
    public IHandle? deathDetectionTimer = null; // 新增：死亡检测计时器
    public bool isGameRunning = false;
    public bool isTeam2CheckEnabled = false;
    public bool isMeleeInfectionEnabled = true;
    public bool isLastOne = false;

    // ========== 死亡检测相关 ==========
    private HashSet<CCitadelPlayerController> _deadPlayers = new HashSet<CCitadelPlayerController>();
    private bool _deathLogging = true;
    private bool _isDeathDetectionEnabled = false;

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
        deathDetectionTimer = null;
        isGameRunning = false;
        isTeam2CheckEnabled = false;
        isMeleeInfectionEnabled = true;
        isLastOne = false;
        _isDeathDetectionEnabled = false;
        _deadPlayers.Clear();
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[KSYX] 插件卸载");
        team3BuffTimer?.Cancel();
        team3BuffTimer = null;
        deathDetectionTimer?.Cancel();
        deathDetectionTimer = null;
        isGameRunning = false;
        isTeam2CheckEnabled = false;
        isMeleeInfectionEnabled = true;
        isLastOne = false;
        _isDeathDetectionEnabled = false;
        _deadPlayers.Clear();
    }

    // ========== 死亡检测方法 ==========
    private void CheckPlayerDeath()
    {
        if (!isGameRunning || !_isDeathDetectionEnabled) return;

        var allControllers = Players.GetAll();
        if (allControllers == null) return;

        foreach (var controller in allControllers)
        {
            if (controller == null || !controller.IsValid) continue;

            if (_deadPlayers.Contains(controller)) continue;

            var pawn = controller.GetHeroPawn();
            if (pawn == null || !pawn.IsValid) continue;

            bool isDead = pawn.LifeState == LifeState.Dead || pawn.LifeState == LifeState.Dying;
            if (!isDead && pawn.Health <= 0)
            {
                isDead = true;
            }

            if (isDead)
            {
                controller.ChangeTeam(1);
                
                _deadPlayers.Add(controller);
                
                if (_deathLogging)
                {
                    Console.WriteLine($"[KSYX][死亡] 玩家 {controller.PlayerName} 已死亡，永久移至观战队伍 (Team 1)");
                }

                var hudMsg = new CCitadelUserMsg_HudGameAnnouncement
                {
                    TitleLocstring = "💀",
                    DescriptionLocstring = $"{controller.PlayerName} 已死亡，进入观战模式"
                };
                foreach (var player in allPlayers)
                {
                    NetMessages.Send(hudMsg, RecipientFilter.Single(player.EntityIndex - 1));
                }
            }
        }
    }

    // ========== 启动死亡检测计时器 ==========
    private void StartDeathDetection()
    {
        if (deathDetectionTimer != null)
        {
            deathDetectionTimer.Cancel();
            deathDetectionTimer = null;
        }

        _isDeathDetectionEnabled = true;
        Console.WriteLine($"[KSYX][死亡检测] 已启用");

        deathDetectionTimer = Timer.Every(500.Milliseconds(), () =>
        {
            CheckPlayerDeath();
        });
    }

    // ========== 停止死亡检测计时器 ==========
    private void StopDeathDetection()
    {
        _isDeathDetectionEnabled = false;
        deathDetectionTimer?.Cancel();
        deathDetectionTimer = null;
        Console.WriteLine($"[KSYX][死亡检测] 已停止");
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

    // ========== 监听近战攻击 ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(GameEvent ev)
    {
        if (!isMeleeInfectionEnabled)
        {
            return HookResult.Continue;
        }

        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null)
        {
            return HookResult.Continue;
        }

        string abilityName = ev.GetString("abilityname", "");

        if (!abilityName.StartsWith("ability_melee"))
            return HookResult.Continue;

        if (pawn.TeamNum != 3)
        {
            return HookResult.Continue;
        }

        var attacker = pawn;
        Timer.NextTick(() =>
        {
            if (attacker == null || !attacker.IsValid) return;
            DetectMeleeHitAndInfect(attacker);
        });

        return HookResult.Continue;
    }

    // ========== 检测近战命中的目标并感染 ==========
    private void DetectMeleeHitAndInfect(CCitadelPlayerPawn attacker)
    {
        if (!isMeleeInfectionEnabled)
        {
            return;
        }

        if (attacker == null || !attacker.IsValid) return;

        var team2Pawns = Players.GetAllPawns()
            .Where(p => p != null && p.IsValid && p.TeamNum == 2)
            .ToList();

        if (team2Pawns.Count == 0)
        {
            return;
        }

        Vector3 attackerPos = attacker.Position;
        Vector3 forward = GetForwardVector(attacker.EyeAngles);
        float meleeRange = 250f;
        float angleThreshold = 0.3f;

        foreach (var victim in team2Pawns)
        {
            if (victim == null || !victim.IsValid) continue;

            float distance = Vector3.Distance(attackerPos, victim.Position);
            if (distance > meleeRange)
            {
                continue;
            }

            Vector3 toTarget = victim.Position - attackerPos;
            Vector3 normalizedToTarget = Vector3.Normalize(toTarget);
            float dotProduct = Vector3.Dot(forward, normalizedToTarget);

            if (dotProduct < angleThreshold)
            {
                continue;
            }

            Console.WriteLine($"[KSYX][重要] Team 3 玩家 {GetControllerFromPawn(attacker)?.PlayerName} 近战命中了 Team 2 玩家 {GetControllerFromPawn(victim)?.PlayerName}！");
            
            var victimRef = victim;
            Timer.NextTick(() =>
            {
                if (victimRef != null && victimRef.IsValid)
                {
                    InfectPlayer(victimRef);
                }
            });

            break;
        }
    }

    // ========== 感染转化玩家 ==========
    private void InfectPlayer(CCitadelPlayerPawn victim)
    {
        if (victim == null || !victim.IsValid) return;

        var victimController = GetControllerFromPawn(victim);
        if (victimController == null) return;

        Console.WriteLine($"[KSYX][重要] 开始感染 {victimController.PlayerName}...");

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
                    }
                }
            }
        }
        playerItems[victimController.Slot] = items;

        using var kv = new KeyValues3();
        kv.SetInt("team", 3);
        victim.AddModifier("citadel_change_team", kv);

        var pawnRef = victim;
        Timer.Once(1.Seconds(), () =>
        {
            if (pawnRef != null && pawnRef.IsValid)
            {
                pawnRef.RemoveModifier("citadel_change_team");
            }
        });

        victimController.SelectHero(Heroes.Necro);

        var selectedSlot = victimController.Slot;
        Timer.Once(3.Seconds(), () =>
        {
            var pawn = victimController.GetHeroPawn();
            if (pawn != null && pawn.IsValid && playerItems.TryGetValue(selectedSlot, out var savedItems))
            {
                foreach (var itemName in savedItems)
                {
                    pawn.AddItem(itemName);
                }
            }
        });

        var hudMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "",
            DescriptionLocstring = $"{victimController.PlayerName} 被感染成了僵尸！"
        };

        foreach (var player in allPlayers)
        {
            NetMessages.Send(hudMsg, RecipientFilter.Single(player.EntityIndex - 1));
        }

        if (isTeam2CheckEnabled)
        {
            Timer.NextTick(() => CheckTeam2AndProcess());
        }
    }

    // ========== 周期性给 Team 3 添加 Buff ==========
    private void StartTeam3BuffTimer()
    {
        Console.WriteLine($"[KSYX][DEBUG] StartTeam3BuffTimer 被调用");
        team3BuffTimer?.Cancel();

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
                return;
            }

            foreach (var pawn in team3Pawns)
            {
                using var kv = new KeyValues3();
                kv.SetFloat("duration", 1.1f);
                
                if (!isLastOne)
                {
                    pawn.AddModifier("modifier_citadel_in_fountain", kv);
                }
                
                pawn.AddModifier("modifier_citadel_disarmed", kv);
                pawn.AddModifier("modifier_citadel_silenced", kv);
            }
        });
    }

    // ========== 检查 Team 2 玩家数量并处理 ==========
    private void CheckTeam2AndProcess()
    {
        if (!isGameRunning || !isTeam2CheckEnabled) return;

        var team2Players = allPlayers
            .Where(p => p.GetHeroPawn() != null && p.GetHeroPawn()?.TeamNum == 2)
            .ToList();

        if (team2Players.Count == 1)
        {
            var lastTeam2Player = team2Players[0];

            var pawn = lastTeam2Player.GetHeroPawn();
            if (pawn != null && pawn.IsValid)
            {
                var items = new List<string>();
                var abilityComponent = pawn.AbilityComponent;
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
                            }
                        }
                    }
                }
                playerItems[lastTeam2Player.Slot] = items;

                isLastOne = true;

                var team3Pawns = Players.GetAllPawns()
                    .Where(p => p != null && p.IsValid && p.TeamNum == 3)
                    .ToList();

                foreach (var team3Pawn in team3Pawns)
                {
                    team3Pawn.RemoveModifier("modifier_citadel_in_fountain");
                }

                isMeleeInfectionEnabled = false;

                lastTeam2Player.SelectHero(Heroes.Priest);

                var slot = lastTeam2Player.Slot;
                Timer.Once(2.Seconds(), () =>
                {
                    var pawnToRestore = lastTeam2Player.GetHeroPawn();
                    if (pawnToRestore != null && pawnToRestore.IsValid && playerItems.TryGetValue(slot, out var savedItems))
                    {
                        foreach (var itemName in savedItems)
                        {
                            pawnToRestore.AddItem(itemName);
                        }
                    }
                });

                var pawnForAbility = lastTeam2Player.GetHeroPawn();
                if (pawnForAbility != null && pawnForAbility.IsValid)
                {
                    var abilityComponent2 = pawnForAbility.AbilityComponent;
                    if (abilityComponent2 != null)
                    {
                        foreach (var ability in abilityComponent2.Abilities)
                        {
                            if (ability == null) continue;
                            if (ability.AbilityName == "ability_priest_weaponswap")
                            {
                                ability.CooldownStart = 0;
                                ability.CooldownEnd = 0;
                                break;
                            }
                        }
                    }
                }

                // ========== 启动死亡检测 ==========
                StartDeathDetection();

                var hudMsg = new CCitadelUserMsg_HudGameAnnouncement
                {
                    TitleLocstring = "⚔️",
                    DescriptionLocstring = $"{lastTeam2Player.PlayerName} 成为了最后的幸存者！"
                };
                foreach (var player in allPlayers)
                {
                    NetMessages.Send(hudMsg, RecipientFilter.Single(player.EntityIndex - 1));
                }
            }
        }
    }

    [Command("ksyx", Description = "僵尸倒计时")]
    public void CmdKsyx(CCitadelPlayerController caller)
    {
        allPlayers = Players.GetAll().ToList();

        if (allPlayers.Count == 0)
        {
            Console.WriteLine($"[KSYX] 没有玩家，命令终止");
            return;
        }

        fixedSender = null;
        isGameRunning = true;
        isTeam2CheckEnabled = false;
        isMeleeInfectionEnabled = true;
        isLastOne = false;
        _isDeathDetectionEnabled = false;
        _deadPlayers.Clear();
        StopDeathDetection();

        ConVar.Find("sv_cheats")?.SetInt(1);

        Timer.Once(500.Milliseconds(), () =>
        {
            ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);

            foreach (var player in allPlayers)
            {
                var pawn = player.GetHeroPawn();
                if (pawn != null)
                {
                    pawn.SetCurrency(ECurrencyType.EGold, 32000);
                }
            }
        });

        foreach (var player in allPlayers)
        {
            var pawn = player.GetHeroPawn();
            if (pawn != null && pawn.TeamNum == 3)
            {
                using var kv = new KeyValues3();
                kv.SetInt("team", 2);
                pawn.AddModifier("citadel_change_team", kv);

                var pawnRef = pawn;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.RemoveModifier("citadel_change_team");
                    }
                });
            }
        }

        int seconds = 30;

        SendGlobalChatMessage($"母体还有 {seconds} 秒后出现");

        var timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;

            if (seconds > 0)
            {
                SendGlobalChatMessage($"母体还有 {seconds} 秒后出现");
            }
        });

        Timer.Once(30.Seconds(), () =>
        {
            timer.Cancel();

            SendGlobalChatMessage("母体来了！");

            var team2Players = allPlayers
                .Where(p => p.GetHeroPawn() != null && p.GetHeroPawn()?.TeamNum == 2)
                .ToList();

            if (team2Players.Count == 0)
            {
                isGameRunning = false;
                return;
            }

            var random = new Random();
            var selected = team2Players[random.Next(team2Players.Count)];

            var selectedPawn = selected.GetHeroPawn();
            if (selectedPawn != null)
            {
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
                            }
                        }
                    }
                }
                playerItems[selected.Slot] = items;

                using var kv = new KeyValues3();
                kv.SetInt("team", 3);
                selectedPawn.AddModifier("citadel_change_team", kv);

                var pawnRef = selectedPawn;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.RemoveModifier("citadel_change_team");
                    }
                });
            }

            selected.SelectHero(Heroes.Necro);

            var selectedSlot = selected.Slot;
            Timer.Once(3.Seconds(), () =>
            {
                var pawn = selected.GetHeroPawn();
                if (pawn != null && pawn.IsValid && playerItems.TryGetValue(selectedSlot, out var items))
                {
                    foreach (var itemName in items)
                    {
                        pawn.AddItem(itemName);
                    }
                }
            });

            StartTeam3BuffTimer();

            isTeam2CheckEnabled = true;

            var hudMsg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "",
                DescriptionLocstring = $"{selected.PlayerName} 变成了母体！"
            };

            foreach (var player in allPlayers)
            {
                NetMessages.Send(hudMsg, RecipientFilter.Single(player.EntityIndex - 1));
            }
        });
    }

    // ========== /r 命令：取消 Team 3 周期性 Buff ==========
    [Command("r", Description = "取消Team 3周期性Buff")]
    public void CmdCancelBuff(CCitadelPlayerController caller)
    {
        if (team3BuffTimer == null)
        {
            if (caller != null)
            {
                caller.PrintToConsole("没有正在运行的Buff计时器");
            }
            return;
        }

        team3BuffTimer.Cancel();
        team3BuffTimer = null;

        if (caller != null)
        {
            caller.PrintToConsole("Team 3 周期性Buff已取消");
        }

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "⛔",
            DescriptionLocstring = "僵尸 Buff 已被取消！"
        };
        foreach (var player in allPlayers)
        {
            NetMessages.Send(msg, RecipientFilter.Single(player.EntityIndex - 1));
        }
    }

    public void SendGlobalChatMessage(string text)
    {
        if (allPlayers.Count == 0) return;

        if (fixedSender == null)
        {
            var random = new Random();
            fixedSender = allPlayers[random.Next(allPlayers.Count)];
        }

        var msg = new CCitadelUserMsg_ChatMsg
        {
            Text = text,
            PlayerSlot = fixedSender.Slot
        };

        NetMessages.Send(msg, RecipientFilter.All);
    }
}
