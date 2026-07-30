using DeadworksManaged.Api;

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

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
        Console.WriteLine($"[KSYX] 执行者槽位: {(caller != null ? caller.Slot : -1)}");

        // 获取所有玩家
        var players = Players.GetAll().ToList();
        Console.WriteLine($"[KSYX] 在线玩家数: {players.Count}");

        if (players.Count == 0)
        {
            Console.WriteLine($"[KSYX] 没有玩家，命令终止");
            return;
        }

        // 打印所有玩家名字
        foreach (var p in players)
        {
            Console.WriteLine($"[KSYX] 玩家: {p.PlayerName}, 槽位: {p.Slot}, 队伍: {p.GetHeroPawn()?.TeamNum ?? -1}");
        }

        // 把所有玩家移到 Team 2
        Console.WriteLine($"[KSYX] 开始移动玩家到 Team 2...");
        foreach (var player in players)
        {
            player.ChangeTeam(2);
            Console.WriteLine($"[KSYX] {player.PlayerName} -> Team 2");
        }

        int seconds = 15;
        Console.WriteLine($"[KSYX] 开始倒计时: {seconds}秒");

        // 发送初始HUD
        SendHUD(players, "僵尸还有", $"{seconds} 秒后出现");
        Console.WriteLine($"[KSYX] 已发送初始HUD");

        var timer = Timer.Every(1.Seconds(), () =>
        {
            seconds--;
            Console.WriteLine($"[KSYX] 倒计时: {seconds}秒");

            if (seconds > 0)
            {
                SendHUD(players, "僵尸还有", $"{seconds} 秒后出现");
            }
        });

        Timer.Once(15.Seconds(), () =>
        {
            Console.WriteLine($"[KSYX] ========== 倒计时结束 ==========");
            timer.Cancel();
            Console.WriteLine($"[KSYX] 计时器已取消");

            SendHUD(players, "僵尸来了！", "");
            Console.WriteLine($"[KSYX] 已发送结束HUD");

            // 选僵尸
            Console.WriteLine($"[KSYX] 开始选择僵尸...");
            var team2Players = Players.GetAll()
                .Where(p => p.GetHeroPawn() != null && p.GetHeroPawn()?.TeamNum == 2)
                .ToList();

            Console.WriteLine($"[KSYX] Team 2 玩家数: {team2Players.Count}");

            if (team2Players.Count == 0)
            {
                Console.WriteLine($"[KSYX] 没有Team 2玩家，无法选择僵尸");
                return;
            }

            var random = new Random();
            var selected = team2Players[random.Next(team2Players.Count)];
            Console.WriteLine($"[KSYX] 选中: {selected.PlayerName}");

            // 切换队伍
            selected.ChangeTeam(3);
            Console.WriteLine($"[KSYX] {selected.PlayerName} -> Team 3");

            // 切换英雄
            selected.SelectHero(Heroes.Inferno);
            Console.WriteLine($"[KSYX] {selected.PlayerName} -> Inferno");

            // 广播
            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "",
                DescriptionLocstring = $"{selected.PlayerName} 变成了僵尸！"
            };

            foreach (var player in Players.GetAll())
            {
                NetMessages.Send(msg, RecipientFilter.Single(player.EntityIndex - 1));
            }

            Console.WriteLine($"[KSYX] 已广播: {selected.PlayerName} 变成了僵尸！");
            Console.WriteLine($"[KSYX] ========== 流程结束 ==========");
        });

        Console.WriteLine($"[KSYX] 命令执行完成，等待倒计时...");
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
