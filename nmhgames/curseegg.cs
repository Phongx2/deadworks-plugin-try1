using DeadworksManaged.Api;
using System;

public class GooseEggWatcherPlugin : DeadworksPluginBase
{
    public override string Name => "Goose Egg Watcher";

    private const string TargetItem = "upgrade_goose_egg";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine("[GooseEggWatcher] 已加载，按键瞬间强制消耗 upgrade_goose_egg。");
    }

    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(PlayerUsedAbilityEvent args)
    {
        // 精确匹配目标道具
        if (!string.Equals(args.Abilityname, TargetItem, StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        var pawn = args.Player?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        // 核心逻辑：检测到按键的瞬间，直接从背包移除该道具
        // 注意：RemoveItem 会把物品直接销毁，不返还灵魂
        pawn.RemoveItem(TargetItem);

        // 获取玩家信息并双端输出
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";
        int playerSlot = pawn.Controller?.Slot ?? -1;

        Console.WriteLine($"[GooseEggWatcher] 检测到 {playerName} (Slot: {playerSlot}) 按下了蛋，道具已被强制消耗。");
        CCitadelPlayerController.PrintToConsoleAll($"[GooseEggWatcher] {playerName} 的蛋被强制消耗了！");

        return HookResult.Continue;
    }
}
