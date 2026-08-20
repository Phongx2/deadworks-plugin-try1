using DeadworksManaged.Api;
using System.Numerics;

namespace NeiGuiPlugin;

public class NeiGuiPlugin : DeadworksPluginBase
{
    public override string Name => "内鬼";

    private bool _isActive = false;
    private HashSet<CCitadelPlayerController> _activePlayers = new HashSet<CCitadelPlayerController>();  // 已激活内鬼的玩家
    private IHandle? _debugTimer = null;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[内鬼] 热重载完成！" : "[内鬼] 已加载！");
        Console.WriteLine("[内鬼] 输入 /ng 让自己成为内鬼");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[内鬼] 已卸载！");
        _isActive = false;
        _activePlayers.Clear();
        _debugTimer?.Cancel();
        _debugTimer = null;
    }

    // ========== 获取前方向量 ==========
    private Vector3 GetForwardVector(Vector3 angles)
    {
        float pitch = angles.X * MathF.PI / 180f;
        float yaw = angles.Y * MathF.PI / 180f;
        
        return new Vector3(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Cos(pitch) * MathF.Sin(yaw),
            -MathF.Sin(pitch)
        );
    }

    // ========== 辅助：从 Pawn 获取 Controller ==========
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

    // ========== /ng 命令 ==========
    [Command("ng", Description = "成为内鬼，近战攻击可以秒杀队友")]
    public void CmdBecomeNeiGui(CCitadelPlayerController caller)
    {
        if (caller == null)
        {
            Console.WriteLine("[内鬼] 错误: 执行者为空");
            return;
        }

        Console.WriteLine($"[内鬼] [DEBUG] 玩家 {caller.PlayerName} 输入了 /ng");

        var pawn = caller.GetHeroPawn();
        if (pawn == null || !pawn.IsValid)
        {
            Console.WriteLine($"[内鬼] [ERROR] 玩家 {caller.PlayerName} 无法获取英雄实体");
            caller.PrintToConsole("[内鬼] 无法获取英雄实体，请先选择英雄");
            return;
        }

        if (_activePlayers.Contains(caller))
        {
            Console.WriteLine($"[内鬼] [DEBUG] 玩家 {caller.PlayerName} 已经是内鬼，移除状态");
            _activePlayers.Remove(caller);
            caller.PrintToConsole("[内鬼] 你已不再是内鬼");
            
            if (_activePlayers.Count == 0)
            {
                _isActive = false;
                Console.WriteLine("[内鬼] [DEBUG] 没有活跃内鬼，停止监听");
            }
            return;
        }

        // 激活内鬼
        _activePlayers.Add(caller);
        _isActive = true;
        Console.WriteLine($"[内鬼] [重要] 玩家 {caller.PlayerName} 成为了内鬼！当前内鬼数: {_activePlayers.Count}");

        // 发送 HUD 公告给该玩家
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🔪 你是内鬼！",
            DescriptionLocstring = "你的近战攻击可以秒杀队友"
        };
        NetMessages.Send(msg, RecipientFilter.Single(caller.Slot));
        Console.WriteLine($"[内鬼] [DEBUG] 已向 {caller.PlayerName} 发送内鬼 HUD");

        caller.PrintToConsole("[内鬼] 你已成为了内鬼！近战攻击可以秒杀队友");
        Console.WriteLine($"[内鬼] [DEBUG] /ng 命令执行完成");
    }

    // ========== 监听近战攻击 ==========
[GameEventHandler("player_used_ability")]
public HookResult OnPlayerUsedAbility(GameEvent ev)
{
    if (!_isActive)
    {
        return HookResult.Continue;
    }

    Console.WriteLine("[内鬼] [DEBUG] player_used_ability 事件触发");

    var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
    if (pawn == null)
    {
        Console.WriteLine("[内鬼] [DEBUG] 无法获取施法者 Pawn");
        return HookResult.Continue;
    }

    var controller = GetControllerFromPawn(pawn);
    if (controller == null)
    {
        Console.WriteLine("[内鬼] [DEBUG] 无法获取施法者 Controller");
        return HookResult.Continue;
    }

    if (!_activePlayers.Contains(controller))
    {
        Console.WriteLine($"[内鬼] [DEBUG] 玩家 {controller.PlayerName} 不是内鬼，跳过");
        return HookResult.Continue;
    }

    string abilityName = ev.GetString("abilityname", "");
    Console.WriteLine($"[内鬼] [DEBUG] 玩家 {controller.PlayerName} 使用了技能: {abilityName}");

    if (!abilityName.StartsWith("ability_melee"))
    {
        Console.WriteLine($"[内鬼] [DEBUG] 不是近战攻击，跳过");
        return HookResult.Continue;
    }

    Console.WriteLine($"[内鬼] [重要] 内鬼 {controller.PlayerName} 使用了近战攻击！");

    var attacker = pawn;
    var attackerController = controller;
    
    // 获取当前使用的近战技能实体
    var meleeAbility = pawn.AbilityComponent?.Abilities
        .FirstOrDefault(a => a?.AbilityName == abilityName);

    Timer.Once(100.Milliseconds(), () =>
    {
        Console.WriteLine($"[内鬼] [DEBUG] 开始检测近战命中 (100ms 延迟)");
        
        if (attacker == null || !attacker.IsValid)
        {
            Console.WriteLine("[内鬼] [DEBUG] 攻击者已失效");
            return;
        }
        if (attackerController == null)
        {
            Console.WriteLine("[内鬼] [DEBUG] 攻击者 Controller 已失效");
            return;
        }
        if (!_activePlayers.Contains(attackerController))
        {
            Console.WriteLine($"[内鬼] [DEBUG] 玩家 {attackerController.PlayerName} 已不再是内鬼");
            return;
        }

        var teammates = Players.GetAllPawns()
            .Where(p => p != null && p.IsValid && p.TeamNum == attacker.TeamNum && p != attacker)
            .ToList();
        Console.WriteLine($"[内鬼] [DEBUG] 找到 {teammates.Count} 名队友");

        Vector3 attackerPos = attacker.Position;
        Vector3 forward = GetForwardVector(attacker.EyeAngles);
 float meleeRange = 180f;        
float angleThreshold = 0.6428f

        bool hitAny = false;

        foreach (var victim in teammates)
        {
            if (victim == null || !victim.IsValid) continue;

            float distance = Vector3.Distance(attackerPos, victim.Position);
            Console.WriteLine($"[内鬼] [DEBUG] 队友距离: {distance:F2} (范围阈值: {meleeRange})");

            if (distance > meleeRange)
            {
                Console.WriteLine($"[内鬼] [DEBUG] 队友距离 {distance:F2} 超出范围");
                continue;
            }

            Vector3 toTarget = victim.Position - attackerPos;
            Vector3 normalizedToTarget = Vector3.Normalize(toTarget);
            float dotProduct = Vector3.Dot(forward, normalizedToTarget);
            Console.WriteLine($"[内鬼] [DEBUG] 夹角值: {dotProduct:F4} (阈值: {angleThreshold})");

            if (dotProduct < angleThreshold)
            {
                Console.WriteLine($"[内鬼] [DEBUG] 夹角值 {dotProduct:F4} 小于阈值，不在攻击扇形内");
                continue;
            }

            var victimController = GetControllerFromPawn(victim);
            if (victimController == null) continue;

            Console.WriteLine($"[内鬼] [重要] 内鬼 {attackerController.PlayerName} 近战命中了队友 {victimController.PlayerName}！");
            hitAny = true;

            Console.WriteLine($"[内鬼] [重要] 正在秒杀队友 {victimController.PlayerName}");

            // ========== 使用修改后的伤害参数 ==========
            victim.Hurt(
                damage: 999999f,
                attacker: attacker,          // 攻击者设为内鬼
                inflictor: null,
                ability: meleeAbility,       // 使用当前近战技能实体
                damageType: 4                // 4 = slash/melee 伤害类型
            );
            Console.WriteLine($"[内鬼] [DEBUG] 队友 {victimController.PlayerName} 已被 {attackerController.PlayerName} 秒杀 (伤害类型: 4, 技能: {abilityName})");

            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "🔪 内鬼出手！",
                DescriptionLocstring = $"{attackerController.PlayerName} 击杀了队友 {victimController.PlayerName}！"
            };
            NetMessages.Send(msg, RecipientFilter.All);
            Console.WriteLine($"[内鬼] [DEBUG] 已广播击杀消息");

            break;
        }

        if (!hitAny)
        {
            Console.WriteLine($"[内鬼] [DEBUG] 近战未命中任何队友");
        }
    });

    return HookResult.Continue;
}
}
