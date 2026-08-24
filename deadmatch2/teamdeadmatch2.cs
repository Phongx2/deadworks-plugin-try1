using DeadworksManaged.Api;
using System.Numerics;

namespace RestartPlugin;

public class RestartPlugin : DeadworksPluginBase
{
    public override string Name => "Restart";

    private bool _isConsoleRestarting = false;
    private RestartState? _consoleRestartState = null;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Restart] 热重载完成！" : "[Restart] 已加载！");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Restart] 已卸载！");
        _restartTimers.Clear();
        _consoleRestartState = null;
        _isConsoleRestarting = false;
    }

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

    private readonly Dictionary<CCitadelPlayerController, RestartState> _restartTimers = new();

    [Command("r", Description = "3秒后重置服务器并换图到 dl_midtown", SuppressChat = true)]
    public void CmdRestart(CCitadelPlayerController? caller)
    {
        string playerName = caller?.PlayerName ?? "Server Console";
        Console.WriteLine($"[Restart] {playerName} 执行了重置命令");

        if (caller == null)
        {
            // ========== 控制台执行 ==========
            if (_isConsoleRestarting)
            {
                Console.WriteLine("[Restart] 控制台已有正在进行的重启，取消");
                CancelConsoleRestart();
                return;
            }

            CCitadelPlayerController.PrintToConsoleAll($"[Restart] {playerName} 发起了服务器重置，将在 3 秒后执行");

            var consoleState = new RestartState(3);
            _isConsoleRestarting = true;
            _consoleRestartState = consoleState;

            SendHUDAnnouncement("🔄 服务器重置", "3 秒后即将重置服务器...");
            StartCountdown(consoleState, null);
            return;
        }

        // ========== 玩家执行 ==========
        if (_restartTimers.ContainsKey(caller))
        {
            CancelRestart(caller);
            caller.PrintToConsole("[Restart] 已取消之前的重启");
            return;
        }

        CCitadelPlayerController.PrintToConsoleAll($"[Restart] {playerName} 发起了服务器重置，将在 3 秒后执行");

        var playerState = new RestartState(3);
        _restartTimers[caller] = playerState;

        SendHUDAnnouncement("🔄 服务器重置", "3 秒后即将重置服务器...");
        StartCountdown(playerState, caller);
    }

    private void StartCountdown(RestartState state, CCitadelPlayerController? caller)
    {
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
                state.HudTimer?.Cancel();
                state.HudTimer = null;

                SendHUDAnnouncement("🔄 服务器重置", "正在重置服务器...");

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

                    if (caller != null)
                        _restartTimers.Remove(caller);
                    else
                    {
                        _isConsoleRestarting = false;
                        _consoleRestartState = null;
                    }
                });
            }
        });
    }

    private void CancelRestart(CCitadelPlayerController caller)
    {
        if (_restartTimers.TryGetValue(caller, out var state))
        {
            state.HudTimer?.Cancel();
            state.ExecuteTimer?.Cancel();
            _restartTimers.Remove(caller);
            Console.WriteLine($"[Restart] 已取消 {caller.PlayerName} 的重启");
            SendHUDAnnouncement("⏹️ 已取消", "服务器重置已取消");
        }
    }

    private void CancelConsoleRestart()
    {
        if (_consoleRestartState != null)
        {
            _consoleRestartState.HudTimer?.Cancel();
            _consoleRestartState.ExecuteTimer?.Cancel();
            _consoleRestartState = null;
        }
        _isConsoleRestarting = false;
        Console.WriteLine("[Restart] 已取消控制台的重启");
        SendHUDAnnouncement("⏹️ 已取消", "服务器重置已取消");
    }

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
