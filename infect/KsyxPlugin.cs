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
        // 获取所有玩家
        var players = Players.GetAll().ToList();
        if (players.Count == 0) return;

        // 初始显示15秒
        int seconds = 15;
        SendCountdown(players, seconds);

        // 使用完整命名空间明确指定是 Deadworks 的 ITimer
        DeadworksManaged.Api.ITimer timer = null;

        // 每秒更新一次
        timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;

            if (seconds >= 0)
            {
                SendCountdown(players, seconds);
            }

            if (seconds <= 0)
            {
                timer.Cancel();
                SendFinalMessage(players, "僵尸来了！");
            }
        });
    }

    // 发送倒计时公告
    public void SendCountdown(List<CCitadelPlayerController> players, int seconds)
    {
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🧟 僵尸还有",
            DescriptionLocstring = $"{seconds} 秒后出现"
        };

        foreach (var player in players)
        {
            NetMessages.Send(msg, RecipientFilter.Single(player.Slot));
        }
    }

    // 发送最终消息
    public void SendFinalMessage(List<CCitadelPlayerController> players, string text)
    {
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🧟",
            DescriptionLocstring = text
        };

        foreach (var player in players)
        {
            NetMessages.Send(msg, RecipientFilter.Single(player.Slot));
        }
    }
}
