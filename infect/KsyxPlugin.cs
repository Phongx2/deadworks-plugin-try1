using DeadworksManaged.Api;

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

    public List<CCitadelPlayerController> allPlayers = new List<CCitadelPlayerController>();
    public Dictionary<CCitadelPlayerController, Heroes> originalHeroes = new Dictionary<CCitadelPlayerController, Heroes>();

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

        // 保存每个玩家的当前英雄
        Console.WriteLine($"[KSYX] 开始保存玩家英雄...");
        originalHeroes.Clear();

        foreach (var player in allPlayers)
        {
            var pawn = player.GetHeroPawn();
            if (pawn != null)
            {
                var hero = GetHeroFromPawn(pawn);
                if (hero.HasValue)
                {
                    originalHeroes[player] = hero.Value;
                    Console.WriteLine($"[KSYX] {player.PlayerName} -> 当前英雄: {hero.Value}");
                }
                else
                {
                    Console.WriteLine($"[KSYX] {player.PlayerName} -> 无法获取英雄，使用默认 Inferno");
                    originalHeroes[player] = Heroes.Inferno;
                }
            }
            else
            {
                Console.WriteLine($"[KSYX] {player.PlayerName} -> 没有英雄实体，使用默认 Inferno");
                originalHeroes[player] = Heroes.Inferno;
            }
        }

        // ========== 修复：使用 citadel_change_team 修改器切换队伍 ==========
        Console.WriteLine($"[KSYX] 开始移动玩家到 Team 2...");
        foreach (var player in allPlayers)
        {
            var pawn = player.GetHeroPawn();
            if (pawn != null)
            {
                // 使用 modifier 切换队伍到 Team 2
                using var kv = new KeyValues3();
                kv.SetInt("team", 2);  // 设置目标队伍
                pawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {player.PlayerName} -> Team 2 (通过 modifier)");
            }
            else
            {
                Console.WriteLine($"[KSYX] {player.PlayerName} -> 没有英雄实体，无法切换队伍");
            }
        }
        // ========== 修复结束 ==========

        // 重新选择原始英雄
        Console.WriteLine($"[KSYX] 开始重新选择英雄...");
        foreach (var player in allPlayers)
        {
            if (originalHeroes.TryGetValue(player, out var hero))
            {
                player.SelectHero(hero);
                Console.WriteLine($"[KSYX] {player.PlayerName} -> 重新选择英雄: {hero}");
            }
        }

        // 给所有玩家发放 32000 金币
        Console.WriteLine($"[KSYX] 开始发放金币...");
        foreach (var player in allPlayers)
        {
            Server.ExecuteCommand($"citadel_give_gold {player.Slot} 32000");
            Console.WriteLine($"[KSYX] {player.PlayerName} (槽位 {player.Slot}) -> +32000 金币");
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

            // ========== 修复：使用 modifier 切换队伍到 Team 3 ==========
            var selectedPawn = selected.GetHeroPawn();
            if (selectedPawn != null)
            {
                using var kv = new KeyValues3();
                kv.SetInt("team", 3);
                selectedPawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {selected.PlayerName} -> Team 3 (通过 modifier)");
            }
            // ========== 修复结束 ==========

            selected.SelectHero(Heroes.Necro);
            Console.WriteLine($"[KSYX] {selected.PlayerName} -> Necro");

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

    public Heroes? GetHeroFromPawn(CCitadelPlayerPawn pawn)
    {
        var vdata = pawn.SubclassVData;
        if (vdata != null)
        {
            var name = vdata.Name;
            Console.WriteLine($"[KSYX] 英雄VData名称: {name}");
            
            if (name.Contains("necro", StringComparison.OrdinalIgnoreCase))
                return Heroes.Necro;
            if (name.Contains("inferno", StringComparison.OrdinalIgnoreCase))
                return Heroes.Inferno;
        }

        var designerName = pawn.DesignerName;
        Console.WriteLine($"[KSYX] 英雄DesignerName: {designerName}");
        if (designerName.Contains("necro", StringComparison.OrdinalIgnoreCase))
            return Heroes.Necro;
        if (designerName.Contains("inferno", StringComparison.OrdinalIgnoreCase))
            return Heroes.Inferno;

        return null;
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
