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
        SendCountdown(players, seconds);

        // 直接使用 ITimer 类型，using DeadworksManaged.Api; 已确保其正确性
        ITimer timer = null;

        timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;

            if (seconds >= 0)
            {
                SendCountdown(players, seconds);
            }

            if (seconds <= 0)
            {
                // 现在可以正确调用 Cancel() 方法了
                timer.Cancel();
                SendFinalMessage(players, "僵尸来了！");
            }
        });
    }

    // ... 其余方法 (SendCountdown, SendFinalMessage) 保持不变 ...
}
