using DeadworksManaged.Api;
using System.Numerics;

namespace MysteryBoxPlugin;

public class MysteryBoxPlugin : DeadworksPluginBase
{
    public override string Name => "神秘盲盒";

    private static readonly Random _rng = new Random();

    // ========== 装备池 ==========
    private readonly string[] _itemPool = new string[]
    {
       // "upgrade_ancient_shield",
       // "upgrade_haunting_scream",
       //"upgrade_apex_combat",
       // "upgrade_aerial_supremacy",
       "upgrade_omnicharge_pendant",
       // "upgrade_timeless_emblem",
       // "upgrade_shadow_step",
        "upgrade_shrink_ray",
        "upgrade_infinite_rounds",
        "upgrade_icarus_wings",
        "upgrade_mystical_piano",
        "upgrade_nullification_aura",
        "upgrade_celestial_guidance",
        "upgrade_eternal_gift",
        "upgrade_patrons_blessing",
        "upgrade_eldritch_shot",
        "upgrade_cloak_of_opportunity",
        "upgrade_runed_gauntlets",
        "upgrade_electric_slippers",
        "upgrade_prism_blast",
        "upgrade_unstable_concoction",
        "upgrade_shivas_bracelet",
        "upgrade_shadow_strike",
    };

    // ========== 装备中文名称 ==========
    private readonly string[] _itemChinese = new string[]
    {
       // "upgrade_ancient_shield",
       // "upgrade_haunting_scream",
       //"upgrade_apex_combat",
       // "upgrade_aerial_supremacy",
        "灭霸的无限宝石",
       // "upgrade_timeless_emblem",
       // "upgrade_shadow_step",
        "？！小小！？",
        "？！射射！？",
        "超级烤鸡翅",
        "？！控控？！",
        "我让你用技能了么",
        "快看是流星",
        "房主给的礼物",
        "导管时间到",
        "？！弹弹？！",
        "魔法披风",
        "胆汁喷涌虫的最爱",
        "我的滑板鞋",
        "夜店蹦迪",
        "绷绷炸弹",
        "原神星超导加强过后的七七减了E技能15s冷却已经王朝",
        "我很神秘",
    };

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[神秘盲盒] 热重载完成！" : "[神秘盲盒] 已加载！");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[神秘盲盒] 已卸载！");
        _pendingBoxes.Clear();
    }

    // ========== 存储每个玩家正在进行的盲盒状态 ==========
    private class BoxState
    {
        public CCitadelPlayerPawn Pawn;
        public CCitadelPlayerController Controller;
        public IHandle? RotationTimer;
        public IHandle? FinalizeTimer;
        public int CurrentItemIndex;
        public string SlotSuffix;  // 用户输入的 x 字符串

        public BoxState(CCitadelPlayerPawn pawn, CCitadelPlayerController controller, string suffix)
        {
            Pawn = pawn;
            Controller = controller;
            RotationTimer = null;
            FinalizeTimer = null;
            CurrentItemIndex = 0;
            SlotSuffix = suffix;
        }
    }

    private readonly Dictionary<CCitadelPlayerPawn, BoxState> _pendingBoxes = new();

    // ========== 核心盲盒逻辑 ==========
    private void StartMysteryBox(CCitadelPlayerController caller, string suffix)
    {
        if (caller == null) return;

        var pawn = caller.GetHeroPawn();
        if (pawn == null)
        {
            caller.PrintToConsole("[神秘盲盒] 无法获取英雄实体");
            return;
        }

        if (_pendingBoxes.ContainsKey(pawn))
        {
            caller.PrintToConsole("[神秘盲盒] 你已经在开启盲盒了！");
            return;
        }

        int currentGold = pawn.GetCurrency(ECurrencyType.EGold);
        if (currentGold < 3200)
        {
            caller.PrintToConsole($"[神秘盲盒] 金钱不足！需要 3200，当前只有 {currentGold}");
            var msg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "❌ 金钱不足",
                DescriptionLocstring = $"需要 3200 金币，你只有 {currentGold}"
            };
            NetMessages.Send(msg, RecipientFilter.Single(caller.Slot));
            return;
        }

        int newGold = currentGold - 3200;
        pawn.SetCurrency(ECurrencyType.EGold, newGold);
        caller.PrintToConsole($"[神秘盲盒] 已扣除 3200 金币，剩余 {newGold}");

        var startMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🎁 神秘盲盒",
            DescriptionLocstring = "正在打开神秘盲盒……"
        };
        NetMessages.Send(startMsg, RecipientFilter.Single(caller.Slot));

        var state = new BoxState(pawn, caller, suffix);
        _pendingBoxes[pawn] = state;

        state.RotationTimer = Timer.Every(50.Milliseconds(), () =>
        {
            if (!pawn.IsValid)
            {
                CleanupBox(state);
                return;
            }

            if (!string.IsNullOrEmpty(_itemPool[state.CurrentItemIndex]))
            {
                pawn.RemoveItem(_itemPool[state.CurrentItemIndex]);
            }

            state.CurrentItemIndex = _rng.Next(0, _itemPool.Length);
            string itemName = _itemPool[state.CurrentItemIndex];
            pawn.AddItem(itemName, false);
        });

        state.FinalizeTimer = Timer.Once(3000.Milliseconds(), () =>
        {
            if (!pawn.IsValid)
            {
                CleanupBox(state);
                return;
            }

            state.RotationTimer?.Cancel();

            if (!string.IsNullOrEmpty(_itemPool[state.CurrentItemIndex]))
            {
                pawn.RemoveItem(_itemPool[state.CurrentItemIndex]);
            }

            int finalIndex = _rng.Next(0, _itemPool.Length);
            string finalItem = _itemPool[finalIndex];
            string chineseName = _itemChinese[finalIndex];
            bool isEnhanced = _rng.NextDouble() < 0.1;

            // ========== 核心修改：finalItem 拼接用户输入的 suffix ==========
            string finalItemWithSuffix = $"{finalItem} {suffix}";

            if (isEnhanced)
            {
                pawn.AddItem(finalItemWithSuffix, true);
            }
            else
            {
                pawn.AddItem(finalItemWithSuffix, false);
            }

            var resultMsg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "🎉 恭喜获得！",
                DescriptionLocstring = isEnhanced ? $"你获得了强化的 {chineseName} (后缀: {suffix}) ✨" : $"你获得了: {chineseName} (后缀: {suffix})"
            };
            NetMessages.Send(resultMsg, RecipientFilter.Single(caller.Slot));

            caller.PrintToConsole($"[神秘盲盒] 你获得了{(isEnhanced ? "强化的 " : " ")}{chineseName} (后缀: {suffix})");

            CleanupBox(state);
        });
    }

    private void CleanupBox(BoxState state)
    {
        if (state == null) return;

        state.RotationTimer?.Cancel();
        state.FinalizeTimer?.Cancel();

        if (state.Pawn != null && _pendingBoxes.ContainsKey(state.Pawn))
        {
            _pendingBoxes.Remove(state.Pawn);
        }
    }

    // ========== 监听 G 键（Cosmetic1 技能） ==========
    [GameEventHandler("player_used_ability")]
    public HookResult OnPlayerUsedAbility(GameEvent ev)
    {
        var pawn = ev.GetPlayerPawn("player")?.As<CCitadelPlayerPawn>();
        if (pawn == null) return HookResult.Continue;

        string abilityName = ev.GetString("abilityname", "");
        var slot = EAbilitySlot.Invalid;

        var abilities = pawn.AbilityComponent?.Abilities;
        if (abilities != null)
        {
            foreach (var ability in abilities)
            {
                if (ability != null && ability.AbilityName == abilityName)
                {
                    slot = ability.AbilitySlot;
                    break;
                }
            }
        }

        if (slot != EAbilitySlot.Cosmetic1) return HookResult.Continue;

        var controller = GetControllerFromPawn(pawn);
        if (controller == null) return HookResult.Continue;

        Console.WriteLine($"[神秘盲盒] 玩家 {controller.PlayerName} 按下了 G 键 (Cosmetic1)");
        StartMysteryBox(controller, "0"); // G 键默认后缀为 "0"

        return HookResult.Continue;
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

    // ========== 命令：!mh ==========
    [Command("mh", Description = "开启一个神秘盲盒，后缀会拼接到物品名称上")]
    public void CmdMysteryBox(CCitadelPlayerController caller, string suffix = "0")
    {
        if (string.IsNullOrEmpty(suffix))
        {
            suffix = "0";
        }
        Console.WriteLine($"[神秘盲盒] 玩家 {caller?.PlayerName} 输入了 !mh {suffix}");
        StartMysteryBox(caller, suffix);
    }

    // ========== 命令：!give ==========
    [Command("give", Description = "给自己添加指定物品（增强版本）", SuppressChat = true)]
    public void CmdGiveItem(CCitadelPlayerController caller, string itemName)
    {
        if (caller == null) return;

        var pawn = caller.GetHeroPawn();
        if (pawn == null)
        {
            caller.PrintToConsole("[神秘盲盒] 无法获取英雄实体");
            return;
        }

        if (string.IsNullOrEmpty(itemName))
        {
            caller.PrintToConsole("[神秘盲盒] 请指定物品名称，例如: !give upgrade_ancient_shield");
            return;
        }

        var result = pawn.AddItem(itemName, true);
        if (result != null)
        {
            caller.PrintToConsole($"[神秘盲盒] 成功添加物品: {itemName} (增强版)");
            Console.WriteLine($"[神秘盲盒] 玩家 {caller.PlayerName} 添加了物品: {itemName} (增强版)");
        }
        else
        {
            caller.PrintToConsole($"[神秘盲盒] 添加物品失败: {itemName}，请检查物品名称是否正确");
            Console.WriteLine($"[神秘盲盒] 玩家 {caller.PlayerName} 添加物品失败: {itemName}");
        }
    }
}
