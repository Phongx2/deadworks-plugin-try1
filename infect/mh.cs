using DeadworksManaged.Api;
using System.Numerics;

namespace MysteryBoxPlugin;

public class MysteryBoxPlugin : DeadworksPluginBase
{
    public override string Name => "神秘盲盒";

    private static readonly Random _rng = new Random();

    // ========== 装备池（示例） ==========
    private readonly string[] _itemPool = new string[]
    {
        "item_health_pack",
        "item_ammo_pack",
        "item_armor_vest",
        "item_speed_boost",
        "item_shield_generator",
        "item_energy_drink",
        "item_repair_kit",
        // 在这里添加更多装备名称...
    };

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[神秘盲盒] 热重载完成！" : "[神秘盲盒] 已加载！");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[神秘盲盒] 已卸载！");
        // 清理所有正在进行的盲盒计时器
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

        // 检查是否已有进行中的盲盒
        if (_pendingBoxes.ContainsKey(pawn))
        {
            caller.PrintToConsole("[神秘盲盒] 你已经在开启盲盒了！");
            return;
        }

        // ========== 1. 检查并扣除 3200 金钱（使用 GetCurrency + SetCurrency） ==========
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

        // 扣除 3200 金币
        int newGold = currentGold - 3200;
        pawn.SetCurrency(ECurrencyType.EGold, newGold);
        caller.PrintToConsole($"[神秘盲盒] 已扣除 3200 金币，剩余 {newGold}");
        // ========== 金钱扣除结束 ==========

        // 2. 显示 HUD 公告
        var startMsg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "🎁 神秘盲盒",
            DescriptionLocstring = "正在打开神秘盲盒……"
        };
        NetMessages.Send(startMsg, RecipientFilter.Single(caller.Slot));

        // 3. 创建状态
        var state = new BoxState(pawn, caller);
        _pendingBoxes[pawn] = state;

        // 4. 开始轮换物品（每 0.5 秒）
        state.RotationTimer = Timer.Every(500.Milliseconds(), () =>
        {
            if (!pawn.IsValid)
            {
                // 玩家已失效，清理状态
                CleanupBox(state);
                return;
            }

            // 移除当前物品（如果有）
            if (!string.IsNullOrEmpty(_itemPool[state.CurrentItemIndex]))
            {
                pawn.RemoveItem(_itemPool[state.CurrentItemIndex]);
            }

            // 随机选取下一个物品索引进行展示
            state.CurrentItemIndex = _rng.Next(0, _itemPool.Length);
            string itemName = _itemPool[state.CurrentItemIndex];
            pawn.AddItem(itemName, false);
        });

        // 5. 3秒后确定最终物品
        state.FinalizeTimer = Timer.Once(3000.Milliseconds(), () =>
        {
            if (!pawn.IsValid)
            {
                CleanupBox(state);
                return;
            }

            // 停止轮换
            state.RotationTimer?.Cancel();

            // 移除当前展示的物品
            if (!string.IsNullOrEmpty(_itemPool[state.CurrentItemIndex]))
            {
                pawn.RemoveItem(_itemPool[state.CurrentItemIndex]);
            }

            // 随机确定最终物品
            int finalIndex = _rng.Next(0, _itemPool.Length);
            string finalItem = _itemPool[finalIndex];
            pawn.AddItem(finalItem, false);

            // 显示最终结果 HUD
            var resultMsg = new CCitadelUserMsg_HudGameAnnouncement
            {
                TitleLocstring = "🎉 恭喜获得！",
                DescriptionLocstring = $"你获得了: {finalItem}"
            };
            NetMessages.Send(resultMsg, RecipientFilter.Single(caller.Slot));

            caller.PrintToConsole($"[神秘盲盒] 你获得了: {finalItem}");

            // 清理状态
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

        // 获取技能名称和槽位
        string abilityName = ev.GetString("abilityname", "");
        var slot = EAbilitySlot.Invalid;

        // 查找该技能对应的槽位
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

        // 只处理 Cosmetic1 (G 键)
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
    [Command("mh", Description = "开启一个神秘盲盒")]
    public void CmdMysteryBox(CCitadelPlayerController caller)
    {
        Console.WriteLine($"[神秘盲盒] 玩家 {caller?.PlayerName} 输入了 !mh");
        StartMysteryBox(caller);
    }
}
