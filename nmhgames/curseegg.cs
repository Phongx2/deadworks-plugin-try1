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
    // 🌟 金鹅蛋购买冷却记录
    // ==========================================
    // 记录每个玩家的冷却结束时间（游戏时钟秒数）
    // 使用 SteamID64 而不是 Slot，防止玩家重连后绕过冷却
    private readonly Dictionary<ulong, float> _gooseEggCooldownEnd = new();

    // 监听玩家获得物品
    [GameEventHandler("ability_added")]
    public HookResult OnAbilityAdded(AbilityAddedEvent args)
    {
        var pawn = args.Userid?.As<CCitadelPlayerPawn>();
        var name = args.Ability?.SubclassVData?.Name;
        
        if (pawn == null || name != "upgrade_goose_egg")
            return HookResult.Continue;

        var controller = pawn.Controller;
        if (controller == null) return HookResult.Continue;

        ulong steamId = Permissions.GetSteamId64(controller.Slot);
        if (steamId == 0) return HookResult.Continue;

        // 记录冷却结束时间：当前时间 + 300秒
        _gooseEggCooldownEnd[steamId] = GlobalVars.CurTime + 300f;
        
        Console.WriteLine($"[GooseEggWatcher] 玩家 {controller.PlayerName} 购买了鹅蛋，进入 5 分钟冷却。");
        
        return HookResult.Continue;
    }

    // 购买前检查冷却
    public override HookResult OnModifyCurrency(ModifyCurrencyEvent args)
    {
        // 判断是否是购买金鹅蛋的扣钱行为
        if (args.Source != ECurrencySource.EItemGooseEgg)
            return HookResult.Continue;

        // 注意：如果这里的 args.Controller 报错，请尝试换成 args.Player
        var controller = args.Controller; 
        if (controller == null) return HookResult.Continue;

        ulong steamId = Permissions.GetSteamId64(controller.Slot);
        if (steamId == 0) return HookResult.Continue;

        // 检查是否在冷却中
        if (_gooseEggCooldownEnd.TryGetValue(steamId, out float endTime)
            && GlobalVars.CurTime < endTime)
        {
            float remaining = endTime - GlobalVars.CurTime;
            
            // 提示玩家并阻止购买（拦截扣钱逻辑）
            Chat.PrintToChat(controller, $"⚠️ 金鹅蛋冷却中，还需等待 {remaining:F0} 秒。");
            return HookResult.Stop; 
        }

        return HookResult.Continue;
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
