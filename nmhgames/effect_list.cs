using DeadworksManaged.Api;
using System;

public static class EffectList
{
    // ---------------- 效果变量名 ----------------
    public const string SpeedBoost = "speed_boost";
    
    // ---------------- 变量 ----------------
    // 你想让玩家抽到的效果名字
    public const string SpeedBoostName = "加速";

    // ---------------- 抽卡池 ----------------
    private static readonly string[] Pool = new string[]
    {
        SpeedBoost
    };

    private static readonly Random _random = new Random();

    public static void Chouka(CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return;

        string selectedEffect = Pool[_random.Next(Pool.Length)];
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";
        Console.WriteLine($"[抽卡] {playerName} 抽到了效果: {selectedEffect}");

        Effects.Execute(selectedEffect, pawn);
    }
}
