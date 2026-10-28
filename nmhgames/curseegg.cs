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

        // 🔑 核心修复：在下一帧强行移除玩家身上的蛋，防止取消读条后蛋被退回来
        Timer.NextTick(() =>
        {
            // 再次检查 pawn 是否有效（防止玩家在这 1 帧内断开连接或死亡）
            if (pawn.IsValid)
            {
                // RemoveItem 会直接销毁物品，不返还灵魂
                bool removed = pawn.RemoveItem(TargetItem);
                if (removed)
                {
                    Console.WriteLine($"[GooseEggWatcher] 已强制消耗 {playerName} 的鹅蛋。");
                }
                else
                {
                    Console.WriteLine($"[GooseEggWatcher] 尝试移除 {playerName} 的蛋失败，可能已经消耗或不存在。");
                }
            }
        });

        // 3. 控制台播报
        Console.WriteLine($"[GooseEggWatcher] 检测到 {playerName} 按下了鹅蛋。");
        CCitadelPlayerController.PrintToConsoleAll($"[鹅蛋] {playerName} 使用了鹅蛋，开始抽卡！");

        // 4. 呼叫抽卡逻辑
        EffectList.Chouka(pawn);

        return HookResult.Continue;
    }
}
