using DeadworksManaged.Api;
using System.Numerics;

namespace SkillShuffle2;




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
        "ability_familiar_attach"
    };

   // ========== 打乱后的技能队列 ==========
    private List<string> _shuffledSigQueue = new List<string>();
    private int _sigIndex = 0;
    private List<string> _shuffledUltQueue = new List<string>();
    private int _ultIndex = 0;

    // ========== 状态标志 ==========
    private bool _isActive = false;
    private bool _sigLuan = false;
    private bool _ultLuan = false;

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

    private readonly Dictionary<(CCitadelPlayerPawn, EAbilitySlot), PassiveDelayInfo> _passiveDelays = new();

    // ========== 存储等待引导结束的替换任务 ==========
    private class PendingReplaceInfo
    {
        public CCitadelPlayerPawn Pawn;
        public EAbilitySlot Slot;
        public string NextSkill;
        public int UpgradeBits;
        public bool IsUltimate;
        public IHandle? CheckTimer;

        public PendingReplaceInfo(CCitadelPlayerPawn pawn, EAbilitySlot slot, string nextSkill, int upgradeBits, bool isUltimate)
        {
            Pawn = pawn;
            Slot = slot;
            NextSkill = nextSkill;
            UpgradeBits = upgradeBits;
            IsUltimate = isUltimate;
            CheckTimer = null;
        }
    }

    private readonly Dictionary<(CCitadelPlayerPawn, EAbilitySlot), PendingReplaceInfo> _pendingReplaces = new();

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
        _pendingReplaces.Clear();
    }

    public override void OnUnload()
    {
        _isActive = false;
        foreach (var delay in _passiveDelays.Values)
        {
            delay.DelayTimer?.Cancel();
        }
        _passiveDelays.Clear();
        
        foreach (var pending in _pendingReplaces.Values)
        {
            pending.CheckTimer?.Cancel();
        }
        _pendingReplaces.Clear();
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
    }

    private void ShuffleUltPool()
    {
        var random = new Random();
        _shuffledUltQueue = _ultimateSkills.OrderBy(x => random.Next()).ToList();
        _ultIndex = 0;
        _ultLuan = true;
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

    // ========== 检查技能是否在引导状态 ==========
    private bool IsAbilityChanneling(CCitadelPlayerPawn pawn, EAbilitySlot slot)
    {
        if (pawn == null || !pawn.IsValid) return false;
        
        var ability = pawn.AbilityComponent?.GetAbilityBySlot(slot);
        if (ability == null || !ability.IsValid) return false;
        
        var baseAbility = ability as CCitadelBaseAbility;
        if (baseAbility != null)
        {
            return baseAbility.IsChanneling;
        }
        return false;
    }

    // ========== 检查技能是否在延迟替换列表中 ==========
    private bool IsDelayedSkill(string skillName)
    {
        return _delayedSkills.Contains(skillName);
    }

    // ========== 执行技能替换（带引导检查） ==========
    private void ExecuteSwapWithChannelCheck(CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName, int upgradeBits, bool isUltimate)
    {
        var key = (pawn, slot);
        
        // 如果正在引导，等待引导结束
        if (IsAbilityChanneling(pawn, slot))
        {
            var controller = GetControllerFromPawn(pawn);
            controller?.PrintToConsole($"[技能替换] 技能正在引导中，等待引导结束...");
            
            // 取消之前的等待任务
            if (_pendingReplaces.TryGetValue(key, out var existingPending))
            {
                existingPending.CheckTimer?.Cancel();
                _pendingReplaces.Remove(key);
            }
            
            var pendingInfo = new PendingReplaceInfo(pawn, slot, newSkillName, upgradeBits, isUltimate);
            
            // 使用 Timer.Every 每2 tick检查一次引导状态
            // Timer.Every 的回调是 Action 类型，不接受参数
            pendingInfo.CheckTimer = Timer.Every(2.Ticks(), () =>
            {
                // 检查pawn是否有效
                if (pawn == null || !pawn.IsValid)
                {
                    pendingInfo.CheckTimer?.Cancel();
                    _pendingReplaces.Remove(key);
                    return;
                }
                
                // 检查是否还在引导
                if (IsAbilityChanneling(pawn, slot))
                {
                    // 还在引导，继续等待
                    return;
                }
                
                // 引导结束，执行替换
                pendingInfo.CheckTimer?.Cancel();
                _pendingReplaces.Remove(key);
                
                var controller2 = GetControllerFromPawn(pawn);
                controller2?.PrintToConsole($"[技能替换] 引导结束，执行替换");
                
                // 获取当前技能和升级位
                var currentAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
                int currentUpgradeBits = currentAbility != null && currentAbility.IsValid ? currentAbility.UpgradeBits : upgradeBits;
                
                ExecuteSwap(pawn, slot, newSkillName, currentUpgradeBits);
                
                // 如果新技能是被动技能，启动延迟替换
                if (_passiveSkills.Contains(newSkillName))
                {
                    ProcessPassiveSkill(pawn, slot, newSkillName, currentUpgradeBits, isUltimate);
                }
            });
            
            _pendingReplaces[key] = pendingInfo;
            return;
        }
        
        // 不在引导状态，直接执行替换
        DoReplace(pawn, slot, newSkillName, upgradeBits, isUltimate);
    }

    // ========== 执行替换的实际逻辑 ==========
    private void DoReplace(CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName, int upgradeBits, bool isUltimate)
    {
        // 获取当前技能和升级位
        var currentAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
        int currentUpgradeBits = currentAbility != null && currentAbility.IsValid ? currentAbility.UpgradeBits : upgradeBits;
        
        ExecuteSwap(pawn, slot, newSkillName, currentUpgradeBits);
        
        // 如果新技能是被动技能，启动延迟替换
        if (_passiveSkills.Contains(newSkillName))
        {
            ProcessPassiveSkill(pawn, slot, newSkillName, currentUpgradeBits, isUltimate);
        }
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

            // 使用带引导检查的替换
            ExecuteSwapWithChannelCheck(pawn, slot, nextSkill, currentUpgradeBits, isUltimate);
            _passiveDelays.Remove(key);
        });

        _passiveDelays[key] = delayInfo;
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

        // 只检测 Signature1、Signature2 和 Signature4
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

            // 检查是否在引导状态
            if (IsAbilityChanneling(capturedPawn, capturedSlot))
            {
                var controller2 = GetControllerFromPawn(capturedPawn);
                controller2?.PrintToConsole($"[技能替换] 技能正在引导中，等待引导结束后替换");
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
                    
                    // 替换前再次检查引导状态
                    if (IsAbilityChanneling(waitPawn, waitSlot))
                    {
                        var controller3 = GetControllerFromPawn(waitPawn);
                        controller3?.PrintToConsole($"[技能替换] 等待冷却结束时技能仍在引导，继续等待...");
                        // 递归等待引导结束
                        ExecuteSwapWithChannelCheck(waitPawn, waitSlot, nextSkill, waitUpgradeBits, waitIsUltimate);
                        return;
                    }
                    
                    if (finalAbility != null && finalAbility.IsValid)
                    {
                        int finalUpgradeBits = finalAbility.UpgradeBits;
                        // 使用带引导检查的替换
                        ExecuteSwapWithChannelCheck(waitPawn, waitSlot, nextSkill, finalUpgradeBits, waitIsUltimate);
                    }
                    else
                    {
                        ExecuteSwapWithChannelCheck(waitPawn, waitSlot, nextSkill, waitUpgradeBits, waitIsUltimate);
                    }
                });
            }
            else
            {
                string nextSkill = capturedIsUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();
                
                // 使用带引导检查的替换
                ExecuteSwapWithChannelCheck(capturedPawn, capturedSlot, nextSkill, capturedUpgradeBits, capturedIsUltimate);
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
            
            foreach (var pending in _pendingReplaces.Values)
            {
                pending.CheckTimer?.Cancel();
            }
            _pendingReplaces.Clear();
            
            if (caller != null) caller.PrintToConsole("[技能替换] 已停止");
            return;
        }

        ShuffleSigPool();
        ShuffleUltPool();
        _isActive = true;
        if (caller != null) caller.PrintToConsole("[技能替换] 已启动，使用技能后将自动替换");
    }

    // ========== 手动重洗技能池 ==========
    [Command("sj2_shuffle", Description = "手动重洗技能池")]
    public void CmdShufflePool(CCitadelPlayerController caller)
    {
        ShuffleSigPool();
        ShuffleUltPool();
        if (caller != null) caller.PrintToConsole("[技能替换] 技能池已手动重洗");
    }
}
