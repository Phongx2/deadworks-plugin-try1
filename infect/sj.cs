using DeadworksManaged.Api;
using System.Numerics;

namespace SkillShuffle;

public class SkillShufflePlugin : DeadworksPluginBase
{
    public override string Name => "Skill Shuffle";

    public IHandle? shuffleTimer = null;
    public bool isShuffling = false;
    public bool isApplying = false;

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

    // ========== SchemaAccessor for UpgradeBits ==========
    private static readonly SchemaAccessor<int> _upgradeBitsAccessor =
        new("CCitadelAbility"u8, "m_nUpgradeBits"u8);

    // ========== 打乱后的技能队列 ==========
    private List<string> _shuffledSigQueue = new List<string>();
    private List<string> _shuffledUltQueue = new List<string>();
    private int _sigIndex = 0;
    private int _ultIndex = 0;
    private bool _isPoolShuffled = false;  // 标记技能池是否已被打乱过

    // ========== 待应用的技能替换队列 ==========
    private Queue<(CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName)> _applyQueue = new Queue<(CCitadelPlayerPawn, EAbilitySlot, string)>();

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] ========== 插件加载 ==========");
        Console.WriteLine($"[{Name}] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        Console.WriteLine($"[{Name}] ===============================");
        shuffleTimer = null;
        isShuffling = false;
        isApplying = false;
        _applyQueue.Clear();
        _sigIndex = 0;
        _ultIndex = 0;
        _shuffledSigQueue.Clear();
        _shuffledUltQueue.Clear();
        _isPoolShuffled = false;
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] 插件卸载");
        shuffleTimer?.Cancel();
        shuffleTimer = null;
        isShuffling = false;
        isApplying = false;
        _applyQueue.Clear();
        _shuffledSigQueue.Clear();
        _shuffledUltQueue.Clear();
        _isPoolShuffled = false;
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

    private int GetSkillUpgradeBits(CBaseEntity ability)
    {
        if (ability == null || !ability.IsValid) return 0;
        return _upgradeBitsAccessor.Get(ability.Handle);
    }

    private void SetSkillUpgradeBits(CBaseEntity ability, int upgradeBits)
    {
        if (ability == null || !ability.IsValid) return;
        _upgradeBitsAccessor.Set(ability.Handle, upgradeBits);
    }

    private void ShufflePools()
    {
        var random = new Random();
        _shuffledSigQueue = _signatureSkills.OrderBy(x => random.Next()).ToList();
        _shuffledUltQueue = _ultimateSkills.OrderBy(x => random.Next()).ToList();
        _sigIndex = 0;
        _ultIndex = 0;
        _isPoolShuffled = true;
        Console.WriteLine($"[{Name}] 技能池已打乱");
    }

    private string GetNextSignatureSkill()
    {
        if (_sigIndex >= _shuffledSigQueue.Count)
        {
            ShufflePools();
        }
        return _shuffledSigQueue[_sigIndex++];
    }

    private string GetNextUltimateSkill()
    {
        if (_ultIndex >= _shuffledUltQueue.Count)
        {
            ShufflePools();
        }
        return _shuffledUltQueue[_ultIndex++];
    }

    private void BuildApplyQueue()
    {
        _applyQueue.Clear();

        var allPawns = Players.GetAllPawns().ToList();
        if (allPawns.Count == 0) return;

        foreach (var pawn in allPawns)
        {
            if (pawn == null || !pawn.IsValid) continue;

            _applyQueue.Enqueue((pawn, EAbilitySlot.Signature1, GetNextSignatureSkill()));
            _applyQueue.Enqueue((pawn, EAbilitySlot.Signature2, GetNextSignatureSkill()));
            _applyQueue.Enqueue((pawn, EAbilitySlot.Signature3, GetNextSignatureSkill()));
            _applyQueue.Enqueue((pawn, EAbilitySlot.Signature4, GetNextUltimateSkill()));
        }
    }

    // ========== 分步应用 ==========
    private enum ApplyStep
    {
        SaveUpgrade,
        RemoveOld,
        AddNew,
        RestoreUpgrade,
        Next
    }

    private (CCitadelPlayerPawn pawn, EAbilitySlot slot, string newSkillName, int upgradeBits, ApplyStep step)? _currentApplyState = null;

    private void ApplyOneSkill()
    {
        if (_currentApplyState == null)
        {
            if (_applyQueue.Count == 0)
            {
                isApplying = false;
                return;
            }

            var (pawn, slot, newSkillName) = _applyQueue.Dequeue();
            if (pawn == null || !pawn.IsValid)
            {
                Timer.NextTick(() => ApplyOneSkill());
                return;
            }

            _currentApplyState = (pawn, slot, newSkillName, 0, ApplyStep.SaveUpgrade);
        }

        var state = _currentApplyState.Value;
        var p = state.pawn;
        var s = state.slot;
        var newName = state.newSkillName;
        var upgradeBits = state.upgradeBits;
        var step = state.step;

        if (!p.IsValid)
        {
            _currentApplyState = null;
            Timer.NextTick(() => ApplyOneSkill());
            return;
        }

        switch (step)
        {
            case ApplyStep.SaveUpgrade:
            {
                var oldAbility = p.AbilityComponent?.Abilities
                    .FirstOrDefault(a => a != null && a.AbilitySlot == s);

                if (oldAbility != null && oldAbility.IsValid)
                {
                    upgradeBits = GetSkillUpgradeBits(oldAbility);
                }
                else
                {
                    upgradeBits = 0;
                }

                _currentApplyState = (p, s, newName, upgradeBits, ApplyStep.RemoveOld);
                Timer.NextTick(() => ApplyOneSkill());
                break;
            }

            case ApplyStep.RemoveOld:
            {
                var oldAbility2 = p.AbilityComponent?.Abilities
                    .FirstOrDefault(a => a != null && a.AbilitySlot == s);

                if (oldAbility2 != null && oldAbility2.IsValid)
                {
                    var oldName = oldAbility2.AbilityName;
                    if (oldName != newName)
                    {
                        p.RemoveAbility(oldName);
                    }
                    else
                    {
                        _currentApplyState = null;
                        Timer.NextTick(() => ApplyOneSkill());
                        return;
                    }
                }

                _currentApplyState = (p, s, newName, upgradeBits, ApplyStep.AddNew);
                Timer.NextTick(() => ApplyOneSkill());
                break;
            }

            case ApplyStep.AddNew:
            {
                p.AddAbility(newName, (ushort)s);
                _currentApplyState = (p, s, newName, upgradeBits, ApplyStep.RestoreUpgrade);
                Timer.NextTick(() => ApplyOneSkill());
                break;
            }

            case ApplyStep.RestoreUpgrade:
            {
                if (upgradeBits > 0)
                {
                    var newAbility = p.AbilityComponent?.Abilities
                        .FirstOrDefault(a => a != null && a.AbilitySlot == s);
                    if (newAbility != null && newAbility.IsValid)
                    {
                        SetSkillUpgradeBits(newAbility, upgradeBits);
                    }
                }

                _currentApplyState = null;
                Timer.NextTick(() => ApplyOneSkill());
                break;
            }
        }
    }

    private void ExecuteShuffle()
    {
        if (!isShuffling) return;
        if (isApplying) return;

        BuildApplyQueue();

        if (_applyQueue.Count == 0) return;

        isApplying = true;
        _currentApplyState = null;
        Timer.NextTick(() => ApplyOneSkill());
    }

    // ========== 命令 ==========
    [Command("sj", Description = "启动/停止技能洗牌（每5秒刷新）")]
    public void CmdShuffle(CCitadelPlayerController caller)
    {
        if (isShuffling)
        {
            isShuffling = false;
            shuffleTimer?.Cancel();
            shuffleTimer = null;
            _applyQueue.Clear();
            isApplying = false;
            _currentApplyState = null;
            if (caller != null) caller.PrintToConsole("技能洗牌已停止");
            return;
        }

        isShuffling = true;
        if (caller != null) caller.PrintToConsole("技能洗牌已启动（每5秒刷新）");

        // 如果技能池从未被打乱过，先打乱一次
        if (!_isPoolShuffled)
        {
            ShufflePools();
        }

        ExecuteShuffle();

        shuffleTimer = Timer.Every(5.Seconds(), () =>
        {
            ExecuteShuffle();
        });
    }

    [Command("sj_once", Description = "手动执行一次完整洗牌（打乱技能池→替换到最后一个玩家4技能）")]
    public void CmdShuffleOnce(CCitadelPlayerController caller)
    {
        if (isApplying)
        {
            if (caller != null) caller.PrintToConsole("正在应用中，请稍后");
            return;
        }

        // ========== 条件：技能池从未打乱过，或者已经抵达队列末尾 ==========
        bool needShuffle = false;
        if (!_isPoolShuffled)
        {
            needShuffle = true;
            Console.WriteLine($"[{Name}] 技能池从未被打乱过，执行打乱");
        }
        else if (_sigIndex >= _shuffledSigQueue.Count && _ultIndex >= _shuffledUltQueue.Count)
        {
            needShuffle = true;
            Console.WriteLine($"[{Name}] 技能池已耗尽，执行重新打乱");
        }
        else
        {
            Console.WriteLine($"[{Name}] 技能池还有剩余，直接使用当前队列 (sigIndex={_sigIndex}/{_shuffledSigQueue.Count}, ultIndex={_ultIndex}/{_shuffledUltQueue.Count})");
        }

        if (needShuffle)
        {
            ShufflePools();
        }
        // ========== 条件判断结束 ==========

        // 执行完整的洗牌（生成队列并应用到最后一个玩家的4技能）
        if (!isShuffling)
        {
            // 临时开启循环模式，执行一次完整洗牌
            isShuffling = true;
            ExecuteShuffle();
            isShuffling = false;
            if (caller != null) caller.PrintToConsole("手动洗牌完成");
        }
        else
        {
            ExecuteShuffle();
            if (caller != null) caller.PrintToConsole("手动洗牌已开始");
        }
    }

    [Command("r", Description = "换图到 dl_mid", ServerOnly = true, ConsoleOnly = true, SuppressChat = true)]
    public void CmdChangeLevel(CCitadelPlayerController? caller)
    {
        try { Server.ExecuteCommand("changelevel dl_mid"); }
        catch (Exception ex) { Console.WriteLine($"[{Name}] 执行换图命令失败: {ex.Message}"); }
    }

    [Command("p", Description = "暂停/恢复游戏", ServerOnly = true, ConsoleOnly = true, SuppressChat = true)]
    public void CmdPauseGame(CCitadelPlayerController? caller)
    {
        try { Server.ExecuteCommand("citadel_pause"); }
        catch (Exception ex) { Console.WriteLine($"[{Name}] 执行暂停/恢复命令失败: {ex.Message}"); }
    }
}
