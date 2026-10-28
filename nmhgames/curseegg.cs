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
        // 1. 判断是否是鹅蛋
        if (!string.Equals(args.Abilityname, TargetItem, StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        // 2. 获取玩家对象
        var pawn = args.Player?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        var playerName = pawn.Controller?.PlayerName ?? "未知玩家";

        // 3. 控制台播报（可选，方便你调试看日志）
        Console.WriteLine($"[GooseEggWatcher] 检测到 {playerName} 按下了鹅蛋。");
        CCitadelPlayerController.PrintToConsoleAll($"[鹅蛋] {playerName} 使用了鹅蛋，开始抽卡！");

        // 4. 呼叫抽卡逻辑，并将当前玩家对象作为参数传入
        EffectList.Chouka(pawn);

        return HookResult.Continue;
    }
}
