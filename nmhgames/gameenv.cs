using DeadworksManaged.Api;
using System;

public class ServerDefaultsPlugin : DeadworksPluginBase
{
    public override string Name => "Server Defaults";

    // 头部变量：1 = 执行开发指令，0 = 不执行
    private int dev = 1;

    // 每次地图启动（包括第一次）都会执行
    public override void OnStartupServer()
    {
        // 1. 设置基础 ConVar（无论 dev 开不开，这个设置一般都保留）
        // 使用 ConVar.SetInt 设置整数变量 [citation:1]
        ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);

        // 2. 如果 dev == 1，执行那些强制击杀指令
        if (dev == 1)
        {
            Console.WriteLine("[ServerDefaults] dev=1，正在清理兵线、守卫、行者和基地守卫...");

            // 使用 Server.ExecuteCommand 模拟在服务器控制台输入指令 [citation:2]
            Server.ExecuteCommand("citadel_force_kill troopers");
            Server.ExecuteCommand("citadel_force_kill guardians");
            Server.ExecuteCommand("citadel_force_kill walkers");
            Server.ExecuteCommand("citadel_force_kill base_guardians");
        }
        else
        {
            Console.WriteLine("[ServerDefaults] dev=0，跳过强制击杀指令。");
        }
    }
}
