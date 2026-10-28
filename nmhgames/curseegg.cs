using DeadworksManaged.Api;
using System;

public class GooseEggWatcherPlugin : DeadworksPluginBase
{
    public override string Name => "Goose Egg Watcher";

    private const string TargetItem = "upgrade_goose_egg";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine("[GooseEggWatcher] 插件已加载，正在监听 upgrade_goose_egg 的使用情况...");
    }

    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(PlayerUsedAbilityEvent args)
    {
        if (!string.Equals(args.Abilityname, TargetItem, StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        // 获取使用者的 Pawn
        var pawn = args.Player?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        var controller = pawn.Controller;
        string playerName = controller?.PlayerName ?? "未知玩家";
        int playerSlot = controller?.Slot ?? -1;

        Console.WriteLine($"[GooseEggWatcher] 检测到 {playerName} (Slot: {playerSlot}) 使用了 {TargetItem}！");
        CCitadelPlayerController.PrintToConsoleAll($"[GooseEggWatcher] {playerName} 使用了鹅蛋 (upgrade_goose_egg)！");

        // 🌟 核心调用：把 pawn 传给抽卡方法
        GooseEggUtils.Chouka(pawn);

        return HookResult.Continue;
    }
}
