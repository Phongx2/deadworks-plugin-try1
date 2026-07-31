using DeadworksManaged.Api;

namespace KsyxCountdown;

public class KsyxPlugin : DeadworksPluginBase
{
    public override string Name => "KSYX";

    public List<CCitadelPlayerController> allPlayers = new List<CCitadelPlayerController>();

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

        // ========== 设置必要的 ConVar ==========
        Console.WriteLine($"[KSYX] 设置 ConVar...");
        ConVar.Find("sv_cheats")?.SetInt(1);
        Console.WriteLine($"[KSYX] sv_cheats -> 1");
        
        ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);
        Console.WriteLine($"[KSYX] citadel_allow_purchasing_anywhere -> 1");
        // ========== ConVar 设置结束 ==========

        // 所有玩家移到 Team 2（使用 modifier）
        Console.WriteLine($"[KSYX] 开始移动玩家到 Team 2...");
        foreach (var player in allPlayers)
        {
            var pawn = player.GetHeroPawn();
            if (pawn != null)
            {
                using var kv = new KeyValues3();
                kv.SetInt("team", 2);
                pawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {player.PlayerName} -> Team 2");
            }
            else
            {
                Console.WriteLine($"[KSYX] {player.PlayerName} -> 没有英雄实体");
            }
        }

        // ========== 给所有玩家发放 32000 金币 ==========
        Console.WriteLine($"[KSYX] 开始发放金币...");
        foreach (var player in allPlayers)
        {
            ConVar.Find("citadel_give_gold")?.SetInt(32000);
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

            // 选中的玩家移到 Team 3（使用 modifier）
            var selectedPawn = selected.GetHeroPawn();
            if (selectedPawn != null)
            {
                using var kv = new KeyValues3();
                kv.SetInt("team", 3);
                selectedPawn.AddModifier("citadel_change_team", kv);
                Console.WriteLine($"[KSYX] {selected.PlayerName} -> Team 3");
            }

            selected.SelectHero(Heroes.Necro);
            Console.WriteLine($"[KSYX] {selected.PlayerName} -> Necro");

            // ========== 母体出现后关闭 sv_cheats ==========
            ConVar.Find("sv_cheats")?.SetInt(0);
            Console.WriteLine($"[KSYX] sv_cheats -> 0");
            // ========== 关闭结束 ==========

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
