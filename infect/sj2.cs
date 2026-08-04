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

    // ========== 所有英雄列表 ==========
    private readonly List<Heroes> _allHeroes = new List<Heroes>
    {
        Heroes.Inferno,
        Heroes.Gigawatt,
        Heroes.Hornet,
        Heroes.Ghost,
        Heroes.Atlas,
        Heroes.Wraith,
        Heroes.Forge,
        Heroes.Chrono,
        Heroes.Dynamo,
        Heroes.Kelvin,
        Heroes.Haze,
        Heroes.Astro,
        Heroes.Bebop,
        Heroes.Nano,
        Heroes.Orion,
        Heroes.Krill,
        Heroes.Shiv,
        Heroes.Tengu,
        Heroes.Warden,
        Heroes.Yamato,
        Heroes.Lash,
        Heroes.Viscous,
        Heroes.Viper,
        Heroes.Magician,
        Heroes.VampireBat,   // 修正：Vampirebat -> VampireBat
        Heroes.Drifter,
        Heroes.Priest,
        Heroes.Frank,
        Heroes.Bookworm,
        Heroes.Doorman
    };

    // ========== 打乱后的技能队列 ==========
    private List<string> _shuffledSigQueue = new List<string>();
    private int _sigIndex = 0;
    private List<string> _shuffledUltQueue = new List<string>();
    private int _ultIndex = 0;
    private bool _isActive = false;

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
        _passiveTimers.Clear();
        ShuffleSigPool();
        ShuffleUltPool();
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] 插件卸载");
        _isActive = false;
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

    // ========== 执行技能替换 ==========
    private void ExecuteSwap(CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName, int upgradeBits)
    {
        if (pawn == null || !pawn.IsValid) return;

        var controller = GetControllerFromPawn(pawn);
        var playerName = controller?.PlayerName ?? "Unknown";

        var oldAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
        string oldName = oldAbility?.AbilityName ?? "";

        if (string.IsNullOrEmpty(oldName) || oldName == newSkillName)
        {
            return;
        }

        Console.WriteLine($"[{Name}] 替换 {playerName} 槽位 {slot}: {oldName} -> {newSkillName}");

        if (oldAbility != null && oldAbility.IsValid)
        {
            pawn.RemoveAbility(oldAbility);
        }

        var newAbility = pawn.AddAbility(newSkillName, (ushort)slot);
        if (newAbility != null)
        {
            var newBaseAbility = newAbility as CCitadelBaseAbility;
            if (newBaseAbility != null && upgradeBits > 0)
            {
                newBaseAbility.UpgradeBits = upgradeBits;
            }
            controller?.PrintToConsole($"[技能替换] {oldName} -> {newSkillName}");
        }
    }

    // ========== 检查技能是否在冷却中 ==========
    private bool IsOnCooldown(CCitadelBaseAbility ability)
    {
        if (ability == null || !ability.IsValid) return false;
        return ability.CooldownEnd > ability.CooldownStart;
    }

    // ========== 等待冷却结束后替换技能 ==========
    private void WaitForCooldownAndSwap(CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName, int upgradeBits)
    {
        var ability = pawn.AbilityComponent?.GetAbilityBySlot(slot);
        if (ability == null || !ability.IsValid)
        {
            ExecuteSwap(pawn, slot, newSkillName, upgradeBits);
            return;
        }

        if (!IsOnCooldown(ability))
        {
            ExecuteSwap(pawn, slot, newSkillName, upgradeBits);
            return;
        }

        var controller = GetControllerFromPawn(pawn);
        var playerName = controller?.PlayerName ?? "Unknown";
        Console.WriteLine($"[{Name}] {playerName} 槽位 {slot} 技能在冷却中，等待冷却结束...");

        // 先声明 timer 为 null
        IHandle? timer = null;
        timer = Timer.Every(500.Milliseconds(), () =>
        {
            var currentAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
            if (currentAbility == null || !currentAbility.IsValid)
            {
                timer?.Cancel();
                ExecuteSwap(pawn, slot, newSkillName, upgradeBits);
                return;
            }

            if (!IsOnCooldown(currentAbility))
            {
                timer?.Cancel();
                Console.WriteLine($"[{Name}] {playerName} 槽位 {slot} 冷却结束，执行替换");
                ExecuteSwap(pawn, slot, newSkillName, upgradeBits);
            }
        });
    }

    // ========== 处理被动技能：10秒后替换为下一个技能 ==========
    private void ProcessPassiveSkill(CCitadelPlayerPawn pawn, EAbilitySlot slot, string passiveSkillName, int upgradeBits, bool isUltimate)
    {
        var key = (pawn, slot);

        if (_passiveTimers.TryGetValue(key, out var existingTimer))
        {
            existingTimer.TimerHandle?.Cancel();
            _passiveTimers.Remove(key);
        }

        string nextSkill;
        int maxAttempts = 50;
        int attempts = 0;
        do
        {
            nextSkill = isUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();
            attempts++;
        } while (_passiveSkills.Contains(nextSkill) && attempts < maxAttempts);

        var controller = GetControllerFromPawn(pawn);
        var playerName = controller?.PlayerName ?? "Unknown";
        Console.WriteLine($"[{Name}] {playerName} 被动技能 {passiveSkillName} 将在 10 秒后替换为 {nextSkill}");

        var timerInfo = new PassiveTimerInfo(pawn, slot, nextSkill, upgradeBits, isUltimate);

        timerInfo.TimerHandle = Timer.Once(10.Seconds(), () =>
        {
            Console.WriteLine($"[{Name}] {playerName} 10秒已到，替换被动技能 {passiveSkillName} -> {nextSkill}");
            WaitForCooldownAndSwap(pawn, slot, nextSkill, upgradeBits);
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
        int upgradeBits = targetAbility.UpgradeBits;

        string newSkillName = isUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();

        Console.WriteLine($"[{Name}] 玩家 {playerName} 使用了 {abilityName} (槽位 {slot}) -> 将替换为 {newSkillName}");

        if (_passiveSkills.Contains(newSkillName))
        {
            Console.WriteLine($"[{Name}] 新技能 {newSkillName} 是被动技能，先替换再等待10秒");
            ExecuteSwap(pawn, slot, newSkillName, upgradeBits);
            ProcessPassiveSkill(pawn, slot, newSkillName, upgradeBits, isUltimate);
        }
        else
        {
            WaitForCooldownAndSwap(pawn, slot, newSkillName, upgradeBits);
        }

        return HookResult.Continue;
    }

    // ========== !cache 命令：遍历所有英雄加载资源 ==========
    [Command("cache", Description = "遍历所有英雄切换，加载所有英雄资源")]
    public void CmdCacheHeroes(CCitadelPlayerController caller)
    {
        if (caller == null)
        {
            Console.WriteLine($"[{Name}] 该命令只能由玩家执行");
            return;
        }

        var pawn = caller.GetHeroPawn();
        if (pawn == null || !pawn.IsValid)
        {
            caller.PrintToConsole("[资源加载] 无法获取英雄实体");
            return;
        }

        caller.PrintToConsole($"[资源加载] 开始遍历 {_allHeroes.Count} 个英雄，每个切换间隔 4 tick...");

        Console.WriteLine($"[{Name}] 玩家 {caller.PlayerName} 开始加载所有英雄资源");

        int heroIndex = 0;
        var callerRef = caller;

        void SwitchNextHero()
        {
            if (heroIndex >= _allHeroes.Count)
            {
                callerRef.SelectHero(_allHeroes[0]);
                callerRef.PrintToConsole($"[资源加载] 所有英雄资源已加载完成！");
                Console.WriteLine($"[{Name}] 玩家 {callerRef.PlayerName} 所有英雄资源加载完成");
                return;
            }

            var hero = _allHeroes[heroIndex];
            Console.WriteLine($"[{Name}] 切换英雄: {hero} ({heroIndex + 1}/{_allHeroes.Count})");

            callerRef.SelectHero(hero);
            callerRef.PrintToConsole($"[资源加载] 加载英雄: {hero} ({heroIndex + 1}/{_allHeroes.Count})");

            heroIndex++;

            // 4 tick 后切换下一个
            Timer.Once(4.Ticks(), () => SwitchNextHero());
        }

        SwitchNextHero();
    }

    // ========== 启动/停止 ==========
    [Command("sj2", Description = "启动/停止技能替换模式")]
    public void CmdShuffle2(CCitadelPlayerController caller)
    {
        if (_isActive)
        {
            _isActive = false;
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
        if (caller != null) caller.PrintToConsole("[技能替换] 已启动，使用技能后将自动替换");
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
