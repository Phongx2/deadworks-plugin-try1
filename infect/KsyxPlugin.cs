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
                var currentHero = GetHeroFromPawn(pawn);
                if (currentHero.HasValue)
                {
                    originalHeroes[player] = currentHero.Value;
                    Console.WriteLine($"[KSYX] {player.PlayerName} -> 当前英雄: {currentHero.Value}");
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

        // 所有玩家移到 Team 2
        Console.WriteLine($"[KSYX] 开始移动玩家到 Team 2...");
        foreach (var player in allPlayers)
        {
            player.ChangeTeam(2);
            Console.WriteLine($"[KSYX] {player.PlayerName} -> Team 2");
        }

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

        // ========== 给所有玩家发放 32000 金币 ==========
        Console.WriteLine($"[KSYX] 开始发放金币...");
        foreach (var player in allPlayers)
        {
            // 通过控制台命令给每个玩家发金币
            ConVar.Find($"citadel_give_gold {player.Slot} 32000")?.SetInt(0);
            Console.WriteLine($"[KSYX] {player.PlayerName} (槽位 {player.Slot}) -> +32000 金币");
        }
        // ========== 发放结束 ==========

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

            selected.ChangeTeam(3);
            Console.WriteLine($"[KSYX] {selected.PlayerName} -> Team 3");

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
