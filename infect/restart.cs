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
        // 清理所有正在进行的计时器
        _restartTimers.Clear();
    }

    // ========== 存储每个玩家的重启状态 ==========
    private class RestartState
    {
        public IHandle? HudTimer;
        public IHandle? ExecuteTimer;
        public int Countdown;

        public RestartState(int countdown)
        {
            Countdown = countdown;
            HudTimer = null;
            ExecuteTimer = null;
        }
    }

    // 使用可空类型作为字典键
    private readonly Dictionary<CCitadelPlayerController?, RestartState> _restartTimers = new();

    // ========== 命令：/r 或 !r ==========
    [Command("r", Description = "3秒后重置服务器并换图到 dl_midtown", SuppressChat = true)]
    public void CmdRestart(CCitadelPlayerController? caller)
    {
        // 获取执行者名称
        string playerName = caller?.PlayerName ?? "Server Console";
        Console.WriteLine($"[Restart] {playerName} 执行了重置命令");

        // 检查是否已有正在进行的重启（包括来自控制台的）
        if (_restartTimers.ContainsKey(caller))
        {
            // 取消之前的计时器
            CancelRestart(caller);
            caller?.PrintToConsole("[Restart] 已取消之前的重启");
            return;
        }

        // 通知所有玩家
        CCitadelPlayerController.PrintToConsoleAll($"[Restart] {playerName} 发起了服务器重置，将在 3 秒后执行");

        // 创建状态
        var state = new RestartState(3);
        _restartTimers[caller] = state;

        // 发送初始 HUD 公告
        SendHUDAnnouncement("🔄 服务器重置", "3 秒后即将重置服务器...");

        // 每秒更新倒计时
        state.HudTimer = Timer.Every(1.Seconds(), () =>
        {
            state.Countdown--;
            Console.WriteLine($"[Restart] 倒计时: {state.Countdown} 秒");

            if (state.Countdown > 0)
            {
                SendHUDAnnouncement("🔄 服务器重置", $"{state.Countdown} 秒后即将重置服务器...");
            }
            else
            {
                // 倒计时结束
                state.HudTimer?.Cancel();
                state.HudTimer = null;

                SendHUDAnnouncement("🔄 服务器重置", "正在重置服务器...");

                // 延迟 0.5 秒执行换图
                state.ExecuteTimer = Timer.Once(500.Milliseconds(), () =>
                {
                    Console.WriteLine("[Restart] 执行换图命令");
                    CCitadelPlayerController.PrintToConsoleAll("[Restart] 正在换图到 dl_midtown...");

                    try
                    {
                        Server.ExecuteCommand("changelevel dl_midtown");
                        Console.WriteLine("[Restart] 换图命令已发送");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Restart] 换图失败: {ex.Message}");
                        CCitadelPlayerController.PrintToConsoleAll($"[Restart] 换图失败: {ex.Message}");
                        SendHUDAnnouncement("❌ 重置失败", $"换图失败: {ex.Message}");
                    }

                    // 清理状态
                    _restartTimers.Remove(caller);
                });
            }
        });
    }

    // ========== 取消重启 ==========
    private void CancelRestart(CCitadelPlayerController? caller)
    {
        if (caller != null && _restartTimers.TryGetValue(caller, out var state))
        {
            state.HudTimer?.Cancel();
            state.ExecuteTimer?.Cancel();
            _restartTimers.Remove(caller);
            Console.WriteLine($"[Restart] 已取消 {caller.PlayerName} 的重启");
            SendHUDAnnouncement("⏹️ 已取消", "服务器重置已取消");
        }
    }

    // ========== 发送 HUD 公告给所有玩家 ==========
    private void SendHUDAnnouncement(string title, string description)
    {
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = title,
            DescriptionLocstring = description
        };
        NetMessages.Send(msg, RecipientFilter.All);
    }
}
