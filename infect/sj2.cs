using DeadworksManaged.Api;
using System.Numerics;

namespace SkillShuffle2;

public class SkillShuffle2Plugin : DeadworksPluginBase
{
    public override string Name => "Skill Shuffle 2";

    // ========== 技能库（1-3技能，共享池） ==========
    private readonly List<string> _signatureSkills = new List<string>
    {
        "ability_incendiary_projectile",
        "ability_flame_dash",
        "ability_afterburn",
        "citadel_ability_lightning_ball",
        "citadel_ability_static_charge",
        "ability_power_surge",
        "citadel_ability_hornet_chain",
        "citadel_ability_hornet_leap",
        "citadel_ability_hornet_sting",
        "ability_blood_bomb",
        "ability_life_drain",
        "ability_blood_shards",
        "citadel_ability_bull_heal",
        "citadel_ability_bull_charge",
        "citadel_ability_passive_beefy",
        "citadel_ability_card_toss",
        "citadel_ability_projectmind",
        "citadel_ability_wraith_rapidfire",
        "citadel_ability_shieldedsentry",
        "citadel_ability_mobile_resupply",
        "citadel_ability_fissure_wall",
        "citadel_ability_chrono_pulse_grenade",
        "citadel_ability_chrono_time_wall",
        "citadel_ability_chrono_kinetic_carbine",
        "citadel_ability_stomp",
        "citadel_ability_void_sphere",
        "citadel_ability_nikuman",
        "ability_ice_grenade",
        "ability_icepath",
        "ability_icebeam",
        "ability_sleep_dagger",
        "ability_smoke_bomb",
        "ability_stacking_damage",
        "ability_explosive_barrel",
        "ability_bounce_pad",
        "ability_crackshot",
        "citadel_ability_uppercut",
        "citadel_ability_sticky_bomb",
        "citadel_ability_hook",
        "ability_nano_clustergrenade",
        "ability_nano_dash",
        "ability_nano_catform",
        "ability_charged_shot",
        "ability_power_jump",
        "ability_immobilize_trap",
        "ability_intimidate",
        "ability_burrow",
        "ability_throw_sand",
        "citadel_ability_shiv_dagger",
        "citadel_ability_shiv_dash",
        "citadel_ability_shiv_defer_damage",
        "citadel_ability_tengu_urn",
        "citadel_ability_tangotether",
        "citadel_ability_tengu_stone_form",
        "ability_warden_crowd_control",
        "ability_warden_high_alert",
        "ability_warden_lock_down",
        "citadel_ability_power_slash",
        "citadel_ability_flying_strike",
        "citadel_ability_healing_slash",
        "citadel_ability_lash_down_strike",
        "citadel_ability_lash",
        "ability_lash_flog",
        "viscous_goo_grenade",
        "viscous_restorative_goo",
        "viscous_telepunch",
        "ability_viper_debuffdagger",
        "ability_viper_venom",
        "ability_viper_snakedash",
        "ability_magician_magicbolt",
        "ability_magician_cloneturret",
        "ability_magician_animalhexarea",
        "ability_vampirebat_steallife",
        "ability_vampirebat_batblink",
        "ability_vampirebat_lovebites",
        "drifter_blood_blast",
        "drifter_shadow_mark",
        "ability_drifter_hunger",
        "ability_priest_flashbang",
        "ability_priest_knockback",
        "ability_priest_beartrap",
        "ability_frank_shocktarget2",
        "ability_frank_selfzap",
        "ability_frank_painaura",
        "ability_bookworm_dragonfire",
        "ability_bookworm_knightbarrier",
        "ability_bookworm_aoemagic",
        "ability_doorman_bomb",
        "ability_doorman_doorway",
        "ability_doorman_luggage_cart"
    };

    private readonly List<string> _ultimateSkills = new List<string>
    {
        "ability_fire_bomb",
        "citadel_ability_storm_cloud",
        "citadel_ability_hornet_snipe",
        "ability_health_swap",
        "citadel_ability_bull_leap",
        "citadel_ability_psychic_lift",
        "citadel_ability_rocket_barrage",
        "citadel_ability_chrono_swap",
        "citadel_ability_self_vacuum",
        "ability_ice_dome",
        "ability_bullet_flurry",
        "ability_gravity_lasso",
        "citadel_ability_bebop_laser_beam",
        "ability_nano_shadow_pulse",
        "ability_guided_arrow",
        "ability_ult_combo",
        "citadel_ability_shiv_killing_blow",
        "citadel_ability_tengu_airlift",
        "ability_warden_riot_protocol",
        "citadel_ability_infinity_slash",
        "citadel_ability_lash_ultimate",
        "viscous_goo_bowling_ball",
        "ability_viper_petrifybola",
        "ability_magician_copyult",
        "ability_vampirebat_batswarm",
        "drifter_darkness",
        "ability_priest_weaponswap",
        "ability_frank_revive",
        "ability_bookworm_knightcharge",
        "ability_doorman_hotel"
    };

    // ========== 打乱后的技能队列（1-3独立） ==========
    private List<string> _shuffledSigQueue = new List<string>();
    private int _sigIndex = 0;

    // ========== 打乱后的技能队列（4独立） ==========
    private List<string> _shuffledUltQueue = new List<string>();
    private int _ultIndex = 0;

    private bool _isActive = false;

    // ========== 分步应用状态 ==========
    private enum ApplyStep
    {
        SaveUpgrade,
        RemoveOld,
        AddNew,
        RestoreUpgrade,
        Done
    }

    private class AbilitySwapState
    {
        public CCitadelPlayerPawn Pawn;
        public EAbilitySlot Slot;
        public string NewSkillName;
        public int UpgradeBits;
        public ApplyStep Step;

        public AbilitySwapState(CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName, int upgradeBits, ApplyStep step)
        {
            Pawn = pawn;
            Slot = slot;
            NewSkillName = newSkillName;
            UpgradeBits = upgradeBits;
            Step = step;
        }
    }

    private AbilitySwapState? _currentState = null;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] ========== 插件加载 ==========");
        Console.WriteLine($"[{Name}] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        Console.WriteLine($"[{Name}] ===============================");
        _isActive = false;
        _sigIndex = 0;
        _ultIndex = 0;
        _shuffledSigQueue.Clear();
        _shuffledUltQueue.Clear();
        ShuffleSigPool();
        ShuffleUltPool();
        CCitadelPlayerController.PrintToConsoleAll("[技能替换] 插件已加载，使用 !sj2 启动");
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] 插件卸载");
        _isActive = false;
        _currentState = null;
        CCitadelPlayerController.PrintToConsoleAll("[技能替换] 插件已卸载");
    }

    // ========== 独立打乱 1-3 技能池 ==========
    private void ShuffleSigPool()
    {
        var random = new Random();
        _shuffledSigQueue = _signatureSkills.OrderBy(x => random.Next()).ToList();
        _sigIndex = 0;
        string msg = $"[技能替换] 1-3技能池已打乱，共 {_shuffledSigQueue.Count} 个技能";
        Console.WriteLine($"[{Name}] {msg}");
        CCitadelPlayerController.PrintToConsoleAll(msg);
    }

    // ========== 独立打乱 4 技能池 ==========
    private void ShuffleUltPool()
    {
        var random = new Random();
        _shuffledUltQueue = _ultimateSkills.OrderBy(x => random.Next()).ToList();
        _ultIndex = 0;
        string msg = $"[技能替换] 4技能池已打乱，共 {_shuffledUltQueue.Count} 个技能";
        Console.WriteLine($"[{Name}] {msg}");
        CCitadelPlayerController.PrintToConsoleAll(msg);
    }

    // ========== 从 1-3 技能池获取下一个 ==========
    private string GetNextSignatureSkill()
    {
        if (_sigIndex >= _shuffledSigQueue.Count)
        {
            string msg = $"[技能替换] 1-3技能池已用完 (索引 {_sigIndex}/{_shuffledSigQueue.Count})，独立重新打乱";
            Console.WriteLine($"[{Name}] {msg}");
            CCitadelPlayerController.PrintToConsoleAll(msg);
            ShuffleSigPool();
        }
        string skill = _shuffledSigQueue[_sigIndex++];
        Console.WriteLine($"[{Name}] 从1-3池取出技能: {skill} (索引 {_sigIndex-1}/{_shuffledSigQueue.Count})");
        return skill;
    }

    // ========== 从 4 技能池获取下一个 ==========
    private string GetNextUltimateSkill()
    {
        if (_ultIndex >= _shuffledUltQueue.Count)
        {
            string msg = $"[技能替换] 4技能池已用完 (索引 {_ultIndex}/{_shuffledUltQueue.Count})，独立重新打乱";
            Console.WriteLine($"[{Name}] {msg}");
            CCitadelPlayerController.PrintToConsoleAll(msg);
            ShuffleUltPool();
        }
        string skill = _shuffledUltQueue[_ultIndex++];
        Console.WriteLine($"[{Name}] 从4池取出技能: {skill} (索引 {_ultIndex-1}/{_shuffledUltQueue.Count})");
        return skill;
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

    // ========== 分步执行技能替换 ==========
    private void ProcessNextStep()
    {
        if (_currentState == null)
        {
            Console.WriteLine($"[{Name}] [DEBUG] ProcessNextStep: 状态为空，退出");
            return;
        }

        var state = _currentState;
        var p = state.Pawn;
        var s = state.Slot;
        var newName = state.NewSkillName;
        var upgradeBits = state.UpgradeBits;

        if (p == null || !p.IsValid)
        {
            Console.WriteLine($"[{Name}] [ERROR] Pawn 无效，放弃替换");
            _currentState = null;
            return;
        }

        switch (state.Step)
        {
            case ApplyStep.SaveUpgrade:
            {
                Console.WriteLine($"[{Name}] [DEBUG] Step: SaveUpgrade - 保存升级位");
                var oldAbility = p.AbilityComponent?.GetAbilityBySlot(s);
                if (oldAbility != null && oldAbility.IsValid)
                {
                    state.UpgradeBits = oldAbility.UpgradeBits;
                    Console.WriteLine($"[{Name}] [DEBUG] 保存升级位: {state.UpgradeBits}");
                }
                else
                {
                    state.UpgradeBits = 0;
                    Console.WriteLine($"[{Name}] [DEBUG] 槽位为空，升级位设为 0");
                }

                state.Step = ApplyStep.RemoveOld;
                Timer.NextTick(() => ProcessNextStep());
                break;
            }

            case ApplyStep.RemoveOld:
            {
                Console.WriteLine($"[{Name}] [DEBUG] Step: RemoveOld - 移除旧技能");
                var oldAbility2 = p.AbilityComponent?.GetAbilityBySlot(s);
                if (oldAbility2 != null && oldAbility2.IsValid)
                {
                    var oldName = oldAbility2.AbilityName;
                    Console.WriteLine($"[{Name}] [DEBUG] 找到旧技能: {oldName}");
                    if (oldName != newName)
                    {
                        p.RemoveAbility(oldAbility2);
                        Console.WriteLine($"[{Name}] [DEBUG] 已移除旧技能: {oldName}");
                    }
                    else
                    {
                        Console.WriteLine($"[{Name}] [DEBUG] 新旧技能相同，跳过替换");
                        _currentState = null;
                        return;
                    }
                }
                else
                {
                    Console.WriteLine($"[{Name}] [DEBUG] 槽位为空，无需移除");
                }

                state.Step = ApplyStep.AddNew;
                Timer.NextTick(() => ProcessNextStep());
                break;
            }

            case ApplyStep.AddNew:
            {
                Console.WriteLine($"[{Name}] [DEBUG] Step: AddNew - 添加新技能: {newName} 到槽位 {s}");
                p.AddAbility(newName, (ushort)s);
                Console.WriteLine($"[{Name}] [DEBUG] 新技能已添加");

                state.Step = ApplyStep.RestoreUpgrade;
                Timer.NextTick(() => ProcessNextStep());
                break;
            }

            case ApplyStep.RestoreUpgrade:
            {
                Console.WriteLine($"[{Name}] [DEBUG] Step: RestoreUpgrade - 恢复升级位");
                if (state.UpgradeBits > 0)
                {
                    var newAbility = p.AbilityComponent?.GetAbilityBySlot(s);
                    if (newAbility != null && newAbility.IsValid)
                    {
                        newAbility.UpgradeBits = state.UpgradeBits;
                        Console.WriteLine($"[{Name}] [DEBUG] 已恢复升级位: {state.UpgradeBits}");
                    }
                    else
                    {
                        Console.WriteLine($"[{Name}] [DEBUG] 新技能无效，无法恢复升级位");
                    }
                }
                else
                {
                    Console.WriteLine($"[{Name}] [DEBUG] 升级位为 0，无需恢复");
                }

                // 执行新技能
                Console.WriteLine($"[{Name}] [DEBUG] 执行新技能: {newName}");
                int result = p.ExecuteAbilityBySlot(s, false, 0);
                if (result == 0)
                {
                    Console.WriteLine($"[{Name}] 新技能已执行: {newName}");
                    var controller = GetControllerFromPawn(p);
                    controller?.PrintToConsole($"[技能替换] 新技能已执行: {newName}");
                }
                else
                {
                    Console.WriteLine($"[{Name}] 新技能执行失败: 返回值 {result}");
                    var controller = GetControllerFromPawn(p);
                    controller?.PrintToConsole($"[技能替换] 新技能执行失败: 返回值 {result}");
                }

                state.Step = ApplyStep.Done;
                _currentState = null;
                Console.WriteLine($"[{Name}] [DEBUG] 技能替换完成");
                break;
            }

            case ApplyStep.Done:
            default:
                _currentState = null;
                break;
        }
    }

    // ========== 监听玩家使用技能 ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(GameEvent ev)
    {
        if (!_isActive) return HookResult.Continue;

        // 如果当前有正在进行的替换，跳过新的事件
        if (_currentState != null)
        {
            Console.WriteLine($"[{Name}] [DEBUG] 正在处理上一个技能替换，跳过新事件");
            return HookResult.Continue;
        }

        Console.WriteLine($"[{Name}] [DEBUG] player_used_ability 事件触发");

        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null)
        {
            Console.WriteLine($"[{Name}] [DEBUG] 无法获取施法者 Pawn");
            return HookResult.Continue;
        }

        string abilityName = ev.GetString("abilityname", "");
        Console.WriteLine($"[{Name}] [DEBUG] 技能名称: {abilityName}");

        if (string.IsNullOrEmpty(abilityName))
        {
            Console.WriteLine($"[{Name}] [DEBUG] 技能名称为空，跳过");
            return HookResult.Continue;
        }

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

        Console.WriteLine($"[{Name}] [DEBUG] 玩家 {playerName} 使用了技能: {abilityName} (槽位 {slot})");
        controller?.PrintToConsole($"[技能替换] 你使用了 {abilityName} (槽位 {slot})");

        bool isUltimate = (slot == EAbilitySlot.Signature4);
        Console.WriteLine($"[{Name}] [DEBUG] 是否为4技能: {isUltimate}");

        string newSkillName = isUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();
        Console.WriteLine($"[{Name}] [DEBUG] 从池中获取新技能: {newSkillName}");

        if (abilityName == newSkillName)
        {
            Console.WriteLine($"[{Name}] [DEBUG] 新旧技能相同 ({abilityName})，跳过替换");
            controller?.PrintToConsole($"[技能替换] 技能相同，跳过: {abilityName}");
            return HookResult.Continue;
        }

        Console.WriteLine($"[{Name}] 替换技能: {abilityName} -> {newSkillName} (槽位 {slot})");
        controller?.PrintToConsole($"[技能替换] 替换技能: {abilityName} -> {newSkillName}");

        // 创建状态并开始分步执行
        _currentState = new AbilitySwapState(pawn, slot, newSkillName, 0, ApplyStep.SaveUpgrade);
        Timer.NextTick(() => ProcessNextStep());

        return HookResult.Continue;
    }

    // ========== 启动/停止功能 ==========
    [Command("sj2", Description = "启动/停止技能替换模式（使用技能时自动替换）")]
    public void CmdShuffle2(CCitadelPlayerController caller)
    {
        if (_isActive)
        {
            _isActive = false;
            _currentState = null;
            Console.WriteLine($"[{Name}] 已停止");
            string msg = "[技能替换] 已停止";
            Console.WriteLine($"[{Name}] {msg}");
            if (caller != null) caller.PrintToConsole(msg);
            CCitadelPlayerController.PrintToConsoleAll(msg);
            return;
        }

        ShuffleSigPool();
        ShuffleUltPool();
        _isActive = true;
        Console.WriteLine($"[{Name}] 已启动");
        string msg2 = "[技能替换] 已启动，使用技能时将自动替换";
        Console.WriteLine($"[{Name}] {msg2}");
        if (caller != null) caller.PrintToConsole(msg2);
        CCitadelPlayerController.PrintToConsoleAll(msg2);
    }

    // ========== 手动重洗技能池 ==========
    [Command("sj2_shuffle", Description = "手动重洗技能池")]
    public void CmdShufflePool(CCitadelPlayerController caller)
    {
        ShuffleSigPool();
        ShuffleUltPool();
        Console.WriteLine($"[{Name}] 技能池已手动重洗");
        string msg = "[技能替换] 技能池已手动重洗";
        if (caller != null) caller.PrintToConsole(msg);
        CCitadelPlayerController.PrintToConsoleAll(msg);
    }
}
