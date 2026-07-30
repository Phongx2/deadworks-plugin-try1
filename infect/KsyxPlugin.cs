using DeadworksManaged.Api;

namespace MyPlugin;

public class HelloPlugin : DeadworksPluginBase
{
    public override string Name => "Hello";

    // ========== 必须添加这两个方法 ==========
    public override void OnLoad(bool isReload)
    {
        Console.WriteLine($"[{Name}] Loaded! (reload={isReload})");
    }

    public override void OnUnload()
    {
        Console.WriteLine($"[{Name}] Unloaded!");
    }
    // ========== 添加结束 ==========

    [Command("hello", Description = "Show a welcome message")]
    public void CmdHello(CCitadelPlayerController caller)
    {
        var msg = new CCitadelUserMsg_HudGameAnnouncement
        {
            TitleLocstring = "HELLO",
            DescriptionLocstring = "Welcome to Deadworks"
        };

        NetMessages.Send(msg, RecipientFilter.Single(caller.EntityIndex - 1));
    }
}
