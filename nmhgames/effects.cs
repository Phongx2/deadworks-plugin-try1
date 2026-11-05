using DeadworksManaged.Api;
using DeadworksManaged.Api.Sounds;
using System;
using System.Collections.Generic;
using System.Linq;
using ITimer = DeadworksManaged.Api.ITimer;
using System.Numerics;
using DeadworksManaged.Api.Utils;

// ==========================================
// 1. 坐标工具与点位配置
// ==========================================
public static class MapPoints
{
    public static Vector3 ParsePos(string posStr)
    {
        var parts = posStr.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 3 && float.TryParse(parts[0], out var x) && float.TryParse(parts[1], out var y) && float.TryParse(parts[2], out var z))
            return new Vector3(x, y, z);
        return new Vector3(0, 0, 0);
    }

    public static readonly string[] TeamAPoints = {
        "-591.781250 -697.187500 11.843750",
        "-637.343750 -316.406250 -2.250000",
        "-646.750000 -61.281250 40.062500",
        "-617.093750 336.000000 -2.406250",
        "-621.312500 699.218750 4.343750"
    };
    public static readonly string[] TeamBPoints = {
        "611.562500 656.562500 7.437500",
        "607.687500 288.625000 0.718750",
        "656.031250 44.125000 9.500000",
        "632.281250 -318.250000 11.531250",
        "603.906250 -717.437500 -1.250000"
    };
}

// ==========================================
// 2. 运行时状态与事件监听插件
// ==========================================
public class EffectRuntimePlugin : DeadworksPluginBase
{
    public override string Name => "Effect Runtime Plugin";
    
    public static EffectRuntimePlugin Instance { get; private set; }

    public static CCitadelPlayerPawn ActivePlayerA { get; private set; }
    public static CCitadelPlayerPawn ActivePlayerB { get; private set; }

    public static Vector3 PlayerAOriginalPos { get; private set; }
    public static Vector3 PlayerBOriginalPos { get; private set; }

    public static readonly Dictionary<CCitadelPlayerPawn, Vector3> OriginalPositions = new();
    public static readonly HashSet<CCitadelPlayerPawn> WatchingDeadPlayers = new();

    private IHandle _duelMonitorTimer;

	    // 🌟 这些是你清理代码里用到的变量，必须先声明
    public static Zone? VisualZone;
    public static Zone? DamageZone;
    public static List<CBeam>? VisualBeams;
    public static readonly Dictionary<CCitadelPlayerPawn, IHandle?> BleedingPawns = new();

	    // ===== 1v1 区域与流血 =====
   

// ===== 预加载列表 =====
	public override void OnPrecacheResources()
{
    Precache.AddResource(Effects.WerewolfModel);
	Precache.AddResource("particles/upgrades/mystical_piano_hit.vpcf");
	Precache.AddResource("particles/upgrades/shrink_ray_cast_smoke.vpcf");




	
}

public override HookResult OnChatMessage(ChatMessage message)
{
    // 没有激活的死亡笔记，或者不是触发者在说话，不处理
    if (Effects.DeadNoteCasterSlot < 0) return HookResult.Continue;
    if (message.Controller == null) return HookResult.Continue;
    if (message.Controller.Slot != Effects.DeadNoteCasterSlot) return HookResult.Continue;

    string input = message.ChatText.Trim();
    if (string.IsNullOrWhiteSpace(input)) return HookResult.Continue;

    var targetController = Players.GetAll()
        .FirstOrDefault(c => c != null && c.IsValid
            && c.Slot != message.Controller.Slot
            && c.PlayerName != null
            && c.PlayerName.Contains(input, StringComparison.OrdinalIgnoreCase));

    // 匹配失败：提示，但不解除监听，允许重新输入
    if (targetController == null)
    {
        Chat.PrintToChat(message.Controller, $"📓 找不到名为「{input}」的玩家，请重新输入。");
        return HookResult.Stop;
    }

    // 匹配成功：解除监听，再击杀
    Effects.DeadNoteCasterSlot = -1;

    var targetPawn = targetController.GetHeroPawn();
    var attackerPawn = message.Controller.GetHeroPawn();
    if (targetPawn != null && targetPawn.IsValid)
    {
        using var info = new CTakeDamageInfo(99999f, attackerPawn, null, null, 0);
        info.DamageFlags = TakeDamageFlags.ForceDeath | TakeDamageFlags.AllowSuicide;
        targetPawn.TakeDamage(info);
    }

   // Chat.PrintToChat(message.Controller, $"📓 死亡笔记生效，{targetController.PlayerName} 已死亡。");
   // Chat.PrintToChatAll($"📓 {message.Controller.PlayerName} 使用了死亡笔记，{targetController.PlayerName} 被写入了死亡名单。");

    return HookResult.Stop;
}















	

    public override void OnLoad(bool isReload)
    {
        Instance = this;
        Console.WriteLine("[EffectRuntimePlugin] 已加载 1v1 运行时逻辑");
    }

    public void Prepare1v1(CCitadelPlayerPawn playerA, CCitadelPlayerPawn playerB)
    {
        ActivePlayerA = playerA;
        ActivePlayerB = playerB;
        
        PlayerAOriginalPos = playerA?.Position ?? new Vector3(0, 0, 0);
        PlayerBOriginalPos = playerB?.Position ?? new Vector3(0, 0, 0);

        OriginalPositions.Clear();
        WatchingDeadPlayers.Clear();

        int teamAIndex = 0;
        int teamBIndex = 0;

        foreach (var c in Players.GetAll())
        {
            var p = c.GetHeroPawn();
            if (p == null || !p.IsValid) continue;
            if (p == playerA || p == playerB) continue;

            var originalPos = p.Position;
            OriginalPositions[p] = originalPos;

            if (p.LifeState != LifeState.Alive)
            {
                WatchingDeadPlayers.Add(p);
                continue;
            }

            if (p.TeamNum == playerA.TeamNum)
            {
                var pointStr = MapPoints.TeamAPoints[teamAIndex % MapPoints.TeamAPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamAIndex++;
            }
            else
            {
                var pointStr = MapPoints.TeamBPoints[teamBIndex % MapPoints.TeamBPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamBIndex++;
            }

            // 🌟 修改为你要的 Modifier
            p.AddModifier("ability_familiar_ability01/effect");
            p.AddModifier("modifier_citadel_in_hideout_zone");
        }

        _duelMonitorTimer?.Cancel();
        _duelMonitorTimer = Timer.Every(1.Seconds(), () =>
        {
            CheckDuelStatus();
        });
    }

    private void CheckDuelStatus()
    {
        if (ActivePlayerA == null || ActivePlayerB == null) return;
        if (!ActivePlayerA.IsValid || !ActivePlayerB.IsValid) return;

        bool isADead = ActivePlayerA.LifeState != LifeState.Alive;
        bool isBDead = ActivePlayerB.LifeState != LifeState.Alive;

        if (isADead == isBDead) return;

        _duelMonitorTimer?.Cancel();
        _duelMonitorTimer = null;

        if (isADead)
        {
            HandleDuelEnd(ActivePlayerB, ActivePlayerA);
        }
        else
        {
            HandleDuelEnd(ActivePlayerA, ActivePlayerB);
        }
    }

    private void HandleDuelEnd(CCitadelPlayerPawn winner, CCitadelPlayerPawn loser)
    {

		 // 🌟 直接在这里清理决斗区域和流血
        if (VisualZone != null) { VisualZone.Dispose(); VisualZone = null; }
        if (DamageZone != null) { DamageZone.Dispose(); DamageZone = null; }
        if (VisualBeams != null) { CBeam.RemoveAll(VisualBeams); VisualBeams = null; }
        
        foreach (var h in BleedingPawns.Values) h?.Cancel();
        BleedingPawns.Clear();


		Vector3 winnerPos = winner == ActivePlayerA ? PlayerAOriginalPos : PlayerBOriginalPos;
        ActivePlayerA = null;
        ActivePlayerB = null;

        var winController = winner.Controller;
        var loseController = loser.Controller;
        if (winController == null || loseController == null) return;

        string winnerName = winController.PlayerName ?? "未知玩家";
        string loserName = loseController.PlayerName ?? "未知玩家";

        GooseEggWatcherPlugin.Instance?.StopBanEgg();

        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🏆 决斗结束",
            DescriptionLocstring = $"{winnerName} 战胜了 {loserName}！"
        };
        NetMessages.Send(msg, RecipientFilter.All);

        string[] taunts = {
            $"跟 {loserName} 打真是简简又单单啊！！",
            $"{loserName} 不行啊",
            "拿下"
        };
        





                string tauntText = taunts[Random.Shared.Next(taunts.Length)];
        var chatMsg = new CCitadelUserMsg_ChatMsg
        {
            PlayerSlot = winController.Slot,
            Text = tauntText,
            AllChat = true
        };
        NetMessages.Send(chatMsg, RecipientFilter.All);













        

        var souls = winner.GetCurrency(ECurrencyType.EGold);
        winner.SetCurrency(ECurrencyType.EGold, souls + 3200);

        if (winner.IsValid)
        {
            // 🌟 直接用上面存好的坐标
            winner.Teleport(position: winnerPos, angles: null, velocity: null);
        }

        // 恢复观战者
        foreach (var kvp in OriginalPositions)
        {
            var p = kvp.Key;
            if (p != null && p.IsValid)
            {
                // 🌟 修改为你要的 Modifier 移除
                p.RemoveModifier("ability_familiar_ability01/effect");
                p.RemoveModifier("modifier_citadel_in_hideout_zone");
                p.Teleport(position: kvp.Value, angles: null, velocity: null);
            }
        }
        OriginalPositions.Clear();
        WatchingDeadPlayers.Clear();
    }

    [GameEventHandler("player_respawned")]
    public HookResult OnPlayerRespawned(PlayerRespawnedEvent args)
    {
        var pawn = args.Userid?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        if (ActivePlayerA != null && ActivePlayerB != null && pawn != ActivePlayerA && pawn != ActivePlayerB)
        {
            Timer.Once(5.Ticks(), () =>
            {
                if (pawn.IsValid && ActivePlayerA != null && ActivePlayerB != null)
                {
                    OriginalPositions[pawn] = pawn.Position;

                    if (pawn.TeamNum == ActivePlayerA.TeamNum)
                    {
                        var pointStr = MapPoints.TeamAPoints[Random.Shared.Next(MapPoints.TeamAPoints.Length)];
                        pawn.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                    }
                    else
                    {
                        var pointStr = MapPoints.TeamBPoints[Random.Shared.Next(MapPoints.TeamBPoints.Length)];
                        pawn.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                    }

                    // 🌟 修改为你要的 Modifier
                    pawn.AddModifier("ability_familiar_ability01/effect");
                    pawn.AddModifier("modifier_citadel_in_hideout_zone");
                }
            });
        }

        return HookResult.Continue;
    }
}

// ==========================================
// 3. 核心效果执行类
// ==========================================
public static class Effects
{
    // ===== wolfdance 用到的字段和常量 =====
    public const string WerewolfModel = "models/heroes_wip/werewolf/transform/werewolf_transform.vmdl";
    private const string WerewolfWeapon = "citadel_weapon_werewolf_claws";
    private const ushort WeaponSlot = 21; // ESlot_Weapon_Primary
    private static readonly Dictionary<CCitadelPlayerPawn, string> _savedModels = new();
    private static readonly Dictionary<CCitadelPlayerPawn, string> _savedWeapons = new();
    private static bool _wolfdanceSaved = false;
	private static readonly Dictionary<CCitadelPlayerPawn, CParticleSystem?> _wolfParticles = new();
	// deadnote 状态：记录哪个玩家正在等待输入 ID
public static int DeadNoteCasterSlot = -1;

    // 换图/重载时清空缓存，让下一次 wolfdance 重新采集
    public static void ResetWolfdanceCache()
    {
        _savedModels.Clear();
        _savedWeapons.Clear();
        _wolfdanceSaved = false;
		_wolfdanceSaved = false;
    }

    public static void Execute(EffectInfo effect, CCitadelPlayerPawn pawn, ITimer timer)
    {
        if (pawn == null) return;
        var controller = pawn.Controller;
        if (controller == null) return;

        if (effect.Id == "speed_boost")
        {
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration);

            pawn.AddModifier("upgrade_fleetfoot_boots/modifier_intrinsic_base", kv: kv,
            abilityValues: new() {
                ["MoveWhileShootingSpeedPenaltyReductionPercent"] = 200f,
                ["MoveWhileZoomedSpeedPenaltyReductionPercent"] = 200f,
                ["SlideScale"] = 299f,
                ["BulletResist"] = 15f,
                ["BaseAttackDamagePercent"] = 15f,
            });

            pawn.AddModifier("upgrade_fleetfoot_boots/modifier_fleetfoot_boots", kv: kv,
            abilityValues: new() {
                ["ActiveBonusMoveSpeed"] = 10f,
                ["SlowResistancePercent"] = 200f,
            });

            pawn.AddModifier("modifier_hero_testing_fast_movespeed", kv);
            EffectList.SendDrawNotification(controller, effect);
        }












		
        else if (effect.Id == "1v1")
        {

    GooseEggWatcherPlugin.Instance?.StartBanEgg();
            
            var enemies = Players.GetAll()
                .Where(c => c != null && c.IsValid && c.Slot != controller.Slot)
                .Select(c => c.GetHeroPawn())
                .Where(p => p != null && p.IsValid && p.TeamNum != pawn.TeamNum)
                .ToList();

            var playerB = enemies.Count > 0 ? enemies[Random.Shared.Next(enemies.Count)] : null;
            var controllerB = playerB?.Controller;
            string nameB = controllerB?.PlayerName ?? "未知敌人";

            Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);
            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "⚠️ 做好准备",
                DescriptionLocstring = $"{controller.PlayerName} 与 {nameB} 将要发起决斗！"
            };
            NetMessages.Send(msg, RecipientFilter.All);


            
            var chatMsg = new CCitadelUserMsg_ChatMsg
            {
                PlayerSlot = controller.Slot,
                Text = $"⚠️ 做好准备，我将对 {nameB} 发起决斗！",
                AllChat = true
            };
            NetMessages.Send(chatMsg, RecipientFilter.All);


             // ===== 启动 1v1 区域、边框、流血逻辑 =====

			            // ===== 启动 1v1 区域、边框、流血逻辑 =====
            if (playerB != null)
            {
                // 清理上一局可能遗留的状态
                if (EffectRuntimePlugin.VisualZone != null) { EffectRuntimePlugin.VisualZone.Dispose(); EffectRuntimePlugin.VisualZone = null; }
                if (EffectRuntimePlugin.DamageZone != null) { EffectRuntimePlugin.DamageZone.Dispose(); EffectRuntimePlugin.DamageZone = null; }
                if (EffectRuntimePlugin.VisualBeams != null) { CBeam.RemoveAll(EffectRuntimePlugin.VisualBeams); EffectRuntimePlugin.VisualBeams = null; }
                foreach (var h in EffectRuntimePlugin.BleedingPawns.Values) h?.Cancel();
                EffectRuntimePlugin.BleedingPawns.Clear();

                // 1. 创建视觉框
                EffectRuntimePlugin.VisualZone = new Zone(
                    new Vector3(-600.75f, -680.44f, -90f),
                    new Vector3(600.03f, 680.22f, 200f)
                );
                EffectRuntimePlugin.VisualBeams = CBeam.CreateBox(EffectRuntimePlugin.VisualZone.Mins, EffectRuntimePlugin.VisualZone.Maxs, width: 4f, color: System.Drawing.Color.Red);

                // 2. 创建伤害判定框
                EffectRuntimePlugin.DamageZone = new Zone(
                    new Vector3(-646.75f, -717.44f, -100f),
                    new Vector3(656.03f, 699.22f, 300f)
                );

                // 3. 局部扣血函数
                void StartBleed(CCitadelPlayerPawn p)
                {
                    if (p == null || !p.IsValid) return;
                    if (EffectRuntimePlugin.BleedingPawns.TryGetValue(p, out var existing) && existing != null) return;

                    void Tick()
                    {
                        if (p == null || !p.IsValid || p.LifeState != LifeState.Alive) return;
                        p.Hurt(Math.Max(1f, p.GetMaxHealth() * 0.10f), attacker: null, inflictor: null, ability: null, damageType: 8);
                    }
                    Tick();
                    EffectRuntimePlugin.BleedingPawns[p] = timer.Every(1.Seconds(), Tick);
                }

                void StopBleed(CCitadelPlayerPawn p)
                {
                    if (p == null) return;
                    if (EffectRuntimePlugin.BleedingPawns.TryGetValue(p, out var h) && h != null)
                    {
                        h.Cancel();
                        EffectRuntimePlugin.BleedingPawns[p] = null;
                    }
                }

                // 4. 绑定事件（只对 A 和 B 生效）
                var a = pawn;
                var b = playerB;
                EffectRuntimePlugin.DamageZone.Left += (z, player, p) =>
                {
                    if (p == null || !p.IsValid) return;
                    if (p != a && p != b) return;
                    Chat.PrintToChat(player, "⚠️ 你离开了决斗区域，开始流血！");
                    StartBleed(p);
                };

                EffectRuntimePlugin.DamageZone.Entered += (z, player, p) =>
                {
                    if (p == null || !p.IsValid) return;
                    if (p != a && p != b) return;
                    Chat.PrintToChat(player, "✅ 你进入了安全区域，停止流血。");
                    StopBleed(p);
                };

                EffectRuntimePlugin.DamageZone.ResetOccupants();
            }



			

            EffectRuntimePlugin.Instance?.Prepare1v1(pawn, playerB!);

            timer.Once(3.Seconds(), () =>
            {
                Console.WriteLine("[1v1] 决斗开始，施加时空扭曲");
                using var kvA = new KeyValues3();
                kvA.SetFloat("duration", 1.0f);
                if (pawn.IsValid) pawn.AddModifier("modifier_chrono_swap_bubble_move", kvA);

                using var kvB = new KeyValues3();
                kvB.SetFloat("duration", 1.0f);
                if (playerB != null && playerB.IsValid) playerB.AddModifier("modifier_chrono_swap_bubble_move", kvB);

                timer.Once(1.Seconds(), () =>
                {
                    Console.WriteLine("[1v1] 移除时空扭曲");
                    if (pawn.IsValid) pawn.RemoveModifier("modifier_chrono_swap_bubble_move");
                    if (playerB != null && playerB.IsValid) playerB.RemoveModifier("modifier_chrono_swap_bubble_move");
                });
            });  
        }


 else if (effect.Id == "undie")
        {
            // 🌟 把写死的 60.0f 改成读取你配置的时长
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration);

            // 给玩家挂上殊死一搏的 Modifier
            pawn.AddModifier("upgrade_cheat_death/modifier_cheat_death_immunity", kv: kv,
            abilityValues: new() {
                ["DeathImmunityDamageReduction"] = -1f,
                ["HealAmpReceivePenaltyPercent"] = -1f,
                ["HealAmpRegenPenaltyPercent"] = -1f,
                ["BonusMoveSpeed"] = 5f,
            });
            // 发送提示
            EffectList.SendDrawNotification(controller, effect);
        }

			
		else if (effect.Id == "UAV")
{
    // 变量名 UAV：抽到的人对敌方所有人施加 30s VisibleToEnemy
	EffectList.SendDrawNotification(controller, effect);

NetMessages.Send(new CCitadelUserMsg_ChatMsg
{
    PlayerSlot = controller.Slot,
    Text = "我方无人侦察机已升空",
    AllChat = true
}, RecipientFilter.All);
		
   int casterTeam = pawn.TeamNum;
            var enemies = Players.GetAll()
                .Where(c => c != null && c.IsValid && c.Slot != controller.Slot)
                .Select(c => c.GetHeroPawn())
                .Where(p => p != null && p.IsValid && p.TeamNum != casterTeam)
                .ToList();

            foreach (var enemy in enemies)
                enemy.ModifierProp.SetModifierState(EModifierState.VisibleToEnemy, true);

            // 🌟 把 30.Seconds() 改成读取 effect.Duration
            timer.Once(((double)effect.Duration).Seconds(), () =>
            {
                foreach (var enemy in enemies)
                {
                    if (enemy == null || !enemy.IsValid) continue;
                    enemy.ModifierProp.SetModifierState(EModifierState.VisibleToEnemy, false);
                }
            });
        }


	        // ================= 效果：无限子弹 =================
         else if (effect.Id == "infbullet")
        {
            // 施加无限弹匣 Modifier，duration 到期后自动结束
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration);
            pawn.AddModifier("upgrade_infinitemagazine/modifier_infinite_magazine_active", kv: kv);

            // 发送抽卡提示
            EffectList.SendDrawNotification(controller, effect);
        }







                // ================= 效果：霸体（免控） =================
        else if (effect.Id == "unstop")
        {
            // 使用 effect.Duration 作为持续时间
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration);

            // 挂载霸体 Modifier
            pawn.AddModifier("upgrade_unstoppable/modifier_unstoppable", kv);

            // 发送抽卡提示
            EffectList.SendDrawNotification(controller, effect);
        }



		        // ================= 效果：yoshiban (大手的力量) =================
        else if (effect.Id == "yoshiban")
        {
            // 1. 找对面队伍的人
            var enemies = Players.GetAll()
                .Where(c => c != null && c.IsValid && c.Slot != controller.Slot)
                .Select(c => c.GetHeroPawn())
                .Where(p => p != null && p.IsValid && p.TeamNum != pawn.TeamNum)
                .ToList();

            if (enemies.Count == 0) return;

            // 2. 随机挑一个倒霉蛋
            var target = enemies[Random.Shared.Next(enemies.Count)];
            string targetName = target.Controller?.PlayerName ?? "未知敌人";
            string casterName = controller.PlayerName ?? "未知玩家";

            // 3. 给倒霉蛋挂上作弊者诅咒
            using var kv = new KeyValues3();
            kv.SetFloat("duration", effect.Duration);
            target.AddModifier("modifier_citadel_cheater_curse", kv);

            // 4. 播放音效
            Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

            // 5. 第一次 HUD 播报
            var msg1 = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "⚠️ 检测到有人作弊",
                DescriptionLocstring = $"{targetName} 变成了青蛙！"
            };
            NetMessages.Send(msg1, RecipientFilter.All);

            // 6. 计时结束后恢复并播报
            timer.Once(((double)effect.Duration).Seconds(), () =>
            {
                if (target != null && target.IsValid)
                {
                    // 虽然 Modifier 理论上到时间会自动消失，但为了保险，我们手动再移除一下
                    target.RemoveModifier("modifier_citadel_cheater_curse");
                }

                // 再次播放音效
                Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

                // 第二次 HUD 播报
                var msg2 = new CCitadelUserMsg_HudGameAnnouncement
                {
                    TitleLocstring = "✅ 原来是误封啊",
                    DescriptionLocstring = $"{targetName} 已解封。"
                };
                NetMessages.Send(msg2, RecipientFilter.All);
            });

            // 发送抽卡提示
            EffectList.SendDrawNotification(controller, effect);
        }
        // ================= 效果：狼人派对 =================
       else if (effect.Id == "wolfdance")
{
    EffectList.SendDrawNotification(controller, effect);

    var hud = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "🐺 狼人派对",
        DescriptionLocstring = $"所有人变成狼人，持续 {effect.Duration} 秒！"
    };
    NetMessages.Send(hud, RecipientFilter.All);

    Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

    var allPawns = Players.GetAll()
        .Select(c => c.GetHeroPawn())
        .Where(p => p != null && p.IsValid)
        .ToList();

    // 只在第一次触发时保存当时在场所有人的原模型和原武器
    if (!_wolfdanceSaved)
    {
        _savedModels.Clear();
        _savedWeapons.Clear();

        foreach (var p in allPawns)
        {
            _savedModels[p] = p.ModelName;

            foreach (var ability in p.AbilityComponent.Abilities)
            {
                if ((ushort)ability.AbilitySlot == WeaponSlot)
                {
                    _savedWeapons[p] = ability.AbilityName;
                    break;
                }
            }
        }

        _wolfdanceSaved = true;
        Console.WriteLine($"[WolfDance] 首次触发，已保存 {allPawns.Count} 名玩家的原模型和原武器，之后不再重新采集。");
    }

    // 只对“第一次保存过的人”换模型和武器，后来加入的人不动
    foreach (var p in allPawns)
{
    if (!_savedModels.ContainsKey(p)) continue;

    p.SetModel(WerewolfModel);

    if (_savedWeapons.TryGetValue(p, out var oldWeapon))
    {
        p.RemoveAbility(oldWeapon);
    }
    p.AddAbility(WerewolfWeapon, WeaponSlot);

    // 狼人期间挂 shrink_ray_cast 粒子，跟随玩家
    var particle = CParticleSystem.Create("particles/upgrades/shrink_ray_cast_smoke.vpcf")
    .AtPosition(p.Position)
    .Spawn();
_wolfParticles[p] = particle;

		  if (particle != null)
    {
        var captured = particle;
        timer.Once(5.Seconds(), () => captured.Destroy());
    }


    // 狼人期间无限子弹，每人一份 KV3，duration 跟随 wolfdance
    using var kv = new KeyValues3();
    kv.SetFloat("duration", effect.Duration);
    p.AddModifier("upgrade_infinitemagazine/modifier_infinite_magazine_active", kv: kv);
		
}

    Console.WriteLine($"[WolfDance] 已对首次保存的玩家变成狼人，{effect.Duration} 秒后恢复。");

    timer.Once(((double)effect.Duration).Seconds(), () =>
{
    foreach (var kvp in _savedModels)
    {
        var p = kvp.Key;
        if (p == null || !p.IsValid) continue;

        // 销毁狼人期间的粒子
        if (_wolfParticles.TryGetValue(p, out var wolfParticle) && wolfParticle != null)
        {
            wolfParticle.Destroy();
        }

        // 兜底移除无限子弹
        p.RemoveModifier("upgrade_infinitemagazine/modifier_infinite_magazine_active");

        p.RemoveAbility(WerewolfWeapon);
        if (_savedWeapons.TryGetValue(p, out var oldWeapon))
        {
            p.AddAbility(oldWeapon, WeaponSlot);
        }

        p.SetModel(kvp.Value);

        // 恢复时也在当前位置播一次粒子
        var restoreParticle = CParticleSystem.Create("particles/upgrades/shrink_ray_cast_smoke.vpcf")
            .AtPosition(p.Position)
            .Spawn();
        if (restoreParticle != null)
        {
            var captured = restoreParticle;
            timer.Once(5.Seconds(), () => captured.Destroy());
        }
    }

    _wolfParticles.Clear();
    Chat.PrintToChatAll("🐺 狼人派对结束，已恢复原模型和武器。");
});}



else if (effect.Id == "pianodrop")
{
    EffectList.SendDrawNotification(controller, effect);

    var hud = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "🎹 钢琴快递",
        DescriptionLocstring = "敌方所有人即将被钢琴砸中 3 次！"
    };
    NetMessages.Send(hud, RecipientFilter.All);

    Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

    int casterTeam = pawn.TeamNum;

    for (int i = 0; i < 3; i++)
    {
        int round = i;
        timer.Once(((double)(3 + i * 3.0f)).Seconds(), () =>
        {
            var enemies = Players.GetAll()
                .Where(c => c != null && c.IsValid)
                .Select(c => c.GetHeroPawn())
                .Where(p => p != null && p.IsValid
                            && p.TeamNum != casterTeam
                            && p.LifeState == LifeState.Alive)
                .ToList();

            if (enemies.Count == 0) return;

            foreach (var enemy in enemies)
            {
                // 先播预警音
               // enemy.EmitSound("Mystical.Piano.AOE.Warning");

                // 1.7 秒后钢琴真正砸下

                    if (enemy == null || !enemy.IsValid) return;

                    // 用砸下那一刻的位置
                    var pos = enemy.Position + Vector3.UnitZ * 100;

                    var particle = CParticleSystem.Create("particles/upgrades/mystical_piano_hit.vpcf")
                        .AtPosition(pos)
                        .StartActive(true)
                        .Spawn();

                    enemy.EmitSound("Mystical.Piano.AOE.Explode");

                    using var kv = new KeyValues3();
                    kv.SetFloat("duration", 0.5f);
                    enemy.AddModifier("modifier_citadel_modifier_knockdown", kv);

                    if (particle != null)
                    {
                        timer.Once(5.Seconds(), () => particle.Destroy());
                    }
                
            }

            Console.WriteLine($"[PianoDrop] 第 {round + 1} 轮，砸向 {enemies.Count} 名敌人。");
        });
    }

}


else if (effect.Id == "deadnote")
{
    EffectList.SendDrawNotification(controller, effect);

    var hud = new CCitadelUserMsg_HudGameAnnouncement
    {
        TitleLocstring = "📓 死亡笔记",
        DescriptionLocstring = "在聊天框输入一个玩家名字，他将立刻死亡。"
    };
    NetMessages.Send(hud, RecipientFilter.Single(controller.Slot));

    // 记录触发者，等待他的下一条聊天
    DeadNoteCasterSlot = controller.Slot;
    Chat.PrintToChat(controller, "📓 死亡笔记已激活，请输入目标玩家名字。");

    // 在控制台列出所有可选目标
  /*  controller.PrintToConsole("========== 死亡笔记·可选目标 ==========");
    int index = 1;
    foreach (var c in Players.GetAll())
{
    if (c == null || !c.IsValid) continue;
    if (c.Slot == controller.Slot) continue;

    var targetPawn = c.GetHeroPawn();   // ← 改名
    if (targetPawn == null || !targetPawn.IsValid) continue;
    if (targetPawn.LifeState != LifeState.Alive) continue;

    controller.PrintToConsole($"[{index}] {c.PlayerName}");
    index++;
}
    controller.PrintToConsole("========================================");*/
}














		


		
//写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写写








		















        
        else
        {
            Console.WriteLine($"[Effects] 未知的效果ID: {effect.Id}");
        }
    }
}

// ==========================================
// 4. 调试指令插件 (/s1)
// ==========================================
public class DebugCommandPlugin : DeadworksPluginBase
{
    public override string Name => "Debug Command Plugin";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine("[DebugCommandPlugin] 调试指令插件已加载！(/s1)");
    }

    [Command("s1", Description = "全服传送到指定点位，3秒后还原")]
    public void CmdS1(Caller caller)
    {
        var tempPositions = new Dictionary<CCitadelPlayerPawn, Vector3>();
        int teamAIndex = 0;
        int teamBIndex = 0;

        foreach (var c in Players.GetAll())
        {
            var p = c.GetHeroPawn();
            if (p == null || !p.IsValid) continue;

            tempPositions[p] = p.Position;

            if (p.TeamNum == 2)
            {
                var pointStr = MapPoints.TeamAPoints[teamAIndex % MapPoints.TeamAPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamAIndex++;
            }
            else if (p.TeamNum == 3)
            {
                var pointStr = MapPoints.TeamBPoints[teamBIndex % MapPoints.TeamBPoints.Length];
                p.Teleport(position: MapPoints.ParsePos(pointStr), angles: null, velocity: null);
                teamBIndex++;
            }

            p.AddModifier("ability_familiar_ability01/effect");
           // p.AddModifier("modifier_citadel_in_hideout_zone");
        }

        caller.Reply("已传送所有玩家，3秒后还原...");

        Timer.Once(5.Seconds(), () =>
        {
            foreach (var kvp in tempPositions)
            {
                if (kvp.Key != null && kvp.Key.IsValid)
                {
                    kvp.Key.RemoveModifier("ability_familiar_ability01/effect");
                   // kvp.Key.RemoveModifier("modifier_citadel_in_hideout_zone");
                    kvp.Key.Teleport(position: kvp.Value, angles: null, velocity: null);
                }
            }
            caller.Reply("坐标已还原！");
        });
    }
}

// ==========================================
// 5. 动态加 Modifier 调试指令 (/e)
// ==========================================
public class SelfEffectCommandPlugin : DeadworksPluginBase
{
    public override string Name => "Self Effect Command Plugin";

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine("[SelfEffectCommandPlugin] /e 命令已加载！");
    }

    





	    [Command("e", Description = "用法: /e <ModifierName> 或 /e s <状态名>")]
    public void CmdApplyModifier(Caller caller, string param1, string? param2 = null)
    {
        var pawn = caller.Player?.GetHeroPawn();
        if (pawn == null || !pawn.IsValid)
        {
            caller.Reply("你必须在游戏内且有英雄才能使用此命令。");
            return;
        }

        if (string.IsNullOrWhiteSpace(param1))
        {
            caller.Reply("请提供 Modifier 或状态名。例如: /e modifier_hero_testing_fast_movespeed");
            return;
        }

        // 🌟 情况一：/e s xxxxx (开关状态)
        if (param1.ToLower() == "s")
        {
            if (string.IsNullOrWhiteSpace(param2))
            {
                caller.Reply("请提供要开关的状态名。例如: /e s UnlimitedAirJumps");
                return;
            }

            // 尝试把玩家输入的字符串转换为 EModifierState 枚举
            if (Enum.TryParse<EModifierState>(param2, true, out var state))
            {
                // 检查当前是否已经开启
                bool isOn = pawn.ModifierProp.HasModifierState(state);
                
                // 切换状态 (取反)
                pawn.ModifierProp.SetModifierState(state, !isOn);

                caller.Reply($"✅ 状态 {state} 已切换为: {(!isOn ? "开启" : "关闭")}");
            }
            else
            {
                caller.Reply($"❌ 找不到状态名: {param2}。请检查拼写是否正确。");
            }
            return;
        }

                // ==========================================
        // 🌟 情况二：/e xxxxx [时长] (施加或移除 Modifier)
        // ==========================================
        string modifierName = param1;
        try
        {
            // 如果玩家输入了第二个参数且是 0，则移除效果
            if (param2 == "0")
            {
                pawn.RemoveModifier(modifierName);
                caller.Reply($"🗑️ 已成功移除 Modifier: {modifierName}");
                return;
            }

            // 如果输入了大于 0 的数字（比如 /e xxx 10），则为指定时长
            if (float.TryParse(param2, out float duration) && duration > 0)
            {
                using var kv = new KeyValues3();
                kv.SetFloat("duration", duration);
                pawn.AddModifier(modifierName, kv);
                caller.Reply($"✅ 已对你施加 Modifier: {modifierName} (持续 {duration} 秒)");
                return;
            }

            // 如果没输入第二个参数，则按默认方式施加（可能是默认时长或永久）
            pawn.AddModifier(modifierName);
            caller.Reply($"✅ 已成功对你施加 Modifier: {modifierName}");
        }
        catch (Exception ex)
        {
            caller.Reply($"❌ 操作失败，请检查 Modifier 名称是否正确。错误信息: {ex.Message}");
        }
    }







[Command("say", Description = "以你的身份向全服发送消息。用法: /say <消息>")]
    public void CmdSay(Caller caller, params string[] messageParts)
    {
        var controller = caller.Player;
        if (controller == null)
        {
            caller.Reply("你必须在游戏内才能使用此命令。");
            return;
        }

        string text = string.Join(' ', messageParts);
        if (string.IsNullOrWhiteSpace(text))
        {
            caller.Reply("请提供要发送的消息。");
            return;
        }

        // 构造聊天网络消息，指定发送者为执行者
        var chatMsg = new CCitadelUserMsg_ChatMsg
        {
            PlayerSlot = controller.Slot,
            Text = text,
            AllChat = true
        };

        // 广播给所有玩家
        NetMessages.Send(chatMsg, RecipientFilter.All);
    }











    
}
