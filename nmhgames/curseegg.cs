using DeadworksManaged.Api;
using System;
using System.Collections.Generic;

public class GooseEggWatcherPlugin : DeadworksPluginBase
{
    public override string Name => "Goose Egg Watcher";

    private const string TargetItem = "upgrade_goose_egg";

    // 记录玩家拿到蛋的起始时间 (SteamID -> 开始时间)
    private readonly Dictionary<ulong, float> _eggHoldStartTime = new();
    // 记录玩家的蛋等级提示定时器 (SteamID -> 定时器)
    private readonly Dictionary<ulong, IHandle> _eggLevelTimers = new();
    // 记录玩家当前的蛋等级 (SteamID -> 等级)
    public static readonly Dictionary<ulong, int> CurrentEggLevels = new();

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
    // 🌟 获得蛋：记录时间并启动等级提示
    // ==========================================
    [GameEventHandler("ability_added")]
    public HookResult OnAbilityAdded(AbilityAddedEvent args)
    {
        var pawn = args.Userid?.As<CCitadelPlayerPawn>();
        var name = args.Ability?.SubclassVData?.Name;
        if (pawn == null || name != TargetItem)
            return HookResult.Continue;

        var controller = pawn.Controller;
        if (controller == null) return HookResult.Continue;

        ulong steamId = Permissions.GetSteamId64(controller.Slot);
        if (steamId == 0) return HookResult.Continue;

        // 记录拿蛋时间
        _eggHoldStartTime[steamId] = GlobalVars.CurTime;
        CurrentEggLevels[steamId] = 1;
        Chat.PrintToChat(controller, "🥚 拿到金鹅蛋！拿着不动，1-4分钟对应1-4级。当前：1级");

        // 取消旧的提示定时器
        if (_eggLevelTimers.TryGetValue(steamId, out var oldTimer)) oldTimer.Cancel();

        // 每 10 秒检查一次等级
        _eggLevelTimers[steamId] = Timer.Every(10.Seconds(), () =>
        {
            if (!pawn.IsValid) return;
            if (!_eggHoldStartTime.TryGetValue(steamId, out float startTime)) return;

            float heldSeconds = GlobalVars.CurTime - startTime;
            int level = 1;
            if (heldSeconds >= 240f) level = 4;
            else if (heldSeconds >= 180f) level = 3;
            else if (heldSeconds >= 120f) level = 2;

            if (CurrentEggLevels[steamId] < level)
            {
                CurrentEggLevels[steamId] = level;
                // 用控制台输出，不刷屏聊天
                controller.PrintToConsole($"🥚 鹅蛋等级提升！当前为 {level} 级");
            }
        });

        return HookResult.Continue;
    }

    // ==========================================
    // 🌟 使用蛋：立刻判定并抽卡
    // ==========================================
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(PlayerUsedAbilityEvent args)
    {
        if (!string.Equals(args.Abilityname, TargetItem, StringComparison.OrdinalIgnoreCase))
            return HookResult.Continue;

        var pawn = args.Player?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        var controller = pawn.Controller;
        if (controller == null) return HookResult.Continue;

        // 如果开关被关闭，拦截并提示
        if (!IsEggEnabled)
        {
            Chat.PrintToChat(controller, "⚠️ 当前处于特殊状态，无法使用鹅蛋！");
            return HookResult.Stop;
        }

        ulong steamId = Permissions.GetSteamId64(controller.Slot);
        if (steamId == 0) return HookResult.Continue;

        // 强制消耗防取消
        Timer.NextTick(() =>
        {
            if (pawn.IsValid) pawn.RemoveItem(TargetItem);
        });

        // 计算持有时间
        float heldSeconds = 0;
        if (_eggHoldStartTime.TryGetValue(steamId, out float startTime))
            heldSeconds = GlobalVars.CurTime - startTime;

        // 清理计时器
        if (_eggLevelTimers.TryGetValue(steamId, out var timer)) timer.Cancel();
        _eggLevelTimers.Remove(steamId);
        _eggHoldStartTime.Remove(steamId);

        // 不满 60 秒：没孵出来
        if (heldSeconds < 60f)
        {
            Chat.PrintToChat(controller, "🥚 你的蛋啥都没有孵出来哦~");
            CurrentEggLevels.Remove(steamId);
            return HookResult.Stop;
        }

        // 锁定等级
        int finalLevel = CurrentEggLevels.TryGetValue(steamId, out int lvl) ? lvl : 1;
        CurrentEggLevels.Remove(steamId);

        Console.WriteLine($"[GooseEggWatcher] {controller.PlayerName} 按下了蛋，持有 {heldSeconds:F0} 秒，等级 {finalLevel}，开始抽卡！");

        // 立刻抽卡
        EffectList.ChoukaByLevel(pawn, finalLevel, this.Timer);

        return HookResult.Stop;
    }
}
