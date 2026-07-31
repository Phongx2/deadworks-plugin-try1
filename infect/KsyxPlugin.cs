using DeadworksManaged.Api;

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

    public List<CCitadelPlayerController> allPlayers = new List<CCitadelPlayerController>();
    public Dictionary<int, List<string>> playerItems = new Dictionary<int, List<string>>();

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[KSYX] ========== 插件加载 ==========");
        Console.WriteLine($"[KSYX] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        Console.WriteLine($"[KSYX] ===============================");
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[KSYX] 插件卸载");
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

        // 设置 sv_cheats = 1
        Console.WriteLine($"[KSYX] 设置 sv_cheats = 1");
        ConVar.Find("sv_cheats")?.SetInt(1);

        // 延迟 0.5 秒后发放金币
        Timer.Once(500.Milliseconds(), () =>
        {
            Console.WriteLine($"[KSYX] 开始发放金币...");
            
            ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);
            Console.WriteLine($"[KSYX] citadel_allow_purchasing_anywhere -> 1");

            // 使用 SetCurrency 给所有玩家 32000 金币
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

        // 只把 Team 3 的玩家移到 Team 2
        Console.WriteLine($"[KSYX] 开始将 Team 3 玩家移到 Team 2...");
        foreach (var player in allPlayers)
        {
            var pawn = player.GetHeroPawn();
            if (pawn != null && pawn.TeamNum == 3)
            {
                using var kv = new KeyValues3();
                kv.SetInt("team", 2);
                pawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {player.PlayerName} (Team 3) -> Team 2");

                // ========== 延迟 1 秒后关闭 citadel_change_team modifier 状态 ==========
                var pawnRef = pawn; // 捕获当前 pawn
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.ModifierProp?.SetModifierState(EModifierState.CitadelChangeTeam, false);
                        Console.WriteLine($"[KSYX] {player.PlayerName} -> 关闭 citadel_change_team 状态");
                    }
                });
                // ========== 关闭结束 ==========
            }
            else
            {
                Console.WriteLine($"[KSYX] {player.PlayerName} 不是 Team 3，跳过");
            }
        }

        int seconds = 15;
        Console.WriteLine($"[KSYX] 开始倒计时: {seconds}秒");

        SendGlobalChatMessage($"母体还有 {seconds} 秒后出现");
        Console.WriteLine($"[KSYX] 已发送初始聊天消息");

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
                return;
            }

            var random = new Random();
            var selected = team2Players[random.Next(team2Players.Count)];
            Console.WriteLine($"[KSYX] 选中: {selected.PlayerName}");

            var selectedPawn = selected.GetHeroPawn();
            if (selectedPawn != null)
            {
                // 保存该玩家的装备
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

                // 选中的玩家移到 Team 3（使用 modifier）
                using var kv = new KeyValues3();
                kv.SetInt("team", 3);
                selectedPawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {selected.PlayerName} -> Team 3");

                // ========== 延迟 1 秒后关闭 citadel_change_team modifier 状态 ==========
                var pawnRef = selectedPawn;
                Timer.Once(1.Seconds(), () =>
                {
                    if (pawnRef != null && pawnRef.IsValid)
                    {
                        pawnRef.ModifierProp?.SetModifierState(EModifierState.CitadelChangeTeam, false);
                        Console.WriteLine($"[KSYX] {selected.PlayerName} -> 关闭 citadel_change_team 状态");
                    }
                });
                // ========== 关闭结束 ==========
            }

            selected.SelectHero(Heroes.Necro);
            Console.WriteLine($"[KSYX] {selected.PlayerName} -> Necro");

            // 延迟 0.5 秒后重新给装备
            Timer.Once(500.Milliseconds(), () =>
            {
                Console.WriteLine($"[KSYX] 开始重新给 {selected.PlayerName} 装备...");
                var pawn = selected.GetHeroPawn();
                if (pawn != null && playerItems.TryGetValue(selected.Slot, out var items))
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

            // 关闭 sv_cheats
            ConVar.Find("sv_cheats")?.SetInt(0);
            Console.WriteLine($"[KSYX] sv_cheats -> 0");

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

        var random = new Random();
        var sender = allPlayers[random.Next(allPlayers.Count)];

        var msg = new CCitadelUserMsg_ChatMsg
        {
            Text = text,
            PlayerSlot = sender.Slot
        };

        NetMessages.Send(msg, RecipientFilter.All);
    }
}
