using DeadworksManaged.Api;
using System.Numerics;

namespace JumpPlugin;

public class JumpPlugin : DeadworksPluginBase
{
    public override string Name => "Jump Test";

    private bool _isActive = false;
    private IHandle? _resetTimer = null;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Jump] 热重载完成！" : "[Jump] 已加载！");
        Console.WriteLine("[Jump] 输入 /jump 启用/关闭无限跳跃");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Jump] 已卸载！");
        _resetTimer?.Cancel();
        _resetTimer = null;
        _isActive = false;
    }

    // ========== /jump 命令 ==========
    [Command("jump", Description = "切换自己的无限跳跃/冲刺")]
    public void CmdJump(CCitadelPlayerController caller)
    {
        if (caller == null)
        {
            Console.WriteLine("[Jump] 错误: 执行者为空");
            return;
        }

        var pawn = caller.GetHeroPawn();
        if (pawn == null || !pawn.IsValid)
        {
            caller.PrintToConsole("[Jump] 无法获取英雄实体，请先选择英雄");
            Console.WriteLine($"[Jump] {caller.PlayerName} 无法获取英雄实体");
            return;
        }

        if (_isActive)
        {
            // 关闭
            _isActive = false;
            _resetTimer?.Cancel();
            _resetTimer = null;
            caller.PrintToConsole("[Jump] 已关闭无限跳跃/冲刺");
            Console.WriteLine($"[Jump] {caller.PlayerName} 关闭了无限跳跃/冲刺");
            return;
        }

        // 开启
        _isActive = true;
        caller.PrintToConsole("[Jump] 已开启无限跳跃/冲刺 (每次跳跃后重置空中次数)");
        Console.WriteLine($"[Jump] {caller.PlayerName} 开启了无限跳跃/冲刺");

        // 启动监听（每帧检查）
        // 启动监听（每 100ms 检查一次）
_resetTimer = Timer.Every(100.Milliseconds(), () =>
{
    if (!_isActive)
    {
        _resetTimer?.Cancel();
        _resetTimer = null;
        return;
    }

    if (pawn == null || !pawn.IsValid) return;

    var abilities = pawn.AbilityComponent?.Abilities;
    if (abilities == null) return;

    foreach (var ability in abilities)
    {
        if (ability == null) continue;

        var jump = ability.As<CCitadel_Ability_Jump>();
        if (jump != null && jump.ConsecutiveAirJumps > 0)
        {
            jump.ConsecutiveAirJumps = 0;
        }

        var dash = ability.As<CCitadel_Ability_Dash>();
        if (dash != null && dash.ConsecutiveAirDashes > 0)
        {
            dash.ConsecutiveAirDashes = 0;
        }
    }
});
       
    }
}
