using DeadworksManaged.Api;
using System.Numerics;

namespace WaikaPlugin;

public class WaikaPlugin : DeadworksPluginBase
{
    public override string Name => "Waika";

    private bool _isLavaActive = false;
    private IHandle? _lavaTimer = null;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Waika] 热重载完成！" : "[Waika] 已加载！");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Waika] 已卸载！");
        StopLava();
    }

    private void StartLava()
    {
        if (_isLavaActive)
        {
            Console.WriteLine("[Waika] Lava 模式已在运行中");
            return;
        }

        Console.WriteLine("[Waika] 启动 Lava 模式");
        _isLavaActive = true;

        // ========== 发送启动 HUD ==========
        var startMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🌋 熔岩模式已启动",
            DescriptionLocstring = "站在地面上会受到灼烧伤害！"
        };
        NetMessages.Send(startMsg, RecipientFilter.All);

        _lavaTimer = Timer.Every(500.Milliseconds(), () =>
        {
            if (!_isLavaActive)
            {
                _lavaTimer?.Cancel();
                _lavaTimer = null;
                return;
            }

            foreach (var pawn in Players.GetAllPawns())
            {
                if (pawn == null || !pawn.IsValid) continue;
                if (!pawn.IsAlive) continue;

                if (pawn.IsOnGround)
                {
                    float maxHealth = pawn.GetMaxHealth();
                    float damage = Math.Max(1f, maxHealth * 0.01f);
                    pawn.Hurt(damage, attacker: null, inflictor: null, ability: null, damageType: 8);
                }
            }
        });
    }

    private void StopLava()
    {
        if (!_isLavaActive) return;

        Console.WriteLine("[Waika] 停止 Lava 模式");
        _isLavaActive = false;
        _lavaTimer?.Cancel();
        _lavaTimer = null;

        // ========== 发送停止 HUD ==========
        var stopMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "⏹️ 熔岩模式已停止",
            DescriptionLocstring = "地面灼烧效果已关闭"
        };
        NetMessages.Send(stopMsg, RecipientFilter.All);
    }

// ========== 新增：/m 命令 ==========
// ========== 新增：/m 命令（只对输入者自己生效） ==========
[Command("m", Description = "切换自己身上的 modifier: /m <modifier名称>")]
public void CmdToggleModifier(CCitadelPlayerController caller, string modifierName)
{
    if (caller == null) return;

    var pawn = caller.GetHeroPawn();
    if (pawn == null)
    {
        caller.PrintToConsole("[Waika] 无法获取英雄实体");
        return;
    }

    // 检查是否存在该 modifier
    bool hasModifier = pawn.ModifierProp?.HasModifier(modifierName) ?? false;

    if (hasModifier)
    {
        // 如果存在，则移除
        pawn.RemoveModifier(modifierName);
        caller.PrintToConsole($"[Waika] 已移除 modifier: {modifierName}");
        Console.WriteLine($"[Waika] {caller.PlayerName} 移除了 modifier: {modifierName}");
    }
    else
    {
        // 如果不存在，则添加（持续 5 秒）
        using var kv = new KeyValues3();
        kv.SetFloat("duration", 2.0f);
        pawn.AddModifier(modifierName, kv);
        caller.PrintToConsole($"[Waika] 已添加 modifier: {modifierName} (持续 5 秒)");
        Console.WriteLine($"[Waika] {caller.PlayerName} 添加了 modifier: {modifierName}");
    }
}

    [Command("t", Description = "功能开关: /t lava (启动/停止地面灼烧)")]
    public void CmdToggle(CCitadelPlayerController caller, string feature)
    {
        string playerName = caller?.PlayerName ?? "Server Console";
        Console.WriteLine($"[Waika] {playerName} 执行了命令: /t {feature}");

        if (feature?.ToLower() == "lava")
        {
            if (_isLavaActive)
            {
                StopLava();
                if (caller != null) caller.PrintToConsole("[Waika] Lava 模式已停止");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 地面灼烧效果已停止");
            }
            else
            {
                StartLava();
                if (caller != null) caller.PrintToConsole("[Waika] Lava 模式已启动");
                CCitadelPlayerController.PrintToConsoleAll("[Waika] 地面灼烧效果已启动！站在地面上会受到伤害");
            }
        }
        else
        {
            string msg = $"[Waika] 未知功能: {feature}。可用功能: lava";
            Console.WriteLine(msg);
            if (caller != null) caller.PrintToConsole(msg);
        }
    }
}
