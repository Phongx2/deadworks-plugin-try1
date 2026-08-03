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
        "ability_incendiary_projectile", "ability_flame_dash", "ability_afterburn",
        "citadel_ability_lightning_ball", "citadel_ability_static_charge", "ability_power_surge",
        "citadel_ability_hornet_chain", "citadel_ability_hornet_leap", "citadel_ability_hornet_sting",
        "ability_blood_bomb", "ability_life_drain", "ability_blood_shards",
        "citadel_ability_bull_heal", "citadel_ability_bull_charge", "citadel_ability_passive_beefy",
        "citadel_ability_card_toss", "citadel_ability_projectmind", "citadel_ability_wraith_rapidfire",
        "citadel_ability_shieldedsentry", "citadel_ability_mobile_resupply", "citadel_ability_fissure_wall",
        "citadel_ability_chrono_pulse_grenade", "citadel_ability_chrono_time_wall", "citadel_ability_chrono_kinetic_carbine",
        "citadel_ability_stomp", "citadel_ability_void_sphere", "citadel_ability_nikuman",
        "ability_ice_grenade", "ability_icepath", "ability_icebeam",
        "ability_sleep_dagger", "ability_smoke_bomb", "ability_stacking_damage",
        "ability_explosive_barrel", "ability_bounce_pad", "ability_crackshot",
        "citadel_ability_uppercut", "citadel_ability_sticky_bomb", "citadel_ability_hook",
        "ability_nano_clustergrenade", "ability_nano_dash", "ability_nano_catform",
        "ability_charged_shot", "ability_power_jump", "ability_immobilize_trap",
        "ability_intimidate", "ability_burrow", "ability_throw_sand",
        "citadel_ability_shiv_dagger", "citadel_ability_shiv_dash", "citadel_ability_shiv_defer_damage",
        "citadel_ability_tengu_urn", "citadel_ability_tangotether", "citadel_ability_tengu_stone_form",
        "ability_warden_crowd_control", "ability_warden_high_alert", "ability_warden_lock_down",
        "citadel_ability_power_slash", "citadel_ability_flying_strike", "citadel_ability_healing_slash",
        "citadel_ability_lash_down_strike", "citadel_ability_lash", "ability_lash_flog",
        "viscous_goo_grenade", "viscous_restorative_goo", "viscous_telepunch",
        "ability_viper_debuffdagger", "ability_viper_venom", "ability_viper_snakedash",
        "ability_magician_magicbolt", "ability_magician_cloneturret", "ability_magician_animalhexarea",
        "ability_vampirebat_steallife", "ability_vampirebat_batblink", "ability_vampirebat_lovebites",
        "drifter_blood_blast", "drifter_shadow_mark", "ability_drifter_hunger",
        "ability_priest_flashbang", "ability_priest_knockback", "ability_priest_beartrap",
        "ability_frank_shocktarget2", "ability_frank_selfzap", "ability_frank_painaura",
        "ability_bookworm_dragonfire", "ability_bookworm_knightbarrier", "ability_bookworm_aoemagic",
        "ability_doorman_bomb", "ability_doorman_doorway", "ability_doorman_luggage_cart"
    };

    private readonly List<string> _ultimateSkills = new List<string>
    {
        "ability_fire_bomb", "citadel_ability_storm_cloud", "citadel_ability_hornet_snipe",
        "ability_health_swap", "citadel_ability_bull_leap", "citadel_ability_psychic_lift",
        "citadel_ability_rocket_barrage", "citadel_ability_chrono_swap", "citadel_ability_self_vacuum",
        "ability_ice_dome", "ability_bullet_flurry", "ability_gravity_lasso",
        "citadel_ability_bebop_laser_beam", "ability_nano_shadow_pulse", "ability_guided_arrow",
        "ability_ult_combo", "citadel_ability_shiv_killing_blow", "citadel_ability_tengu_airlift",
        "ability_warden_riot_protocol", "citadel_ability_infinity_slash", "citadel_ability_lash_ultimate",
        "viscous_goo_bowling_ball", "ability_viper_petrifybola", "ability_magician_copyult",
        "ability_vampirebat_batswarm", "drifter_darkness", "ability_priest_weaponswap",
        "ability_frank_revive", "ability_bookworm_knightcharge", "ability_doorman_hotel"
    };

    // ========== 预计算缓存 ==========
    private List<(int pawnHandle, EAbilitySlot slot, int upgradeBits, string newSkillName)>? _pendingResults;
    private int _applyIndex = 0;
    private int _batchSize = 8;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        shuffleTimer = null;
        isShuffling = false;
        isApplying = false;
        _pendingResults = null;
        _applyIndex = 0;
    }

    public override void OnUnload()
    {
        shuffleTimer?.Cancel();
        shuffleTimer = null;
        isShuffling = false;
        isApplying = false;
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
        return ability.UpgradeBits;
    }

    private void SetSkillUpgradeBits(CBaseEntity ability, int upgradeBits)
    {
        if (ability == null || !ability.IsValid) return;
        ability.UpgradeBits = upgradeBits;
    }

    // ========== 分步应用技能 ==========
    private void ApplyShuffleStep()
    {
        if (_pendingResults == null || _applyIndex >= _pendingResults.Count)
        {
            _pendingResults = null;
            _applyIndex = 0;
            isApplying = false;
            return;
        }

        isApplying = true;
        int processed = 0;

        while (_applyIndex < _pendingResults.Count && processed < _batchSize)
        {
            var (pawnHandle, slot, upgradeBits, newSkillName) = _pendingResults[_applyIndex];
            var pawn = CBaseEntity.FromHandle<CCitadelPlayerPawn>((uint)pawnHandle);

            if (pawn != null && pawn.IsValid)
            {
                var oldAbility = pawn.AbilityComponent?.Abilities
                    .FirstOrDefault(a => a != null && a.AbilitySlot == slot);

                if (oldAbility != null && oldAbility.IsValid)
                {
                    if (oldAbility.AbilityName != newSkillName)
                    {
                        pawn.RemoveAbility(oldAbility.AbilityName);
                        var newAbility = pawn.AddAbility(newSkillName, (ushort)slot);
                        if (newAbility != null)
                        {
                            SetSkillUpgradeBits(newAbility, upgradeBits);
                        }
                    }
                    else
                    {
                        SetSkillUpgradeBits(oldAbility, upgradeBits);
                    }
                }
            }

            _applyIndex++;
            processed++;
        }

        if (_applyIndex >= _pendingResults.Count)
        {
            _pendingResults = null;
            _applyIndex = 0;
            isApplying = false;
        }
        else
        {
            Timer.NextTick(() => ApplyShuffleStep());
        }
    }

    // ========== 优化后的洗牌逻辑 ==========
    private void ShuffleSkills()
    {
        if (!isShuffling || isApplying) return;

        var allPawns = Players.GetAllPawns().ToList();
        if (allPawns.Count == 0) return;

        // 收集技能信息
        var sigInfos = new List<(CCitadelPlayerPawn pawn, EAbilitySlot slot, int upgradeBits)>();
        var ultInfos = new List<(CCitadelPlayerPawn pawn, EAbilitySlot slot, int upgradeBits)>();

        foreach (var pawn in allPawns)
        {
            if (pawn == null || !pawn.IsValid) continue;
            var abilities = pawn.AbilityComponent?.Abilities;
            if (abilities == null) continue;

            foreach (var ability in abilities)
            {
                if (ability == null) continue;
                var slot = ability.AbilitySlot;
                if (slot < EAbilitySlot.Signature1 || slot > EAbilitySlot.Signature4) continue;

                int upgradeBits = GetSkillUpgradeBits(ability);
                if (slot >= EAbilitySlot.Signature1 && slot <= EAbilitySlot.Signature3)
                    sigInfos.Add((pawn, slot, upgradeBits));
                else if (slot == EAbilitySlot.Signature4)
                    ultInfos.Add((pawn, slot, upgradeBits));
            }
        }

        if (sigInfos.Count == 0 && ultInfos.Count == 0) return;

        var random = new Random();

        // 生成 1-3 技能池
        var selectedSigSkills = GenerateShuffledPool(_signatureSkills, sigInfos.Count, random);

        // 生成 4 技能池
        var selectedUltSkills = GenerateShuffledPool(_ultimateSkills, ultInfos.Count, random);

        // 构建结果缓存（使用 pawn 的 EntityIndex 作为句柄）
        var results = new List<(int, EAbilitySlot, int, string)>();

        for (int i = 0; i < sigInfos.Count; i++)
        {
            results.Add((sigInfos[i].pawn.EntityIndex, sigInfos[i].slot, sigInfos[i].upgradeBits, selectedSigSkills[i]));
        }

        for (int i = 0; i < ultInfos.Count; i++)
        {
            results.Add((ultInfos[i].pawn.EntityIndex, ultInfos[i].slot, ultInfos[i].upgradeBits, selectedUltSkills[i]));
        }

        // 清空旧缓存，启动分步应用
        _pendingResults = results;
        _applyIndex = 0;
        isApplying = false;

        Timer.NextTick(() => ApplyShuffleStep());
    }

    // ========== 生成打乱的技能池 ==========
    private List<string> GenerateShuffledPool(List<string> pool, int count, Random random)
    {
        if (count == 0) return new List<string>();

        var shuffled = pool.OrderBy(x => random.Next()).ToList();
        var result = new List<string>();

        while (result.Count < count)
        {
            var remaining = count - result.Count;
            var take = Math.Min(remaining, shuffled.Count);
            result.AddRange(shuffled.Take(take));
            if (result.Count < count)
            {
                shuffled = pool.OrderBy(x => random.Next()).ToList();
            }
        }

        return result;
    }

    // ========== 启动技能洗牌 ==========
    [Command("sj", Description = "启动/停止技能洗牌（每1秒刷新）")]
    public void CmdShuffle(CCitadelPlayerController caller)
    {
        if (isShuffling)
        {
            isShuffling = false;
            shuffleTimer?.Cancel();
            shuffleTimer = null;
            _pendingResults = null;
            _applyIndex = 0;
            isApplying = false;
            if (caller != null) caller.PrintToConsole("技能洗牌已停止");
            return;
        }

        isShuffling = true;
        ShuffleSkills();

        shuffleTimer = Timer.Every(5.Seconds(), () =>
        {
            if (isShuffling) ShuffleSkills();
        });

        if (caller != null) caller.PrintToConsole("技能洗牌已启动（每1秒刷新）");
    }

    // ========== 手动执行一次洗牌 ==========
    [Command("sj_once", Description = "手动执行一次技能洗牌（5秒后执行）")]
public void CmdShuffleOnce(CCitadelPlayerController caller)
{
    Console.WriteLine($"[{Name}] 手动洗牌命令触发，5秒后执行");

    Timer.Once(5.Seconds(), () =>
    {
        if (isApplying)
        {
            if (caller != null) caller.PrintToConsole("正在应用中，请稍后");
            return;  // 这个 return 只结束 Timer 的回调，不是结束 CmdShuffleOnce
        }

        if (!isShuffling)
        {
            isShuffling = true;
            ShuffleSkills();
            isShuffling = false;
            if (caller != null) caller.PrintToConsole("手动洗牌完成");
        }
        else
        {
            if (caller != null) caller.PrintToConsole("洗牌正在运行中，请使用 !sj 停止");
        }
    });
}

    // ========== /r 命令：执行换图 ==========
    [Command("r", Description = "换图到 dl_mid", ServerOnly = true, ConsoleOnly = true, SuppressChat = true)]
    public void CmdChangeLevel(CCitadelPlayerController? caller)
    {
        try { Server.ExecuteCommand("changelevel dl_mid"); }
        catch (Exception ex) { Console.WriteLine($"[{Name}] 执行换图命令失败: {ex.Message}"); }
    }

    // ========== /p 命令：暂停/恢复游戏 ==========
    [Command("p", Description = "暂停/恢复游戏", ServerOnly = true, ConsoleOnly = true, SuppressChat = true)]
    public void CmdPauseGame(CCitadelPlayerController? caller)
    {
        try { Server.ExecuteCommand("citadel_pause"); }
        catch (Exception ex) { Console.WriteLine($"[{Name}] 执行暂停/恢复命令失败: {ex.Message}"); }
    }
}
