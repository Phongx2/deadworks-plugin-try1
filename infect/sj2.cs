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
       // "ability_magician_copyult",
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
        "ability_crackshot",
        "ability_vampirebat_lovebites",
        "citadel_ability_tangotether",
        "ability_drifter_hunger"
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

    private readonly Dictionary<(CCitadelPlayerPawn, EAbilitySlot), PassiveDelayInfo> _passiveDelays = new();

    public override void OnLoad(bool isReload)
    {
        //Console.WriteLine($"[{Name}] ========== 插件加载 ==========");
        //Console.WriteLine($"[{Name}] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        //Console.WriteLine($"[{Name}] ===============================");
        _isActive = false;
        _sigLuan = false;
        _ultLuan = false;
        _sigIndex = 0;
        _ultIndex = 0;
        _shuffledSigQueue.Clear();
        _shuffledUltQueue.Clear();
        _passiveDelays.Clear();
        CCitadelPlayerController.PrintToConsoleAll("[技能替换] 插件已加载，使用 !sj2 启动");
    }

    public override void OnUnload()
    {
        //Console.WriteLine($"[{Name}] 插件卸载");
        _isActive = false;
        foreach (var delay in _passiveDelays.Values)
        {
            delay.DelayTimer?.Cancel();
        }
        _passiveDelays.Clear();
        CCitadelPlayerController.PrintToConsoleAll("[技能替换] 插件已卸载");
    }

    private void ShuffleSigPool()
    {
        var random = new Random();
        _shuffledSigQueue = _signatureSkills.OrderBy(x => random.Next()).ToList();
        _sigIndex = 0;
        _sigLuan = true;
        //Console.WriteLine($"[{Name}] 1-2技能池已打乱，共 {_shuffledSigQueue.Count} 个技能");
        CCitadelPlayerController.PrintToConsoleAll($"[技能替换] 1-2技能池已打乱，共 {_shuffledSigQueue.Count} 个技能");
    }

    private void ShuffleUltPool()
    {
        var random = new Random();
        _shuffledUltQueue = _ultimateSkills.OrderBy(x => random.Next()).ToList();
        _ultIndex = 0;
        _ultLuan = true;
        //Console.WriteLine($"[{Name}] 4技能池已打乱，共 {_shuffledUltQueue.Count} 个技能");
        CCitadelPlayerController.PrintToConsoleAll($"[技能替换] 4技能池已打乱，共 {_shuffledUltQueue.Count} 个技能");
    }

    private string GetNextSignatureSkill()
    {
        if (_sigIndex >= _shuffledSigQueue.Count - 1)
        {
            //Console.WriteLine($"[{Name}] 1-2技能池即将用光，当前索引 {_sigIndex}/{_shuffledSigQueue.Count}");
            var skill = _shuffledSigQueue[_sigIndex];
            _sigIndex++;
            Timer.Once(8.Ticks(), () =>
            {
                //Console.WriteLine($"[{Name}] 8 tick 已到，重新打乱 1-2 技能池");
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
            //Console.WriteLine($"[{Name}] 4技能池即将用光，当前索引 {_ultIndex}/{_shuffledUltQueue.Count}");
            var skill = _shuffledUltQueue[_ultIndex];
            _ultIndex++;
            Timer.Once(8.Ticks(), () =>
            {
                //Console.WriteLine($"[{Name}] 8 tick 已到，重新打乱 4 技能池");
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

        //Console.WriteLine($"[{Name}] 替换 {playerName} 槽位 {slot}: {oldName} -> {newSkillName}");
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
                    //Console.WriteLine($"[{Name}] 已恢复升级位: {capturedUpgradeBits}");
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
        var playerName = controller?.PlayerName ?? "Unknown";
        //Console.WriteLine($"[{Name}] {playerName} 被动技能 {passiveSkillName} 将在 10 秒后替换为 {nextSkill}");
        controller?.PrintToConsole($"[技能替换] 被动技能将在 10 秒后替换");

        var delayInfo = new PassiveDelayInfo(pawn, slot, passiveSkillName, upgradeBits, isUltimate);

        delayInfo.DelayTimer = Timer.Once(10.Seconds(), () =>
        {
            //Console.WriteLine($"[{Name}] 10秒已到，替换被动技能 {passiveSkillName} -> {nextSkill}");
            controller?.PrintToConsole($"[技能替换] 被动技能已替换");

            var currentAbility = pawn.AbilityComponent?.GetAbilityBySlot(slot);
            int currentUpgradeBits = currentAbility != null && currentAbility.IsValid ? currentAbility.UpgradeBits : upgradeBits;

            ExecuteSwap(pawn, slot, nextSkill, currentUpgradeBits);
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
            //Console.WriteLine($"[{Name}] 技能池未初始化，请先执行 !sj2");
            return HookResult.Continue;
        }

        //Console.WriteLine($"[{Name}] [DEBUG] player_used_ability 事件触发");

        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null)
        {
            //Console.WriteLine($"[{Name}] [DEBUG] 无法获取施法者 Pawn");
            return HookResult.Continue;
        }

        string abilityName = ev.GetString("abilityname", "");
        //Console.WriteLine($"[{Name}] [DEBUG] 技能名称: {abilityName}");

        if (string.IsNullOrEmpty(abilityName))
        {
            //Console.WriteLine($"[{Name}] [DEBUG] 技能名称为空，跳过");
            return HookResult.Continue;
        }

        var abilities = pawn.AbilityComponent?.Abilities;
        if (abilities == null)
        {
            //Console.WriteLine($"[{Name}] [DEBUG] 无法获取技能列表");
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
                //Console.WriteLine($"[{Name}] [DEBUG] 找到目标技能，槽位: {slot}");
                break;
            }
        }

        // 修改为只检测 Signature1 和 Signature2
        if (slot != EAbilitySlot.Signature1 && slot != EAbilitySlot.Signature2 && slot != EAbilitySlot.Signature4)
        {
            //Console.WriteLine($"[{Name}] [DEBUG] 技能槽位 {slot} 不在 1,2,4 范围内，跳过");
            return HookResult.Continue;
        }

        if (targetAbility == null)
        {
            //Console.WriteLine($"[{Name}] [DEBUG] 目标技能为空，跳过");
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

        //Console.WriteLine($"[{Name}] 玩家 {playerName} 使用了 {abilityName} (槽位 {slot})，冷却: {remainingCooldown} 秒");

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
                //Console.WriteLine($"[{Name}] 技能已被移除，跳过替换");
                return;
            }

            float currentCooldownEnd = currentAbility.CooldownEnd;
            float currentCooldownStart = currentAbility.CooldownStart;
            float currentRemaining = currentCooldownEnd - currentCooldownStart;
            if (currentRemaining < 0) currentRemaining = 0;

            //Console.WriteLine($"[{Name}] 8 tick 后，{capturedAbilityName} 剩余冷却: {currentRemaining} 秒");

            if (currentRemaining > 0)
            {
                float waitTime = currentRemaining - 0.5f;
                if (waitTime < 0.1f) waitTime = 0.1f;

                //Console.WriteLine($"[{Name}] 等待 {(int)(waitTime * 1000)} 毫秒后替换技能");

                string nextSkill = capturedIsUltimate ? GetNextUltimateSkill() : GetNextSignatureSkill();

                var waitPawn = capturedPawn;
                var waitSlot = capturedSlot;
                var waitUpgradeBits = capturedUpgradeBits;
                var waitIsUltimate = capturedIsUltimate;

                // ========== 使用 Milliseconds ==========
                int waitMilliseconds = (int)(waitTime * 1000);

                Timer.Once(waitMilliseconds.Milliseconds(), () =>
                {
                    //Console.WriteLine($"[{Name}] 等待结束，执行替换");
                    var finalAbility = waitPawn.AbilityComponent?.GetAbilityBySlot(waitSlot);
                    if (finalAbility != null && finalAbility.IsValid)
                    {
                        float finalRemaining = finalAbility.CooldownEnd - finalAbility.CooldownStart;
                        if (finalRemaining > 0)
                        {
                            //Console.WriteLine($"[{Name}] 技能仍在冷却中 ({finalRemaining} 秒)，强制替换");
                        }
                        int finalUpgradeBits = finalAbility.UpgradeBits;
                        ExecuteSwap(waitPawn, waitSlot, nextSkill, finalUpgradeBits);

                        if (_passiveSkills.Contains(nextSkill))
                        {
                            //Console.WriteLine($"[{Name}] 新技能 {nextSkill} 是被动技能，启动 10 秒延迟替换");
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
                //Console.WriteLine($"[{Name}] 技能已就绪，立即替换为 {nextSkill}");

                ExecuteSwap(capturedPawn, capturedSlot, nextSkill, capturedUpgradeBits);

                if (_passiveSkills.Contains(nextSkill))
                {
                    //Console.WriteLine($"[{Name}] 新技能 {nextSkill} 是被动技能，启动 10 秒延迟替换");
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
            //Console.WriteLine($"[{Name}] 已停止");
            if (caller != null) caller.PrintToConsole("[技能替换] 已停止");
            CCitadelPlayerController.PrintToConsoleAll("[技能替换] 已停止");
            return;
        }

        ShuffleSigPool();
        ShuffleUltPool();
        _isActive = true;
        //Console.WriteLine($"[{Name}] 已启动");
        if (caller != null) caller.PrintToConsole("[技能替换] 已启动，使用技能后将自动替换");
        CCitadelPlayerController.PrintToConsoleAll("[技能替换] 已启动");
    }

    // ========== 手动重洗技能池 ==========
    [Command("sj2_shuffle", Description = "手动重洗技能池")]
    public void CmdShufflePool(CCitadelPlayerController caller)
    {
        ShuffleSigPool();
        ShuffleUltPool();
        //Console.WriteLine($"[{Name}] 技能池已手动重洗");
        if (caller != null) caller.PrintToConsole("[技能替换] 技能池已手动重洗");
        CCitadelPlayerController.PrintToConsoleAll("[技能替换] 技能池已手动重洗");
    }
}
