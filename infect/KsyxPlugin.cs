using DeadworksManaged.Api;

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

    // 固定ID，用于更新同一条HUD
    private const int ANNOUNCEMENT_ID = 9999;

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

        var players = Players.GetAll().ToList();
        Console.WriteLine($"[KSYX] 在线玩家数: {players.Count}");

        if (players.Count == 0)
        {
            Console.WriteLine($"[KSYX] 没有玩家，命令终止");
            return;
        }

        foreach (var player in players)
        {
            player.ChangeTeam(2);
            Console.WriteLine($"[KSYX] {player.PlayerName} -> Team 2");
        }

        int seconds = 15;
        Console.WriteLine($"[KSYX] 开始倒计时: {seconds}秒");

        // 发送初始HUD（使用固定ID）
        SendHUD(players, $"母体还有 {seconds} 秒后出现");
        Console.WriteLine($"[KSYX] 已发送初始HUD");

        var timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;
            Console.WriteLine($"[KSYX] 倒计时: {seconds}秒");

            if (seconds > 0)
            {
                // 更新同一条HUD
                SendHUD(players, $"母体还有 {seconds} 秒后出现");
            }
        });

        Timer.Once(15.Seconds(), () =>
        {
            Console.WriteLine($"[KSYX] ========== 倒计时结束 ==========");
            timer.Cancel();
            Console.WriteLine($"[KSYX] 计时器已取消");

            // 显示最终消息
            SendHUD(players, "母体来了！");
            Console.WriteLine($"[KSYX] 已发送结束HUD");

            Console.WriteLine($"[KSYX] 开始选择母体...");
            var team2Players = Players.GetAll()
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

            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "",
                DescriptionLocstring = $"{selected.PlayerName} 变成了母体！"
            };

            foreach (var player in Players.GetAll())
            {
                NetMessages.Send(msg, RecipientFilter.Single(player.EntityIndex - 1));
            }

            Console.WriteLine($"[KSYX] 已广播: {selected.PlayerName} 变成了母体！");
            Console.WriteLine($"[KSYX] ========== 流程结束 ==========");
        });

        Console.WriteLine($"[KSYX] 命令执行完成，等待倒计时...");
    }

    // ========== 发送单行HUD（不带标题） ==========
    public void SendHUD(List<CCitadelPlayerController> players, string text)
    {
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "",           // 标题留空
            DescriptionLocstring = text,   // 只显示描述文字
            AnnouncementID = ANNOUNCEMENT_ID  // 固定ID，覆盖旧消息
        };

        foreach (var player in players)
        {
            NetMessages.Send(msg, RecipientFilter.Single(player.EntityIndex - 1));
        }
    }

    // ========== 重载：发送两行HUD（用于最终消息） ==========
    public void SendHUD(List<CCitadelPlayerController> players, string title, string desc)
    {
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = title,
            DescriptionLocstring = desc,
            AnnouncementID = ANNOUNCEMENT_ID  // 固定ID，覆盖旧消息
        };

        foreach (var player in players)
        {
            NetMessages.Send(msg, RecipientFilter.Single(player.EntityIndex - 1));
        }
    }
}
