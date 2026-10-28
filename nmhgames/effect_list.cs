using DeadworksManaged.Api;
using System;

public static class EffectList
{
    // ==========================================
    // 1. 效果变量名 (方便在 effects.cs 里用 switch 匹配)
    // ==========================================
    
    // 个人效果
    public const string GiveGold = "give_gold";
    public const string GiveItem = "give_item";
    public const string HealFull = "heal_full";
    
    // 全局效果
    public const string SpawnBoss = "spawn_boss";
    public const string GlobalAnnounce = "global_announce";


    // ==========================================
    // 2. 抽卡池与随机数
    // ==========================================
    
    // 把你能抽到的效果放进这个数组里
    private static readonly string[] Pool = new string[]
    {
        GiveGold,
        GiveItem,
        HealFull,
        SpawnBoss,
        GlobalAnnounce
    };

    private static readonly Random _random = new Random();


    // ==========================================
    // 3. 抽卡逻辑
    // ==========================================
    
    /// <summary>
    /// 根据传入的玩家对象，随机抽取一个效果并执行
    /// </summary>
    /// <param name="pawn">使用鹅蛋的玩家对象</param>
    public static void Chouka(CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return;

        // 随机抽取一个效果名
        string selectedEffect = Pool[_random.Next(Pool.Length)];

        // 打印日志，方便你在服务器控制台看到抽到了什么
        string playerName = pawn.Controller?.PlayerName ?? "未知玩家";
        Console.WriteLine($"[抽卡] {playerName} 抽到了效果: {selectedEffect}");

        // 交给 Effects 类去执行（把选中的效果名和玩家对象一起传过去）
        Effects.Execute(selectedEffect, pawn);
    }
}
