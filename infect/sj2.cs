using DeadworksManaged.Api;
using System.Numerics;

namespace SkillShuffle2;

public class SkillShuffle2Plugin : DeadworksPluginBase
{
    public override string Name => "Skill Shuffle 2 (Debug)";

    private bool _isActive = false;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] ========== 插件加载 ==========");
        Console.WriteLine($"[{Name}] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        Console.WriteLine($"[{Name}] ===============================");
        _isActive = false;
        CCitadelPlayerController.PrintToConsoleAll("[技能调试] 插件已加载，使用 !sj2 启动");
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] 插件卸载");
        _isActive = false;
        CCitadelPlayerController.PrintToConsoleAll("[技能调试] 插件已卸载");
    }

    private CCitadelPlayerController? GetControllerFromPawn(CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return null;
        foreach (var controller in Players.GetAll())
        {
            if (controller.GetHeroPawn() == pawn)
                return controller;
        }
        return null;
    }

    // ========== 监听玩家使用技能 ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(GameEvent ev)
    {
        if (!_isActive) return HookResult.Continue;

        Console.WriteLine($"[{Name}] [DEBUG] player_used_ability 事件触发");

        // 获取施法者
        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null)
        {
            Console.WriteLine($"[{Name}] [DEBUG] 无法获取施法者 Pawn");
            return HookResult.Continue;
        }

        // 获取技能名称
        string abilityName = ev.GetString("abilityname", "");
        Console.WriteLine($"[{Name}] [DEBUG] 技能名称: {abilityName}");

        if (string.IsNullOrEmpty(abilityName))
        {
            Console.WriteLine($"[{Name}] [DEBUG] 技能名称为空，跳过");
            return HookResult.Continue;
        }

        // 获取技能槽位
        var abilities = pawn.AbilityComponent?.Abilities;
        if (abilities == null)
        {
            Console.WriteLine($"[{Name}] [DEBUG] 无法获取技能列表");
            return HookResult.Continue;
        }

        CCitadelBaseAbility? targetAbility = null;
        var slot = EAbilitySlot.Invalid;

        foreach (var ability in abilities)
        {
            if (ability == null) continue;
            if (ability.AbilityName == abilityName)
            {
                targetAbility = ability;
                slot = ability.AbilitySlot;
                Console.WriteLine($"[{Name}] [DEBUG] 找到目标技能，槽位: {slot}");
                break;
            }
        }

        if (slot < EAbilitySlot.Signature1 || slot > EAbilitySlot.Signature4)
        {
            Console.WriteLine($"[{Name}] [DEBUG] 技能槽位 {slot} 不在 1-4 范围内，跳过");
            return HookResult.Continue;
        }

        if (targetAbility == null)
        {
            Console.WriteLine($"[{Name}] [DEBUG] 目标技能为空，跳过");
            return HookResult.Continue;
        }

        var controller = GetControllerFromPawn(pawn);
        var playerName = controller?.PlayerName ?? "Unknown";

        // 获取冷却信息
        float cooldownStart = targetAbility.CooldownStart;
        float cooldownEnd = targetAbility.CooldownEnd;
        float remainingCooldown = cooldownEnd - cooldownStart;
        if (remainingCooldown < 0) remainingCooldown = 0;

        // ========== 延迟 10 tick 后输出信息 ==========
        var capturedPlayerName = playerName;
        var capturedAbilityName = abilityName;
        var capturedSlot = slot;
        var capturedRemaining = remainingCooldown;
        var capturedCooldownStart = cooldownStart;
        var capturedCooldownEnd = cooldownEnd;

        Timer.Once(10.Ticks(), () =>
        {
            string msg = $"[技能调试] 玩家 {capturedPlayerName} 使用了技能: {capturedAbilityName}";
            Console.WriteLine($"[{Name}] {msg}");
            controller?.PrintToConsole(msg);

            string slotName = GetSlotName(capturedSlot);
            string cdMsg = $"[技能调试] 槽位: {slotName}, 冷却开始: {capturedCooldownStart}, 冷却结束: {capturedCooldownEnd}, 剩余冷却: {capturedRemaining} 秒";
            Console.WriteLine($"[{Name}] {cdMsg}");
            controller?.PrintToConsole(cdMsg);

            // 检查是否在冷却中
            string status = capturedRemaining > 0 ? "冷却中" : "可用";
            string statusMsg = $"[技能调试] 技能状态: {status}";
            Console.WriteLine($"[{Name}] {statusMsg}");
            controller?.PrintToConsole(statusMsg);
        });

        return HookResult.Continue;
    }

    private string GetSlotName(EAbilitySlot slot)
    {
        switch (slot)
        {
            case EAbilitySlot.Signature1: return "1";
            case EAbilitySlot.Signature2: return "2";
            case EAbilitySlot.Signature3: return "3";
            case EAbilitySlot.Signature4: return "4";
            default: return slot.ToString();
        }
    }

    // ========== 启动/停止 ==========
    [Command("sj2", Description = "启动/停止技能调试模式")]
    public void CmdShuffle2(CCitadelPlayerController caller)
    {
        if (_isActive)
        {
            _isActive = false;
            Console.WriteLine($"[{Name}] 已停止");
            if (caller != null) caller.PrintToConsole("[技能调试] 已停止");
            CCitadelPlayerController.PrintToConsoleAll("[技能调试] 已停止");
            return;
        }

        _isActive = true;
        Console.WriteLine($"[{Name}] 已启动");
        if (caller != null) caller.PrintToConsole("[技能调试] 已启动，使用技能后将在 10 tick 后输出信息");
        CCitadelPlayerController.PrintToConsoleAll("[技能调试] 已启动");
    }
}
