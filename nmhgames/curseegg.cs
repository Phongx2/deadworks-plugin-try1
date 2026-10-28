using DeadworksManaged.Api;
using System;

public class GooseEggWatcherPlugin : DeadworksPluginBase
{
    public override string Name => "Goose Egg Watcher";

    // 目标道具的内部名称
    private const string TargetItem = "upgrade_goose_egg";

    public override void OnLoad(bool isReload)
    {
        // 插件加载时在服务器控制台输出一条提示，方便确认插件已启动
        Console.WriteLine("[GooseEggWatcher] 插件已加载，正在监听 upgrade_goose_egg 的使用情况...");
    }

    // 监听玩家使用技能/道具的事件
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(PlayerUsedAbilityEvent args)
    {
        // 1. 判断使用的道具名是否匹配目标（忽略大小写，防止大小写差异导致匹配失败）
        if (!string.Equals(args.Abilityname, TargetItem, StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        // 2. 获取使用者的 Pawn 和 Controller，以拿到玩家名字
        var pawn = args.Player?.As<CCitadelPlayerPawn>();
        var controller = pawn?.Controller;

        // 如果获取不到玩家信息（极少数情况），保底输出
        string playerName = controller?.PlayerName ?? "未知玩家";
        int playerSlot = controller?.Slot ?? -1;

        // 3. 在服务器控制台输出
        Console.WriteLine($"[GooseEggWatcher] 检测到 {playerName} (Slot: {playerSlot}) 使用了 {TargetItem}！");

        // 4. 在所有玩家的控制台里输出
        // 使用 CCitadelPlayerController.PrintToConsoleAll 给所有人发送控制台消息
        CCitadelPlayerController.PrintToConsoleAll($"[GooseEggWatcher] {playerName} 使用了鹅蛋 (upgrade_goose_egg)！");

        return HookResult.Continue;
    }
}
