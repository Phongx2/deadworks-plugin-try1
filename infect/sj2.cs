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
        "ability_afterburn",          // 被动
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
        "citadel_ability_passive_beefy", // 被动
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
        "ability_stacking_damage",     // 被动
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
        "citadel_ability_tangotether",   // 被动
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
        "ability_viper_snakedash",      // 被动
        "ability_magician_magicbolt",
        "ability_magician_cloneturret",
        "ability_magician_animalhexarea",
        "ability_vampirebat_steallife",
        "ability_vampirebat_batblink",
        "ability_vampirebat_lovebites",  // 被动
        "drifter_blood_blast",
        "drifter_shadow_mark",
        "ability_drifter_hunger",        // 被动
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

    // ========== 被动技能列表 ==========
    private readonly HashSet<string> _passiveSkills = new HashSet<string>
    {
        "ability_afterburn",
        "ability_stacking_damage",
        "citadel_ability_passive_beefy",
        "citadel_ability_tangotether",
        "ability_viper_snakedash",
        "ability_vampirebat_lovebites",
        "ability_drifter_hunger"
    };

    // ========== 打乱后的技能队列 ==========
    private List<string> _shuffledSigQueue = new List<string>();
    private int _sigIndex = 0;
    private List<string> _shuffledUltQueue = new List<string>();
    private int _ultIndex = 0;
    private bool _isActive = false;

    // ========== 每个玩家的待替换状态 ==========
    private class PendingSwap
    {
        public CCitadelPlayerPawn Pawn;
        public EAbilitySlot Slot;
        public string OldSkillName;
        public string NewSkillName;
        public int UpgradeBits;
        public bool IsUltimate;
        public float CooldownEnd;

        public PendingSwap(CCitadelPlayerPawn pawn, EAbilitySlot slot, string oldSkillName, string newSkillName, int upgradeBits, bool isUltimate, float cooldownEnd)
        {
            Pawn = pawn;
            Slot = slot;
            OldSkillName = oldSkillName;
            NewSkillName = newSkillName;
            UpgradeBits = upgradeBits;
            IsUltimate = isUltimate;
            CooldownEnd = cooldownEnd;
        }
    }

    private readonly Dictionary<CCitadelPlayerPawn, List<PendingSwap>> _pendingSwaps = new();

    // ========== 每个玩家每个槽位的被动延迟计时器 ==========
    private class PassiveTimerInfo
    {
        public CCitadelPlayerPawn Pawn;
        public EAbilitySlot Slot;
        public string NextSkillName;
        public int UpgradeBits;
        public bool IsUltimate;
        public IHandle? TimerHandle;

        public PassiveTimerInfo(CCitadelPlayerPawn pawn, EAbilitySlot slot, string nextSkillName, int upgradeBits, bool isUltimate)
        {
            Pawn = pawn;
            Slot = slot;
            NextSkillName = nextSkillName;
            UpgradeBits = upgradeBits;
            IsUltimate = isUltimate;
            TimerHandle = null;
        }
    }

    private readonly Dictionary<(CCitadelPlayerPawn, EAbilitySlot), PassiveTimerInfo> _passiveTimers = new();

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
        _pendingSwaps.Clear();
        _passiveTimers.Clear();
        ShuffleSigPool();
        ShuffleUltPool();
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] 插件卸载");
        _isActive = false;
        _pendingSwaps.Clear();
        // 取消所有被动计时器
        foreach (var timer in _passiveTimers.Values)
        {
            timer.TimerHandle?.Cancel();
        }
        _passiveTimers.Clear();
    }

    private void ShuffleSigPool()
    {
        var random = new Random();
        _shuffledSigQueue = _signatureSkills.OrderBy(x => random.Next()).ToList();
        _sigIndex = 0;
        Console.WriteLine($"[{Name}] 1-3技能池已打乱，共 {_shuffledSigQueue.Count} 个技能");
    }

    private void ShuffleUltPool()
    {
        var random = new Random();
        _shuffledUltQueue = _ultimateSkills.OrderBy(x => random.Next()).ToList();
        _ultIndex = 0;
        Console.WriteLine($"[{Name}] 4技能池已打乱，共 {_shuffledUltQueue.Count} 个技能");
    }

    private string GetNextSignatureSkill()
    {
        if (_sigIndex >= _shuffledSigQueue.Count)
        {
            Console.WriteLine($"[{Name}] 1-3技能池已用完，重新打乱");
            ShuffleSigPool();
        }
        return _shuffledSigQueue[_sigIndex++];
    }

    private string GetNextUltimateSkill()
    {
        if (_ultIndex >= _shuffledUltQueue.Count)
        {
            Console.WriteLine($"[{Name}] 4技能池已用完，重新打乱");
            ShuffleUltPool();
        }
        return _shuffledUltQueue[_ultIndex++];
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

    // ========== 执行技能替换（不执行新技能） ==========
    private void ExecuteSwap(CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName, int upgradeBits, bool isUltimate)
    {
        if (pawn == null || !pawn.IsValid) return;

        var controller = GetControllerFromPawn(pawn);
        var playerName = controller?.PlayerName ?? "Unknown";

        // 获取旧技能
        var oldAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
        string oldName = oldAbility?.AbilityName ?? "";

        if (string.IsNullOrEmpty(oldName) || oldName == newSkillName)
        {
            Console.WriteLine($"[{Name}] {playerName} 槽位 {slot} 技能相同或为空，跳过");
            return;
        }

        Console.WriteLine($"[{Name}] 替换 {playerName} 槽位 {slot}: {oldName} -> {newSkillName}");

        // 移除旧技能
        if (oldAbility != null && oldAbility.IsValid)
        {
            pawn.RemoveAbility(oldAbility);
        }

        // 添加新技能
        var newAbility = pawn.AddAbility(newSkillName, (ushort)slot);
        if (newAbility != null)
        {
            var newBaseAbility = newAbility as CCitadelBaseAbility;
            if (newBaseAbility != null && upgradeBits > 0)
            {
                newBaseAbility.UpgradeBits = upgradeBits;
                Console.WriteLine($"[{Name}] 已恢复升级位: {upgradeBits}");
            }
            controller?.PrintToConsole($"[技能替换] {oldName} -> {newSkillName}");
        }
        else
        {
            Console.WriteLine($"[{Name}] 添加技能失败: {newSkillName}");
        }
    }

    // ========== 检查并处理被动技能的延迟替换 ==========
    private void ProcessPassiveSkill(CCitadelPlayerPawn pawn, EAbilitySlot slot, string passiveSkillName, int upgradeBits, bool isUltimate)
    {
        var key = (pawn, slot);

        // 如果已经有一个计时器在运行，取消它
        if (_passiveTimers.TryGetValue(key, out var existingTimer))
        {
            existingTimer.TimerHandle?.Cancel();
            _passiveTimers.Remove(key);
        }

        // 从池中获取下一个技能（跳过被动技能）
        string nextSkill;
        int maxAttempts = 50;
        int attempts = 0;
        do
        {
            nextSkill = isUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();
            attempts++;
        } while (_passiveSkills.Contains(nextSkill) && attempts < maxAttempts);

        if (_passiveSkills.Contains(nextSkill))
        {
            Console.WriteLine($"[{Name}] 警告: 连续抽到被动技能，使用 {nextSkill}");
        }

        Console.WriteLine($"[{Name}] 被动技能 {passiveSkillName} 将在 10 秒后替换为 {nextSkill}");

        var timerInfo = new PassiveTimerInfo(pawn, slot, nextSkill, upgradeBits, isUltimate);

        // 启动 10 秒计时器
        timerInfo.TimerHandle = Timer.Once(10.Seconds(), () =>
        {
            Console.WriteLine($"[{Name}] 10秒已到，替换被动技能 {passiveSkillName} -> {nextSkill}");
            ExecuteSwap(pawn, slot, nextSkill, upgradeBits, isUltimate);
            _passiveTimers.Remove(key);
        });

        _passiveTimers[key] = timerInfo;
    }

    // ========== 监听玩家使用技能 ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(GameEvent ev)
    {
        if (!_isActive) return HookResult.Continue;

        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        string abilityName = ev.GetString("abilityname", "");
        if (string.IsNullOrEmpty(abilityName)) return HookResult.Continue;

        // 获取技能槽位
        var abilities = pawn.AbilityComponent?.Abilities;
        if (abilities == null) return HookResult.Continue;

        CCitadelBaseAbility? targetAbility = null;
        var slot = EAbilitySlot.Invalid;

        foreach (var ability in abilities)
        {
            if (ability == null) continue;
            if (ability.AbilityName == abilityName)
            {
                targetAbility = ability;
                slot = ability.AbilitySlot;
                break;
            }
        }

        if (slot < EAbilitySlot.Signature1 || slot > EAbilitySlot.Signature4) return HookResult.Continue;
        if (targetAbility == null) return HookResult.Continue;

        var controller = GetControllerFromPawn(pawn);
        var playerName = controller?.PlayerName ?? "Unknown";

        bool isUltimate = (slot == EAbilitySlot.Signature4);

        // 检查技能是否在冷却中
        float now = (float)ServerTime.Now;
        if (targetAbility.CooldownEnd > now)
        {
            Console.WriteLine($"[{Name}] {playerName} 技能 {abilityName} 还在冷却中 (冷却结束: {targetAbility.CooldownEnd})，等待冷却");
            // 冷却中，等待冷却结束后再处理
            float cooldownEnd = targetAbility.CooldownEnd;
            var pawnRef = pawn;
            var slotRef = slot;
            var abilityNameRef = abilityName;
            var isUltimateRef = isUltimate;

            Timer.Once((cooldownEnd - now + 0.5f).Seconds(), () =>
            {
                if (!_isActive || pawnRef == null || !pawnRef.IsValid) return;
                // 冷却结束后，检查这个技能是否还存在
                var currentAbility = pawnRef.AbilityComponent?.GetAbilityBySlot(slotRef);
                if (currentAbility == null || !currentAbility.IsValid || currentAbility.AbilityName != abilityNameRef)
                {
                    Console.WriteLine($"[{Name}] 技能已被替换或移除，跳过");
                    return;
                }

                // 获取升级位
                int upgradeBits = currentAbility.UpgradeBits;

                // 从池中获取下一个技能
                string newSkillName = isUltimateRef ? GetNextUltimateSkill() : GetNextSignatureSkill();

                Console.WriteLine($"[{Name}] 冷却结束，替换 {playerName} 槽位 {slotRef}: {abilityNameRef} -> {newSkillName}");

                // 检查新技能是否为被动技能
                if (_passiveSkills.Contains(newSkillName))
                {
                    Console.WriteLine($"[{Name}] 新技能 {newSkillName} 是被动技能，先替换再等待10秒");
                    // 先替换为被动技能
                    ExecuteSwap(pawnRef, slotRef, newSkillName, upgradeBits, isUltimateRef);
                    // 然后处理被动技能的延迟替换
                    ProcessPassiveSkill(pawnRef, slotRef, newSkillName, upgradeBits, isUltimateRef);
                }
                else
                {
                    // 直接替换
                    ExecuteSwap(pawnRef, slotRef, newSkillName, upgradeBits, isUltimateRef);
                }
            });

            return HookResult.Continue;
        }

        // 技能不在冷却中，正常处理
        // 获取升级位
        int upgradeBits = targetAbility.UpgradeBits;

        // 从池中获取下一个技能
        string newSkillName = isUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();

        Console.WriteLine($"[{Name}] 玩家 {playerName} 使用了 {abilityName} (槽位 {slot}) -> 替换为 {newSkillName}");

        // 检查新技能是否为被动技能
        if (_passiveSkills.Contains(newSkillName))
        {
            Console.WriteLine($"[{Name}] 新技能 {newSkillName} 是被动技能，先替换再等待10秒");
            // 先替换为被动技能
            ExecuteSwap(pawn, slot, newSkillName, upgradeBits, isUltimate);
            // 然后处理被动技能的延迟替换
            ProcessPassiveSkill(pawn, slot, newSkillName, upgradeBits, isUltimate);
        }
        else
        {
            // 直接替换
            ExecuteSwap(pawn, slot, newSkillName, upgradeBits, isUltimate);
        }

        return HookResult.Continue;
    }

    // ========== 启动/停止 ==========
    [Command("sj2", Description = "启动/停止技能替换模式")]
    public void CmdShuffle2(CCitadelPlayerController caller)
    {
        if (_isActive)
        {
            _isActive = false;
            _pendingSwaps.Clear();
            foreach (var timer in _passiveTimers.Values)
            {
                timer.TimerHandle?.Cancel();
            }
            _passiveTimers.Clear();
            Console.WriteLine($"[{Name}] 已停止");
            if (caller != null) caller.PrintToConsole("[技能替换] 已停止");
            return;
        }

        ShuffleSigPool();
        ShuffleUltPool();
        _isActive = true;
        Console.WriteLine($"[{Name}] 已启动");
        if (caller != null) caller.PrintToConsole("[技能替换] 已启动，使用技能冷却结束后将自动替换");
    }

    // ========== 手动重洗技能池 ==========
    [Command("sj2_shuffle", Description = "手动重洗技能池")]
    public void CmdShufflePool(CCitadelPlayerController caller)
    {
        ShuffleSigPool();
        ShuffleUltPool();
        Console.WriteLine($"[{Name}] 技能池已手动重洗");
        if (caller != null) caller.PrintToConsole("[技能替换] 技能池已手动重洗");
    }
}
