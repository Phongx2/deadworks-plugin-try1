using DeadworksManaged.Api;
using System.Numerics;

namespace JumpPlugin;

public class JumpPlugin : DeadworksPluginBase
{
    public override string Name => "Jump Test";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Jump] 热重载完成！" : "[Jump] 已加载！");
        Console.WriteLine("[Jump] 输入 /jump 给自己启用无限耐力，再次输入关闭");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Jump] 已卸载！");
    }

    // ========== /jump 命令 ==========
    [Command("jump", Description = "切换自己的无限耐力")]
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

        var mp = pawn.ModifierProp;
        if (mp == null)
        {
            caller.PrintToConsole("[Jump] 无法获取 ModifierProp");
            return;
        }

        // ========== 检查当前状态 ==========
        bool hasAirJumps = mp.HasModifierState(EModifierState.UnlimitedAirJumps);
        bool hasAirDashes = mp.HasModifierState(EModifierState.UnlimitedAirDashes);

        Console.WriteLine($"[Jump] {caller.PlayerName} 当前状态: UnlimitedAirJumps={hasAirJumps}, UnlimitedAirDashes={hasAirDashes}");

        if (hasAirJumps || hasAirDashes)
        {
            // 关闭
            mp.SetModifierState(EModifierState.UnlimitedAirJumps, false);
            mp.SetModifierState(EModifierState.UnlimitedAirDashes, false);
            caller.PrintToConsole("[Jump] 已关闭无限耐力");
            Console.WriteLine($"[Jump] {caller.PlayerName} 关闭了无限耐力");
        }
        else
        {
            // 开启
            mp.SetModifierState(EModifierState.UnlimitedAirJumps, true);
            mp.SetModifierState(EModifierState.UnlimitedAirDashes, true);
            caller.PrintToConsole("[Jump] 已开启无限耐力 (无限空中跳跃和冲刺)");
            Console.WriteLine($"[Jump] {caller.PlayerName} 开启了无限耐力");
        }
    }
}
