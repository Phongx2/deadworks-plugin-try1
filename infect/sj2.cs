using DeadworksManaged.Api;
using System.Numerics;

namespace SkillShuffle2;

public override void OnStartupServer()
{
    ConVar.Find("citadel_active_lane")?.SetInt(4);
}



public class SkillShuffle2Plugin : DeadworksPluginBase
{
    public override string Name => "Skill Shuffle 2";

    // ========== 技能库（1-2技能，共享池） ==========
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
        "citadel_ability_uppercut",
        "citadel_ability_sticky_bomb",
        "citadel_ability_hook",
        "ability_nano_clustergrenade",
        "ability_nano_dash",
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
        "ability_magician_animalhexarea",
        "ability_vampirebat_steallife",
        "ability_vampirebat_batblink",
        "ability_vampirebat_lovebites",
        "drifter_blood_blast",
        "drifter_shadow_mark",
        "ability_drifter_hunger",
        "ability_drifter_hunger",
        "ability_priest_flashbang",
        "ability_priest_knockback",
        "ability_priest_beartrap",
        "ability_frank_shocktarget2",
        "ability_frank_selfzap",
        "ability_bookworm_dragonfire",
        "ability_bookworm_knightbarrier",
        "ability_bookworm_aoemagic",
        "ability_doorman_bomb",
        "ability_doorman_luggage_cart", 
            "ability_punkgoat_ult",
            "ability_punkgoat_goatflip",
            "ability_punkgoat_blasted",
            "ability_necro_hauntingskull",
            "ability_necro_zombiewall",
            "ability_necro_fear",
            "ability_fencer_throwblade",
            "ability_fencer_riposte",
            "ability_fencer_lunge",
            "ability_familiar_ability02",
            "ability_familiar_attach",
            "ability_familiar_helpinghands",
            "ability_werewolf_unloadgun",
            "ability_werewolf_kickflip",
            "ability_werewolf_netshot",
            "ability_unicorn_radiantblast",
            "ability_unicorn_prismaticguard",
            "ability_unicorn_luminousstrike"
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
       // "ability_magician_copyult",
        "ability_vampirebat_batswarm",
        "drifter_darkness",
        "ability_priest_weaponswap",
        "ability_frank_revive",
        "ability_bookworm_knightcharge",
        "ability_doorman_hotel",
         "ability_punkgoat_tether",
            "ability_necro_gravestone",
            "ability_fencer_ultimate",
            "ability_familiar_ability01",
            "ability_werewolf_transformation",
            "ability_unicorn_dazzlingorb"
    };

    // ========== 被动技能列表 ==========
    private readonly HashSet<string> _passiveSkills = new HashSet<string>
    {
        "ability_afterburn",
        "ability_stacking_damage",
        "citadel_ability_passive_beefy",
        "citadel_ability_tangotether",
        "ability_viper_snakedash",
        "ability_crackshot",
        "ability_vampirebat_lovebites",
        "citadel_ability_tangotether",
            "ability_necro_fear",
        "ability_drifter_hunger"
    };


 // ========== 需要延迟替换的技能列表（使用后10秒才替换） ==========
    private readonly HashSet<string> _delayedSkills = new HashSet<string>
    {
        // 在这里添加需要延迟替换的技能名称
        // 例如：
        // "ability_flame_dash",
        // "citadel_ability_lightning_ball",
        // "ability_ice_grenade"
    };

    // ========== 打乱后的技能队列 ==========
    private List<string> _shuffledSigQueue = new List<string>();
    private int _sigIndex = 0;
    private List<string> _shuffledUltQueue = new List<string>();
    private int _ultIndex = 0;

    // ========== 状态标志 ==========
    private bool _isActive = false;
    private bool _sigLuan = false;  // 1-2技能池是否已打乱
    private bool _ultLuan = false;  // 4技能池是否已打乱

    // ========== 被动技能延迟信息 ==========
    private class PassiveDelayInfo
    {
        public CCitadelPlayerPawn Pawn;
        public EAbilitySlot Slot;
        public string PassiveSkillName;
        public int UpgradeBits;
        public bool IsUltimate;
        public IHandle? DelayTimer;

        public PassiveDelayInfo(CCitadelPlayerPawn pawn, EAbilitySlot slot, string passiveSkillName, int upgradeBits, bool isUltimate)
        {
            Pawn = pawn;
            Slot = slot;
            PassiveSkillName = passiveSkillName;
            UpgradeBits = upgradeBits;
            IsUltimate = isUltimate;
            DelayTimer = null;
        }
    }

    // ========== 延迟技能替换信息 ==========
    private class DelayedSkillInfo
    {
        public CCitadelPlayerPawn Pawn;
        public EAbilitySlot Slot;
        public string SkillName;
        public int UpgradeBits;
        public bool IsUltimate;
        public IHandle? DelayTimer;

        public DelayedSkillInfo(CCitadelPlayerPawn pawn, EAbilitySlot slot, string skillName, int upgradeBits, bool isUltimate)
        {
            Pawn = pawn;
            Slot = slot;
            SkillName = skillName;
            UpgradeBits = upgradeBits;
            IsUltimate = isUltimate;
            DelayTimer = null;
        }
    }

    private readonly Dictionary<(CCitadelPlayerPawn, EAbilitySlot), PassiveDelayInfo> _passiveDelays = new();
    private readonly Dictionary<(CCitadelPlayerPawn, EAbilitySlot), DelayedSkillInfo> _delayedSkillTimers = new();

    public override void OnLoad(bool isReload)
    {
        _isActive = false;
        _sigLuan = false;
        _ultLuan = false;
        _sigIndex = 0;
        _ultIndex = 0;
        _shuffledSigQueue.Clear();
        _shuffledUltQueue.Clear();
        _passiveDelays.Clear();
        _delayedSkillTimers.Clear();
        //CCitadelPlayerController.PrintToConsoleAll("[技能替换] 插件已加载，使用 !sj2 启动");
    }

    public override void OnUnload()
    {
        _isActive = false;
        foreach (var delay in _passiveDelays.Values)
        {
            delay.DelayTimer?.Cancel();
        }
        _passiveDelays.Clear();
        foreach (var delay in _delayedSkillTimers.Values)
        {
            delay.DelayTimer?.Cancel();
        }
        _delayedSkillTimers.Clear();
        //CCitadelPlayerController.PrintToConsoleAll("[技能替换] 插件已卸载");
    }

    public override void OnStartupServer()
{
    ConVar.Find("citadel_active_lane")?.SetInt(4);
}

    private void ShuffleSigPool()
    {
        var random = new Random();
        _shuffledSigQueue = _signatureSkills.OrderBy(x => random.Next()).ToList();
        _sigIndex = 0;
        _sigLuan = true;
        //CCitadelPlayerController.PrintToConsoleAll($"[技能替换] 1-2技能池已打乱，共 {_shuffledSigQueue.Count} 个技能");
    }

    private void ShuffleUltPool()
    {
        var random = new Random();
        _shuffledUltQueue = _ultimateSkills.OrderBy(x => random.Next()).ToList();
        _ultIndex = 0;
        _ultLuan = true;
        //CCitadelPlayerController.PrintToConsoleAll($"[技能替换] 4技能池已打乱，共 {_shuffledUltQueue.Count} 个技能");
    }

    private string GetNextSignatureSkill()
    {
        if (_sigIndex >= _shuffledSigQueue.Count - 1)
        {
            var skill = _shuffledSigQueue[_sigIndex];
            _sigIndex++;
            Timer.Once(8.Ticks(), () =>
            {
                ShuffleSigPool();
            });
            return skill;
        }

        if (_sigIndex >= _shuffledSigQueue.Count)
        {
            ShuffleSigPool();
            return _shuffledSigQueue[_sigIndex++];
        }

        return _shuffledSigQueue[_sigIndex++];
    }

    private string GetNextUltimateSkill()
    {
        if (_ultIndex >= _shuffledUltQueue.Count - 1)
        {
            var skill = _shuffledUltQueue[_ultIndex];
            _ultIndex++;
            Timer.Once(8.Ticks(), () =>
            {
                ShuffleUltPool();
            });
            return skill;
        }

        if (_ultIndex >= _shuffledUltQueue.Count)
        {
            ShuffleUltPool();
            return _shuffledUltQueue[_ultIndex++];
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

        controller?.PrintToConsole($"[技能替换] {oldName} -> {newSkillName}");

        if (oldAbility != null && oldAbility.IsValid)
        {
            pawn.RemoveAbility(oldAbility);
        }

        var newAbility = pawn.AddAbility(newSkillName, (ushort)slot);
        if (newAbility != null)
        {
            var newBaseAbility = newAbility as CCitadelBaseAbility;
            var capturedUpgradeBits = upgradeBits;
            var capturedSlot = slot;
            var capturedPawn = pawn;
            Timer.Once(4.Ticks(), () =>
            {
                var abilityToRestore = capturedPawn.AbilityComponent?.GetAbilityBySlot(capturedSlot);
                if (abilityToRestore != null && abilityToRestore.IsValid && capturedUpgradeBits > 0)
                {
                    abilityToRestore.UpgradeBits = capturedUpgradeBits;
                }
            });
        }
    }

    // ========== 处理被动技能：10秒后替换为下一个技能 ==========
    private void ProcessPassiveSkill(CCitadelPlayerPawn pawn, EAbilitySlot slot, string passiveSkillName, int upgradeBits, bool isUltimate)
    {
        var key = (pawn, slot);

        if (_passiveDelays.TryGetValue(key, out var existingDelay))
        {
            existingDelay.DelayTimer?.Cancel();
            _passiveDelays.Remove(key);
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
        controller?.PrintToConsole($"[技能替换] 被动技能将在 10 秒后替换");

        var delayInfo = new PassiveDelayInfo(pawn, slot, passiveSkillName, upgradeBits, isUltimate);

        delayInfo.DelayTimer = Timer.Once(10.Seconds(), () =>
        {
            controller?.PrintToConsole($"[技能替换] 被动技能已替换");

            var currentAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
            int currentUpgradeBits = currentAbility != null && currentAbility.IsValid ? currentAbility.UpgradeBits : upgradeBits;

            ExecuteSwap(pawn, slot, nextSkill, currentUpgradeBits);
            _passiveDelays.Remove(key);
        });

        _passiveDelays[key] = delayInfo;
    }

    // ========== 执行延迟技能替换（10秒后替换） ==========
    private void ProcessDelayedSkill(CCitadelPlayerPawn pawn, EAbilitySlot slot, string skillName, int upgradeBits, bool isUltimate)
    {
        var key = (pawn, slot);

        // 取消之前的延迟计时器
        if (_delayedSkillTimers.TryGetValue(key, out var existingDelay))
        {
            existingDelay.DelayTimer?.Cancel();
            _delayedSkillTimers.Remove(key);
        }

        var controller = GetControllerFromPawn(pawn);
        controller?.PrintToConsole($"[技能替换] 技能 {skillName} 将在 10 秒后替换");

        var delayInfo = new DelayedSkillInfo(pawn, slot, skillName, upgradeBits, isUltimate);

        delayInfo.DelayTimer = Timer.Once(10.Seconds(), () =>
        {
            controller?.PrintToConsole($"[技能替换] 延迟技能 {skillName} 已开始替换");

            // 获取下一个技能
            string nextSkill = isUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();

            // 检查当前技能是否还存在
            var currentAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
            int currentUpgradeBits = currentAbility != null && currentAbility.IsValid ? currentAbility.UpgradeBits : upgradeBits;

            // 执行替换
            ExecuteSwap(pawn, slot, nextSkill, currentUpgradeBits);

            // 如果新技能是被动技能，启动被动替换
            if (_passiveSkills.Contains(nextSkill))
            {
                ProcessPassiveSkill(pawn, slot, nextSkill, currentUpgradeBits, isUltimate);
            }

            _delayedSkillTimers.Remove(key);
        });

        _delayedSkillTimers[key] = delayInfo;
    }

    // ========== 监听玩家使用技能 ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(GameEvent ev)
    {
        if (!_isActive) return HookResult.Continue;
        if (!_sigLuan || !_ultLuan)
        {
            return HookResult.Continue;
        }

        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null)
        {
            return HookResult.Continue;
        }

        string abilityName = ev.GetString("abilityname", "");

        if (string.IsNullOrEmpty(abilityName))
        {
            return HookResult.Continue;
        }

        var abilities = pawn.AbilityComponent?.Abilities;
        if (abilities == null)
        {
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
                break;
            }
        }

        // 检测 Signature1, Signature2, Signature4
        if (slot != EAbilitySlot.Signature1 && slot != EAbilitySlot.Signature2 && slot != EAbilitySlot.Signature4)
        {
            return HookResult.Continue;
        }

        if (targetAbility == null)
        {
            return HookResult.Continue;
        }

        var controller = GetControllerFromPawn(pawn);
        var playerName = controller?.PlayerName ?? "Unknown";

        bool isUltimate = (slot == EAbilitySlot.Signature4);
        int upgradeBits = targetAbility.UpgradeBits;
        float cooldownStart = targetAbility.CooldownStart;
        float cooldownEnd = targetAbility.CooldownEnd;
        float remainingCooldown = cooldownEnd - cooldownStart;
        if (remainingCooldown < 0) remainingCooldown = 0;

        // ========== 检查是否是需要延迟替换的技能 ==========
        if (_delayedSkills.Contains(abilityName))
        {
            // 延迟技能：取消正常的替换逻辑，改为10秒后替换
            var delayedKey = (pawn, slot);
            
            // 取消之前可能存在的延迟计时器
            if (_delayedSkillTimers.TryGetValue(delayedKey, out var existingDelay))
            {
                existingDelay.DelayTimer?.Cancel();
                _delayedSkillTimers.Remove(delayedKey);
            }

            // 启动10秒延迟替换
            ProcessDelayedSkill(pawn, slot, abilityName, upgradeBits, isUltimate);
            
            // 如果技能有冷却，可以在这里处理冷却期间的替换逻辑
            // 但我们使用延迟替换，所以直接返回
            return HookResult.Continue;
        }

        // ========== 正常的技能替换逻辑 ==========
        var capturedPawn = pawn;
        var capturedSlot = slot;
        var capturedUpgradeBits = upgradeBits;
        var capturedIsUltimate = isUltimate;
        var capturedAbilityName = abilityName;

        Timer.Once(8.Ticks(), () =>
        {
            var currentAbility = capturedPawn.AbilityComponent?.GetAbilityBySlot(capturedSlot);
            if (currentAbility == null || !currentAbility.IsValid)
            {
                return;
            }

            float currentCooldownEnd = currentAbility.CooldownEnd;
            float currentCooldownStart = currentAbility.CooldownStart;
            float currentRemaining = currentCooldownEnd - currentCooldownStart;
            if (currentRemaining < 0) currentRemaining = 0;

            if (currentRemaining > 0)
            {
                float waitTime = currentRemaining - 0.5f;
                if (waitTime < 0.1f) waitTime = 0.1f;

                string nextSkill = capturedIsUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();

                var waitPawn = capturedPawn;
                var waitSlot = capturedSlot;
                var waitUpgradeBits = capturedUpgradeBits;
                var waitIsUltimate = capturedIsUltimate;

                int waitMilliseconds = (int)(waitTime * 1000);

                Timer.Once(waitMilliseconds.Milliseconds(), () =>
                {
                    var finalAbility = waitPawn.AbilityComponent?.GetAbilityBySlot(waitSlot);
                    if (finalAbility != null && finalAbility.IsValid)
                    {
                        int finalUpgradeBits = finalAbility.UpgradeBits;
                        ExecuteSwap(waitPawn, waitSlot, nextSkill, finalUpgradeBits);

                        if (_passiveSkills.Contains(nextSkill))
                        {
                            ProcessPassiveSkill(waitPawn, waitSlot, nextSkill, finalUpgradeBits, waitIsUltimate);
                        }
                    }
                    else
                    {
                        ExecuteSwap(waitPawn, waitSlot, nextSkill, waitUpgradeBits);
                        if (_passiveSkills.Contains(nextSkill))
                        {
                            ProcessPassiveSkill(waitPawn, waitSlot, nextSkill, waitUpgradeBits, waitIsUltimate);
                        }
                    }
                });
            }
            else
            {
                string nextSkill = capturedIsUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();

                ExecuteSwap(capturedPawn, capturedSlot, nextSkill, capturedUpgradeBits);

                if (_passiveSkills.Contains(nextSkill))
                {
                    ProcessPassiveSkill(capturedPawn, capturedSlot, nextSkill, capturedUpgradeBits, capturedIsUltimate);
                }
            }
        });

        return HookResult.Continue;
    }

    // ========== 启动/停止 ==========
    [Command("sj2", Description = "启动/停止技能替换模式")]
    public void CmdShuffle2(CCitadelPlayerController caller)
    {
        if (_isActive)
        {
            _isActive = false;
            _sigLuan = false;
            _ultLuan = false;
            foreach (var delay in _passiveDelays.Values)
            {
                delay.DelayTimer?.Cancel();
            }
            _passiveDelays.Clear();
            foreach (var delay in _delayedSkillTimers.Values)
            {
                delay.DelayTimer?.Cancel();
            }
            _delayedSkillTimers.Clear();
            if (caller != null) caller.PrintToConsole("[技能替换] 已停止");
            //CCitadelPlayerController.PrintToConsoleAll("[技能替换] 已停止");
            return;
        }

        ShuffleSigPool();
        ShuffleUltPool();
        _isActive = true;
        if (caller != null) caller.PrintToConsole("[技能替换] 已启动，使用技能后将自动替换");
        //CCitadelPlayerController.PrintToConsoleAll("[技能替换] 已启动");
    }

    // ========== 手动重洗技能池 ==========
    [Command("sj2_shuffle", Description = "手动重洗技能池")]
    public void CmdShufflePool(CCitadelPlayerController caller)
    {
        ShuffleSigPool();
        ShuffleUltPool();
        if (caller != null) caller.PrintToConsole("[技能替换] 技能池已手动重洗");
        //CCitadelPlayerController.PrintToConsoleAll("[技能替换] 技能池已手动重洗");
    }
}
