using DeadworksManaged.Api;
using System.Numerics;

namespace SkillShuffle;

public class SkillShufflePlugin : DeadworksPluginBase
{
    public override string Name => "Skill Shuffle";

    public IHandle? shuffleTimer = null;
    public bool isShuffling = false;

    // ========== 技能库（1-3技能，共享池） ==========
    private readonly List<string> _signatureSkills = new List<string>
    {
        // hero_inferno
        "ability_incendiary_projectile",
        "ability_flame_dash",
        "ability_afterburn",
        // hero_gigawatt
        "citadel_ability_lightning_ball",
        "citadel_ability_static_charge",
        "ability_power_surge",
        // hero_hornet
        "citadel_ability_hornet_chain",
        "citadel_ability_hornet_leap",
        "citadel_ability_hornet_sting",
        // hero_ghost
        "ability_blood_bomb",
        "ability_life_drain",
        "ability_blood_shards",
        // hero_atlas
        "citadel_ability_bull_heal",
        "citadel_ability_bull_charge",
        "citadel_ability_passive_beefy",
        // hero_wraith
        "citadel_ability_card_toss",
        "citadel_ability_projectmind",
        "citadel_ability_wraith_rapidfire",
        // hero_forge
        "citadel_ability_shieldedsentry",
        "citadel_ability_mobile_resupply",
        "citadel_ability_fissure_wall",
        // hero_chrono
        "citadel_ability_chrono_pulse_grenade",
        "citadel_ability_chrono_time_wall",
        "citadel_ability_chrono_kinetic_carbine",
        // hero_dynamo
        "citadel_ability_stomp",
        "citadel_ability_void_sphere",
        "citadel_ability_nikuman",
        // hero_kelvin
        "ability_ice_grenade",
        "ability_icepath",
        "ability_icebeam",
        // hero_haze
        "ability_sleep_dagger",
        "ability_smoke_bomb",
        "ability_stacking_damage",
        // hero_astro
        "ability_explosive_barrel",
        "ability_bounce_pad",
        "ability_crackshot",
        // hero_bebop
        "citadel_ability_uppercut",
        "citadel_ability_sticky_bomb",
        "citadel_ability_hook",
        // hero_nano
        "ability_nano_clustergrenade",
        "ability_nano_dash",
        "ability_nano_catform",
        // hero_orion
        "ability_charged_shot",
        "ability_power_jump",
        "ability_immobilize_trap",
        // hero_krill
        "ability_intimidate",
        "ability_burrow",
        "ability_throw_sand",
        // hero_shiv
        "citadel_ability_shiv_dagger",
        "citadel_ability_shiv_dash",
        "citadel_ability_shiv_defer_damage",
        // hero_tengu
        "citadel_ability_tengu_urn",
        "citadel_ability_tangotether",
        "citadel_ability_tengu_stone_form",
        // hero_warden
        "ability_warden_crowd_control",
        "ability_warden_high_alert",
        "ability_warden_lock_down",
        // hero_yamato
        "citadel_ability_power_slash",
        "citadel_ability_flying_strike",
        "citadel_ability_healing_slash",
        // hero_lash
        "citadel_ability_lash_down_strike",
        "citadel_ability_lash",
        "ability_lash_flog",
        // hero_viscous
        "viscous_goo_grenade",
        "viscous_restorative_goo",
        "viscous_telepunch",
        // hero_viper
        "ability_viper_debuffdagger",
        "ability_viper_venom",
        "ability_viper_snakedash",
        // hero_magician
        "ability_magician_magicbolt",
        "ability_magician_cloneturret",
        "ability_magician_animalhexarea",
        // hero_vampirebat
        "ability_vampirebat_steallife",
        "ability_vampirebat_batblink",
        "ability_vampirebat_lovebites",
        // hero_drifter
        "drifter_blood_blast",
        "drifter_shadow_mark",
        "ability_drifter_hunger",
        // hero_priest
        "ability_priest_flashbang",
        "ability_priest_knockback",
        "ability_priest_beartrap",
        // hero_frank
        "ability_frank_shocktarget2",
        "ability_frank_selfzap",
        "ability_frank_painaura",
        // hero_bookworm
        "ability_bookworm_dragonfire",
        "ability_bookworm_knightbarrier",
        "ability_bookworm_aoemagic",
        // hero_doorman
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

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] ========== 插件加载 ==========");
        Console.WriteLine($"[{Name}] 加载状态: {(isReload ? "热重载" : "首次加载")}");
        Console.WriteLine($"[{Name}] ===============================");
        shuffleTimer = null;
        isShuffling = false;
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] 插件卸载");
        shuffleTimer?.Cancel();
        shuffleTimer = null;
        isShuffling = false;
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

    // ========== 获取技能的升级位（使用 SchemaAccessor） ==========
    private int GetSkillUpgradeBits(CBaseEntity ability)
    {
        if (ability == null || !ability.IsValid) return 0;
        return _upgradeBitsAccessor.Get(ability.Handle);
    }

    // ========== 设置技能的升级位（使用 SchemaAccessor） ==========
    private void SetSkillUpgradeBits(CBaseEntity ability, int upgradeBits)
    {
        if (ability == null || !ability.IsValid) return;
        _upgradeBitsAccessor.Set(ability.Handle, upgradeBits);
    }

    // ========== 打乱所有玩家的技能 ==========
    private void ShuffleSkills()
    {
        if (!isShuffling)
        {
            Console.WriteLine($"[{Name}][洗牌] 已停止，跳过");
            return;
        }

        Console.WriteLine($"[{Name}][洗牌] 开始收集所有玩家的技能信息...");

        var allPawns = Players.GetAllPawns().ToList();
        if (allPawns.Count == 0)
        {
            Console.WriteLine($"[{Name}][洗牌] 没有玩家");
            return;
        }

        // ========== 步骤1: 收集所有玩家每个技能的 UpgradeBits ==========
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
                // 只处理 1-4 号技能位
                if (slot < EAbilitySlot.Signature1 || slot > EAbilitySlot.Signature4)
                    continue;

                // 保存 UpgradeBits（使用 SchemaAccessor）
                int upgradeBits = GetSkillUpgradeBits(ability);
                playerSkillInfos.Add((pawn, slot, upgradeBits));
            }
        }

        if (playerSkillInfos.Count == 0)
        {
            Console.WriteLine($"[{Name}][洗牌] 没有找到任何 1-4 技能");
            return;
        }

        Console.WriteLine($"[{Name}][洗牌] 共收集到 {playerSkillInfos.Count} 个技能的升级信息");

        // ========== 步骤2: 分离 1-3 技能和 4 技能 ==========
        var sigInfos = playerSkillInfos.Where(info => info.slot >= EAbilitySlot.Signature1 && info.slot <= EAbilitySlot.Signature3).ToList();
        var ultInfos = playerSkillInfos.Where(info => info.slot == EAbilitySlot.Signature4).ToList();

        Console.WriteLine($"[{Name}][洗牌] 1-3技能槽位数: {sigInfos.Count}, 4技能槽位数: {ultInfos.Count}");

        // ========== 步骤3: 创建打乱后的技能池（确保不重复） ==========
        var random = new Random();

        // 3.1 1-3技能池：从总池中随机抽取，数量等于 sigInfos.Count
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

        // 3.2 4技能池：从总池中随机抽取，数量等于 ultInfos.Count
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

        Console.WriteLine($"[{Name}][洗牌] 已生成 {selectedSigSkills.Count} 个1-3技能和 {selectedUltSkills.Count} 个4技能");

        // ========== 步骤4: 分配技能并恢复 UpgradeBits ==========
        Console.WriteLine($"[{Name}][洗牌] 开始分配技能...");

        // 4.1 分配 1-3 技能
        for (int i = 0; i < sigInfos.Count; i++)
        {
            var info = sigInfos[i];
            var newSkillName = selectedSigSkills[i];

            var controller = GetControllerFromPawn(info.pawn);
            var playerName = controller?.PlayerName ?? "Unknown";

            // 先移除该槽位的旧技能
            var oldAbility = info.pawn.AbilityComponent?.Abilities
                .FirstOrDefault(a => a != null && a.AbilitySlot == info.slot);
            
            if (oldAbility != null && oldAbility.IsValid)
            {
                var oldName = oldAbility.AbilityName;
                if (oldName == newSkillName)
                {
                    Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 的 {oldName} 保持不变");
                    SetSkillUpgradeBits(oldAbility, info.upgradeBits);
                    continue;
                }

                Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 的 {oldName} -> {newSkillName}");
                info.pawn.RemoveAbility(oldName);
            }

            var newAbility = info.pawn.AddAbility(newSkillName, (ushort)info.slot);
            if (newAbility != null)
            {
                SetSkillUpgradeBits(newAbility, info.upgradeBits);
                Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 成功获得 {newSkillName}，升级位: {info.upgradeBits}");
            }
            else
            {
                Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 添加 {newSkillName} 失败");
            }
        }

        // 4.2 分配 4 技能
        for (int i = 0; i < ultInfos.Count; i++)
        {
            var info = ultInfos[i];
            var newSkillName = selectedUltSkills[i];

            var controller = GetControllerFromPawn(info.pawn);
            var playerName = controller?.PlayerName ?? "Unknown";

            var oldAbility = info.pawn.AbilityComponent?.Abilities
                .FirstOrDefault(a => a != null && a.AbilitySlot == info.slot);
            
            if (oldAbility != null && oldAbility.IsValid)
            {
                var oldName = oldAbility.AbilityName;
                if (oldName == newSkillName)
                {
                    Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 的 {oldName} 保持不变");
                    SetSkillUpgradeBits(oldAbility, info.upgradeBits);
                    continue;
                }

                Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 的 {oldName} -> {newSkillName}");
                info.pawn.RemoveAbility(oldName);
            }

            var newAbility = info.pawn.AddAbility(newSkillName, (ushort)info.slot);
            if (newAbility != null)
            {
                SetSkillUpgradeBits(newAbility, info.upgradeBits);
                Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 成功获得 {newSkillName}，升级位: {info.upgradeBits}");
            }
            else
            {
                Console.WriteLine($"[{Name}][洗牌] {playerName} 槽位 {info.slot} 添加 {newSkillName} 失败");
            }
        }

        Console.WriteLine($"[{Name}][洗牌] 技能洗牌完成！");
    }

    // ========== 启动技能洗牌 ==========
    [Command("sj", Description = "启动/停止技能洗牌（每1秒刷新）")]
    public void CmdShuffle(CCitadelPlayerController caller)
    {
        Console.WriteLine($"[{Name}] ========== 技能洗牌命令触发 ==========");
        Console.WriteLine($"[{Name}] 执行者: {(caller != null ? caller.PlayerName : "null")}");

        if (isShuffling)
        {
            Console.WriteLine($"[{Name}] 停止技能洗牌...");
            isShuffling = false;
            shuffleTimer?.Cancel();
            shuffleTimer = null;
            Console.WriteLine($"[{Name}] 技能洗牌已停止");
            if (caller != null)
            {
                caller.PrintToConsole("技能洗牌已停止");
            }
            return;
        }

        Console.WriteLine($"[{Name}] 启动技能洗牌...");
        isShuffling = true;

        ShuffleSkills();

        shuffleTimer = Timer.Every(1.Seconds(), () =>
        {
            ShuffleSkills();
        });

        Console.WriteLine($"[{Name}] 技能洗牌已启动（每1秒刷新）");
        if (caller != null)
        {
            caller.PrintToConsole("技能洗牌已启动（每1秒刷新）");
        }
    }

    // ========== 手动执行一次洗牌 ==========
    [Command("sj_once", Description = "手动执行一次技能洗牌")]
    public void CmdShuffleOnce(CCitadelPlayerController caller)
    {
        Console.WriteLine($"[{Name}] ========== 手动洗牌命令触发 ==========");
        Console.WriteLine($"[{Name}] 执行者: {(caller != null ? caller.PlayerName : "null")}");

        if (!isShuffling)
        {
            isShuffling = true;
            ShuffleSkills();
            isShuffling = false;
            Console.WriteLine($"[{Name}] 手动洗牌完成");
            if (caller != null)
            {
                caller.PrintToConsole("手动洗牌完成");
            }
        }
        else
        {
            Console.WriteLine($"[{Name}] 洗牌正在运行中，请使用 !sj 停止后再手动执行");
            if (caller != null)
            {
                caller.PrintToConsole("洗牌正在运行中，请使用 !sj 停止");
            }
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
        Console.WriteLine($"[{Name}] ========== 换图命令触发 ==========");
        Console.WriteLine($"[{Name}] 执行者: {(caller != null ? caller.PlayerName : "null")}");
        Console.WriteLine($"[{Name}] 执行换图命令，目标地图: dl_mid");
        try
        {
            Server.ExecuteCommand("changelevel dl_mid");
            Console.WriteLine($"[{Name}] 换图命令已发送。");
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
        Console.WriteLine($"[{Name}] ========== 暂停命令触发 ==========");
        Console.WriteLine($"[{Name}] 执行者: {(caller != null ? caller.PlayerName : "null")}");
        Console.WriteLine($"[{Name}] 执行暂停/恢复命令");
        try
        {
            Server.ExecuteCommand("citadel_pause");
            Console.WriteLine($"[{Name}] 暂停/恢复命令已发送。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{Name}] 执行暂停/恢复命令失败: {ex.Message}");
        }
    }
}
