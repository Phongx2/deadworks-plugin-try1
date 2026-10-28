using DeadworksManaged.Api;
using System;

public class ServerDefaultsPlugin : DeadworksPluginBase
{
    public override string Name => "Server Defaults";

    // 1 = 开启开发者指令，0 = 关闭
    private int dev = 1;

    public override void OnStartupServer()
    {
        // 基础设置：随处购买（不受 dev 影响）
        ConVar.Find("citadel_allow_purchasing_anywhere")?.SetInt(1);
ConVar.Find("citadel_active_lane")?.SetInt(4);
 ConVar.Find("citadel_player_spawn_time_max_respawn_time")?.SetInt(15);
ConVar.Find("citadel_player_starting_gold")?.SetInt(1600);
ConVar.Find("citadel_trooper_spawn_enabled")?.SetInt(0);
ConVar.Find("citadel_start_players_on_zipline")?.SetInt(1);





        

        if (dev == 1)
        {
            Console.WriteLine("[ServerDefaults] dev=1，开启 sv_cheats 并清理地图实体...");
            
            // 第 1 帧：开启作弊模式
            Server.ExecuteCommand("sv_cheats 1");

            // 第 2 帧：在作弊模式下执行击杀指令
            Timer.NextTick(() =>
            {
                Server.ExecuteCommand("citadel_force_kill troopers");
                Server.ExecuteCommand("citadel_force_kill guardians");
                Server.ExecuteCommand("citadel_force_kill walkers");
                Server.ExecuteCommand("citadel_force_kill base_guardians");

                // 第 3 帧：击杀指令提交后，关闭作弊模式
                Timer.NextTick(() =>
                {
                    Server.ExecuteCommand("sv_cheats 0");
                    Console.WriteLine("[ServerDefaults] 清理完毕，sv_cheats 已关闭。");
                });
            });
        }
    }
}
