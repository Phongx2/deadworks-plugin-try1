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

        int seconds = 15;

        SendHUD(players, "🧟 僵尸还有", $"{seconds} 秒后出现");

        // 直接用 Timer.Every，返回 IHandle
        var timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;
            if (seconds > 0)
            {
                SendHUD(players, "🧟 僵尸还有", $"{seconds} 秒后出现");
            }
            else
            {
                timer.Cancel();  // 用 timer.Cancel() 停止
                SendHUD(players, "🧟", "僵尸来了！");
            }
        });
    }

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
