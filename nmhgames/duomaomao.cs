using DeadworksManaged.Api;
using DeadworksManaged.Api.Entities;
using DeadworksManaged.Api.Commands;

public class CrateCommandPlugin : DeadworksPluginBase
{
    public override string Name => "Crate Command";

    // 注意：预缓存必须使用 .vmdl 结尾，不能用 .vmdl_c
    private const string CrateModel = "models/items/item_crate.vmdl";

    // 1. 预缓存资源，防止运行时卡顿或崩溃
    public override void OnPrecacheResources()
    {
        Precache.AddResource(CrateModel);
    }

    // 2. 注册命令：自动生成 /t、!t 和 dw_t
    [Command("t", Description = "把玩家模型变成箱子")]
    public void CmdCrate(Caller caller)
    {
        // 获取执行命令的玩家及其英雄实体
        var pawn = caller.Player?.GetHeroPawn();
        if (pawn == null)
            throw new CommandException("只有玩家才能使用此命令。");

        // 尝试清空玩家原本的模型（⚠️ 实测可能无效，因为引擎通常需要有效路径）
        pawn.AcceptInput("SetModel", value: "");

        // 创建箱子实体
        var prop = CBaseEntity.CreateByDesignerName("prop_dynamic");
        if (prop == null)
            throw new CommandException("无法创建箱子实体。");

        var ekv = new CEntityKeyValues();
        ekv.SetString("model", CrateModel);
        ekv.SetVector("origin", pawn.Position); // 初始位置放在玩家脚底
        prop.Spawn(ekv);

        // 将箱子绑定到玩家身上，使其跟随玩家移动
        prop.SetParent(pawn);

        // 给予玩家反馈
        caller.Reply("箱子已绑定到你身上。");
    }
}
