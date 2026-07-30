using DeadworksManaged.Api;

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] Loaded! (reload={isReload})");
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] Unloaded!");
    }

    [Command("ksyx", Description = "僵尸倒计时")]
    public void CmdKsyx(CCitadelPlayerController caller)
    {
        var players = Players.GetAll().ToList();
        if (players.Count == 0) return;

        // 所有玩家移到 Team 2
        foreach (var player in players)
        {
            player.ChangeTeam(2);
        }

        int seconds = 15;

        // 显示初始倒计时
        SendHUD(players, "僵尸还有", $"{seconds} 秒后出现");

        var timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;
            if (seconds > 0)
            {
                SendHUD(players, "僵尸还有", $"{seconds} 秒后出现");
            }
        });

        Timer.Once(15.Seconds(), () =>
        {
            timer.Cancel();
            SendHUD(players, "僵尸来了！", "");

            // 从 Team 2 随机选一名玩家
            var team2Players = Players.GetAll()
                .Where(p => p.GetHeroPawn() != null && p.GetHeroPawn()?.TeamNum == 2)
                .ToList();

            if (team2Players.Count > 0)
            {
                var random = new Random();
                var selected = team2Players[random.Next(team2Players.Count)];

                selected.ChangeTeam(3);
                selected.SelectHero(Heroes.Inferno);  // 先用 Inferno 测试

                // 广播消息 - 直接用 NetMessages.Send，和 RollTheDice 一样
                var msg = new CCitadelUserMsg_HudGameAnnouncement
                {
                    TitleLocstring = "",
                    DescriptionLocstring = $"{selected.PlayerName} 变成了僵尸！"
                };

                foreach (var player in Players.GetAll())
                {
                    NetMessages.Send(msg, RecipientFilter.Single(player.EntityIndex - 1));
                }

                Console.WriteLine($"[KSYX] {selected.PlayerName} 被选为僵尸");
            }
        });
    }

    // 发送 HUD 公告
    public void SendHUD(List<CCitadelPlayerController> players, string title, string desc)
    {
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = title,
            DescriptionLocstring = desc
        };

        foreach (var player in players)
        {
            NetMessages.Send(msg, RecipientFilter.Single(player.EntityIndex - 1));
        }
    }
}
