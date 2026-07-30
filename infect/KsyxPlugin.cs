using DeadworksManaged.Api;

namespace MyPlugin;

public class HelloPlugin : DeadworksPluginBase
{
    public override string Name => "Hello";

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
