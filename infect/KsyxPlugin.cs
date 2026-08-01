using DeadworksManaged.Api;

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

    // ========== 周期性给 Team 3 添加 Buff ==========
    private void StartTeam3BuffTimer()
    {
        team3BuffTimer?.Cancel();

        team3BuffTimer = Timer.Every(1.Seconds(), () =>
        {
            if (!isGameRunning)
            {
                team3BuffTimer?.Cancel();
                team3BuffTimer = null;
                return;
            }

            var team3Pawns = Players.GetAllPawns()
                .Where(p => p != null && p.IsValid && p.TeamNum == 3)
                .ToList();

            if (team3Pawns.Count == 0) return;

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

            // ========== 启动 Team 3 周期性 Buff ==========
            Console.WriteLine($"[KSYX] 启动 Team 3 周期性 Buff...");
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
