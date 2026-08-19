using DeadworksManaged.Api;
using System.Numerics;

namespace RestartPlugin;

public class RestartPlugin : DeadworksPluginBase
{
    public override string Name => "Restart";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Restart] 热重载完成！" : "[Restart] 已加载！");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Restart] 已卸载！");
    }

    // ========== 命令：/r 或 !r ==========
    [Command("r", Description = "执行换图到 dl_midtown", SuppressChat = true)]
    public void CmdRestart(CCitadelPlayerController caller)
    {
        // 获取执行者名称
        string playerName = caller?.PlayerName ?? "Server Console";
        Console.WriteLine($"[Restart] {playerName} 执行了换图命令");

        // 通知所有玩家
        CCitadelPlayerController.PrintToConsoleAll($"[Restart] {playerName} 正在换图到 dl_midtown...");

        // 执行换图
        try
        {
            Server.ExecuteCommand("changelevel dl_midtown");
            Console.WriteLine("[Restart] 换图命令已发送");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Restart] 换图失败: {ex.Message}");
            CCitadelPlayerController.PrintToConsoleAll($"[Restart] 换图失败: {ex.Message}");
        }
    }
}
