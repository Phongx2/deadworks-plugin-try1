using DeadworksManaged.Api;
using System.Numerics;

namespace SkillShuffle;

public class SkillShufflePlugin : DeadworksPluginBase
{
    public override string Name => "Skill Shuffle";

    public IHandle? shuffleTimer = null;
    public bool isShuffling = false;
    public bool isCalculating = false;  // 是否正在计算中

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

    // ========== 技能库（4技能，独立池） ==========
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

    // 预计算的结果缓存
    private List<(CCitadelPlayerPawn pawn, EAbilitySlot slot, int upgradeBits, string newSkillName)>? _pendingSigResults;
    private List<(CCitadelPlayerPawn pawn, EAbilitySlot slot, int upgradeBits, string newSkillName)>? _pendingUltResults;

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] ========== 插件加载 ==========");
        Console.WriteLine($"[{Name}] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        Console.WriteLine($"[{Name}] ===============================");
        shuffleTimer = null;
        isShuffling = false;
        isCalculating = false;
        _pendingSigResults = null;
        _pendingUltResults = null;
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] 插件卸载");
        shuffleTimer?.Cancel();
        shuffleTimer = null;
        isShuffling = false;
        isCalculating = false;
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

    // ========== 获取技能的升级位 ==========
    private int GetSkillUpgradeBits(CBaseEntity ability)
    {
        if (ability == null || !ability.IsValid) return 0;
        return _upgradeBitsAccessor.Get(ability.Handle);
    }

    // ========== 设置技能的升级位 ==========
    private void SetSkillUpgradeBits(CBaseEntity ability, int upgradeBits)
    {
        if (ability == null || !ability.IsValid) return;
        _upgradeBitsAccessor.Set(ability.Handle, upgradeBits);
    }

    // ========== 缓慢计算下一次洗牌结果（5秒内逐步计算） ==========
    private void CalculateNextShuffle()
    {
        if (isCalculating)
        {
            Console.WriteLine($"[{Name}][计算] 正在计算中，请稍后");
            return;
        }

        Console.WriteLine($"[{Name}][计算] 开始计算下一次技能分配（预计5秒）...");
        isCalculating = true;

        var allPawns = Players.GetAllPawns().ToList();
        if (allPawns.Count == 0)
        {
            Console.WriteLine($"[{Name}][计算] 没有玩家");
            isCalculating = false;
            return;
        }

        // 收集所有玩家技能信息
        var playerSkillInfos = new List<(CCitadelPlayerPawn pawn, EAbilitySlot slot, int upgradeBits)>();

        foreach (var pawn in allPawns)
        {
            if (pawn == null || !pawn.IsValid) continue;
            var abilities = pawn.AbilityComponent?.Abilities;
            if (abilities == null) continue;

            foreach (var ability in abilities)
            {
                if (ability == null) continue;
                var slot = ability.AbilitySlot;
                if (slot < EAbilitySlot.Signature1 || slot > EAbilitySlot.Signature4)
                    continue;

                int upgradeBits = GetSkillUpgradeBits(ability);
                playerSkillInfos.Add((pawn, slot, upgradeBits));
            }
        }

        if (playerSkillInfos.Count == 0)
        {
            Console.WriteLine($"[{Name}][计算] 没有找到任何 1-4 技能");
            isCalculating = false;
            return;
        }

        var sigInfos = playerSkillInfos.Where(info => info.slot >= EAbilitySlot.Signature1 && info.slot <= EAbilitySlot.Signature3).ToList();
        var ultInfos = playerSkillInfos.Where(info => info.slot == EAbilitySlot.Signature4).ToList();

        var random = new Random();

        // 预生成打乱后的技能池
        var shuffledSigPool = _signatureSkills.OrderBy(x => random.Next()).ToList();
        var selectedSigSkills = new List<string>();
        while (selectedSigSkills.Count < sigInfos.Count)
        {
            var remaining = sigInfos.Count - selectedSigSkills.Count;
            var take = Math.Min(remaining, shuffledSigPool.Count);
            selectedSigSkills.AddRange(shuffledSigPool.Take(take));
            if (selectedSigSkills.Count < sigInfos.Count)
            {
                shuffledSigPool = _signatureSkills.OrderBy(x => random.Next()).ToList();
            }
        }

        var shuffledUltPool = _ultimateSkills.OrderBy(x => random.Next()).ToList();
        var selectedUltSkills = new List<string>();
        while (selectedUltSkills.Count < ultInfos.Count)
        {
            var remaining = ultInfos.Count - selectedUltSkills.Count;
            var take = Math.Min(remaining, shuffledUltPool.Count);
            selectedUltSkills.AddRange(shuffledUltPool.Take(take));
            if (selectedUltSkills.Count < ultInfos.Count)
            {
                shuffledUltPool = _ultimateSkills.OrderBy(x => random.Next()).ToList();
            }
        }

        // 构建结果缓存
        _pendingSigResults = new List<(CCitadelPlayerPawn, EAbilitySlot, int, string)>();
        for (int i = 0; i < sigInfos.Count; i++)
        {
            var info = sigInfos[i];
            _pendingSigResults.Add((info.pawn, info.slot, info.upgradeBits, selectedSigSkills[i]));
        }

        _pendingUltResults = new List<(CCitadelPlayerPawn, EAbilitySlot, int, string)>();
        for (int i = 0; i < ultInfos.Count; i++)
        {
            var info = ultInfos[i];
            _pendingUltResults.Add((info.pawn, info.slot, info.upgradeBits, selectedUltSkills[i]));
        }

        Console.WriteLine($"[{Name}][计算] 计算完成，将在5秒后应用");
    }

    // ========== 应用预计算的结果 ==========
    private void ApplyShuffleResults()
    {
        if (_pendingSigResults == null && _pendingUltResults == null)
        {
            Console.WriteLine($"[{Name}][应用] 没有预计算的结果");
            return;
        }

        Console.WriteLine($"[{Name}][应用] 开始应用技能分配...");

        // 应用 1-3 技能
        if (_pendingSigResults != null)
        {
            foreach (var (pawn, slot, upgradeBits, newSkillName) in _pendingSigResults)
            {
                if (pawn == null || !pawn.IsValid) continue;

                var controller = GetControllerFromPawn(pawn);
                var playerName = controller?.PlayerName ?? "Unknown";

                var oldAbility = pawn.AbilityComponent?.Abilities
                    .FirstOrDefault(a => a != null && a.AbilitySlot == slot);

                if (oldAbility != null && oldAbility.IsValid)
                {
                    var oldName = oldAbility.AbilityName;
                    if (oldName == newSkillName)
                    {
                        SetSkillUpgradeBits(oldAbility, upgradeBits);
                        continue;
                    }
                    pawn.RemoveAbility(oldName);
                }

                var newAbility = pawn.AddAbility(newSkillName, (ushort)slot);
                if (newAbility != null)
                {
                    SetSkillUpgradeBits(newAbility, upgradeBits);
                }
            }
            _pendingSigResults = null;
        }

        // 应用 4 技能
        if (_pendingUltResults != null)
        {
            foreach (var (pawn, slot, upgradeBits, newSkillName) in _pendingUltResults)
            {
                if (pawn == null || !pawn.IsValid) continue;

                var controller = GetControllerFromPawn(pawn);
                var playerName = controller?.PlayerName ?? "Unknown";

                var oldAbility = pawn.AbilityComponent?.Abilities
                    .FirstOrDefault(a => a != null && a.AbilitySlot == slot);

                if (oldAbility != null && oldAbility.IsValid)
                {
                    var oldName = oldAbility.AbilityName;
                    if (oldName == newSkillName)
                    {
                        SetSkillUpgradeBits(oldAbility, upgradeBits);
                        continue;
                    }
                    pawn.RemoveAbility(oldName);
                }

                var newAbility = pawn.AddAbility(newSkillName, (ushort)slot);
                if (newAbility != null)
                {
                    SetSkillUpgradeBits(newAbility, upgradeBits);
                }
            }
            _pendingUltResults = null;
        }

        Console.WriteLine($"[{Name}][应用] 技能分配完成");
        isCalculating = false;
    }

    // ========== 执行一次完整洗牌（计算 + 5秒后应用） ==========
    private void ExecuteShuffle()
    {
        if (isCalculating)
        {
            Console.WriteLine($"[{Name}] 正在计算中，请稍后");
            return;
        }

        // 开始计算
        CalculateNextShuffle();

        // 5秒后应用结果
        Timer.Once(5.Seconds(), () =>
        {
            ApplyShuffleResults();
            Console.WriteLine($"[{Name}] 本次洗牌完成");
        });
    }

    // ========== 启动技能洗牌 ==========
    [Command("sj", Description = "启动/停止技能洗牌（每5秒刷新）")]
    public void CmdShuffle(CCitadelPlayerController caller)
    {
        if (isShuffling)
        {
            Console.WriteLine($"[{Name}] 停止技能洗牌...");
            isShuffling = false;
            shuffleTimer?.Cancel();
            shuffleTimer = null;
            _pendingSigResults = null;
            _pendingUltResults = null;
            isCalculating = false;
            if (caller != null)
            {
                caller.PrintToConsole("技能洗牌已停止");
            }
            return;
        }

        Console.WriteLine($"[{Name}] 启动技能洗牌...");
        isShuffling = true;

        // 先执行一次
        ExecuteShuffle();

        // 每5秒执行一次
        shuffleTimer = Timer.Every(5.Seconds(), () =>
        {
            if (isShuffling)
            {
                ExecuteShuffle();
            }
        });

        if (caller != null)
        {
            caller.PrintToConsole("技能洗牌已启动（每5秒刷新）");
        }
    }

    // ========== 手动执行一次洗牌 ==========
    [Command("sj_once", Description = "手动执行一次技能洗牌（5秒后应用）")]
    public void CmdShuffleOnce(CCitadelPlayerController caller)
    {
        if (isCalculating)
        {
            Console.WriteLine($"[{Name}] 正在计算中，请稍后");
            if (caller != null)
            {
                caller.PrintToConsole("正在计算中，请稍后");
            }
            return;
        }

        ExecuteShuffle();
        if (caller != null)
        {
            caller.PrintToConsole("技能洗牌已开始计算，5秒后应用");
        }
    }

    // ========== /r 命令：执行换图 ==========
    [Command("r", 
        Description = "换图到 dl_mid", 
        ServerOnly = true, 
        ConsoleOnly = true,
        SuppressChat = true)]
    public void CmdChangeLevel(CCitadelPlayerController? caller)
    {
        try
        {
            Server.ExecuteCommand("changelevel dl_mid");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] 执行换图命令失败: {ex.Message}");
        }
    }

    // ========== /p 命令：暂停/恢复游戏 ==========
    [Command("p", 
        Description = "暂停/恢复游戏", 
        ServerOnly = true, 
        ConsoleOnly = true,
        SuppressChat = true)]
    public void CmdPauseGame(CCitadelPlayerController? caller)
    {
        try
        {
            Server.ExecuteCommand("citadel_pause");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] 执行暂停/恢复命令失败: {ex.Message}");
        }
    }
}
