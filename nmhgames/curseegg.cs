using DeadworksManaged.Api;
using System;
using System.Collections.Generic;

public class GooseEggWatcherPlugin : DeadworksPluginBase
{
    public override string Name => "Goose Egg Watcher";

    private const string TargetItem = "upgrade_goose_egg";

    // 🌟 全局开关：true 表示允许使用，false 表示禁用
    public static bool IsEggEnabled = true;

    // 供外部调用的单例
    public static GooseEggWatcherPlugin? Instance { get; private set; }

    public override void OnLoad(bool isReload)
    {
        Instance = this;
        Console.WriteLine("[GooseEggWatcher] 插件已加载...");
    }

    // ==========================================
    // 🌟 开启/关闭禁用
    // ==========================================
    public void StartBanEgg()
    {
        IsEggEnabled = false;
        Console.WriteLine("[GooseEggWatcher] 鹅蛋已进入禁用状态。");
    }

    public void StopBanEgg()
    {
        IsEggEnabled = true;
        Console.WriteLine("[GooseEggWatcher] 鹅蛋已恢复使用。");
    }

    // ==========================================
    // 🌟 槽位映射工具
    // ==========================================
    private static InputButton? ButtonForSlot(EAbilitySlot slot) => slot switch
    {
        EAbilitySlot.Signature1  => InputButton.Ability1,
        EAbilitySlot.Signature2  => InputButton.Ability2,
        EAbilitySlot.Signature3  => InputButton.Ability3,
        EAbilitySlot.Signature4  => InputButton.Ability4,
        EAbilitySlot.ActiveItem1 => InputButton.Item1,
        EAbilitySlot.ActiveItem2 => InputButton.Item2,
        EAbilitySlot.ActiveItem3 => InputButton.Item3,
        EAbilitySlot.ActiveItem4 => InputButton.Item4,
        _ => null,
    };

    // ==========================================
    // 🌟 实时拦截按键
    // ==========================================
    public override void OnAbilityAttempt(AbilityAttemptEvent args)
    {
        // 如果没被禁用，直接放行
        if (IsEggEnabled) return;

        var pawn = args.Controller?.GetHeroPawn();
        if (pawn == null || !pawn.IsValid) return;

        // 遍历玩家的技能/道具，找鹅蛋
        foreach (var ability in pawn.AbilityComponent.Abilities)
        {
            if (ability.IsItem && ability.AbilityName == TargetItem)
            {
                // 算出它对应的按键并 Block
                if (ButtonForSlot(ability.AbilitySlot) is { } btn)
                {
                    args.Block(btn);
                }
                break;
            }
        }
    }





 









    // ==========================================
    // 兜底拦截与抽卡逻辑
    // ==========================================
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(PlayerUsedAbilityEvent args)
    {
        if (!string.Equals(args.Abilityname, TargetItem, StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        var pawn = args.Player?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        var playerName = pawn.Controller?.PlayerName ?? "未知玩家";

        // 如果开关被关闭，拦截并提示
        if (!IsEggEnabled)
        {
            Chat.PrintToChat(pawn.Controller, "⚠️ 当前处于特殊状态，无法使用鹅蛋！");
            return HookResult.Stop; 
        }

        // 强制消耗防取消
        Timer.NextTick(() =>
        {
            if (pawn.IsValid) pawn.RemoveItem(TargetItem);
        });

        Console.WriteLine($"[GooseEggWatcher] 检测到 {playerName} 按下了鹅蛋。");
        CCitadelPlayerController.PrintToConsoleAll($"[鹅蛋] {playerName} 使用了鹅蛋，开始抽卡！");

        // 呼叫抽卡逻辑
        EffectList.Chouka(pawn, this.Timer);

        return HookResult.Continue;
    }
}
