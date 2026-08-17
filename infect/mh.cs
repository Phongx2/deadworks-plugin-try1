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

        public BoxState(CCitadelPlayerPawn pawn, CCitadelPlayerController controller)
        {
            Pawn = pawn;
            Controller = controller;
            RotationTimer = null;
            FinalizeTimer = null;
            CurrentItemIndex = 0;
        }
    }

    private readonly Dictionary<CCitadelPlayerPawn, BoxState> _pendingBoxes = new();

    // ========== 核心盲盒逻辑 ==========
    private void StartMysteryBox(CCitadelPlayerController caller)
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

        var state = new BoxState(pawn, caller);
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

            // ========== 直接添加物品，不加后缀 ==========
            if (isEnhanced)
            {
                pawn.AddItem(finalItem, true);
            }
            else
            {
                pawn.AddItem(finalItem, false);
            }

            var resultMsg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "🎉 恭喜获得！",
                DescriptionLocstring = isEnhanced ? $"你获得了强化的 {chineseName} ✨" : $"你获得了: {chineseName}"
            };
            NetMessages.Send(resultMsg, RecipientFilter.Single(caller.Slot));

            caller.PrintToConsole($"[神秘盲盒] 你获得了{(isEnhanced ? "强化的 " : " ")}{chineseName}");

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
        StartMysteryBox(controller);

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
    [Command("mh", Description = "开启一个神秘盲盒", SuppressChat = true)]
    public void CmdMysteryBox(CCitadelPlayerController caller)
    {
        Console.WriteLine($"[神秘盲盒] 玩家 {caller?.PlayerName} 输入了 !mh");
        StartMysteryBox(caller);
    }

    // ========== 命令：!give ==========
    [Command("give", Description = "添加物品: !give <物品名> <0-2> <true/false>", SuppressChat = true)]
public void CmdGiveItem(CCitadelPlayerController caller, string itemName, string suffix, string enhanced)
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
        caller.PrintToConsole("[神秘盲盒] 请指定物品名称，例如: !give upgrade_ancient_shield 0 true");
        return;
    }

    // 解析 enhanced 参数
    bool isEnhanced = enhanced.Equals("true", StringComparison.OrdinalIgnoreCase);

    // 解析 suffix 参数（0-2）
    if (string.IsNullOrEmpty(suffix))
    {
        suffix = "0";
    }

    // 尝试将 suffix 解析为 int，用于 AddItem 的第三个参数
    if (!int.TryParse(suffix, out int suffixInt))
    {
        suffixInt = 0;
    }
    if (suffixInt < 0 || suffixInt > 2)
    {
        suffixInt = 0;
    }

    // ========== 使用三个参数调用 AddItem ==========
    var result = pawn.AddItem(itemName, isEnhanced, suffixInt);

    if (result != null)
    {
        caller.PrintToConsole($"[神秘盲盒] 成功添加物品: {itemName} (增强: {isEnhanced}, 后缀: {suffixInt})");
        Console.WriteLine($"[神秘盲盒] 玩家 {caller.PlayerName} 添加了物品: {itemName} (增强: {isEnhanced}, 后缀: {suffixInt})");
    }
    else
    {
        caller.PrintToConsole($"[神秘盲盒] 添加物品失败: {itemName}，请检查物品名称是否正确");
        Console.WriteLine($"[神秘盲盒] 玩家 {caller.PlayerName} 添加物品失败: {itemName}");
    }
}
}
