using DeadworksManaged.Api;
using System.Numerics;
using DeadworksManaged.Api.Sounds;

namespace XfPlugin;

public class XfPlugin : DeadworksPluginBase
{
    public override string Name => "Xf";

    private bool _isActive = false;
    private IHandle? _moveTimer = null;

    private CCitadelPlayerPawn? _team2Target = null;
    private List<CCitadelPlayerPawn> _team2Followers = new List<CCitadelPlayerPawn>();
    private CCitadelPlayerPawn? _team3Target = null;
    private List<CCitadelPlayerPawn> _team3Followers = new List<CCitadelPlayerPawn>();

    public override void OnLoad(bool isReload)
    {
        Console.WriteLine(isReload ? "[Xf] 热重载完成！" : "[Xf] 已加载！");
        Console.WriteLine("[Xf] 输入 /xf 启动/停止跟随模式");
    }

    public override void OnUnload()
    {
        Console.WriteLine("[Xf] 已卸载！");
        StopXf();
    }

    [GameEventHandler("player_hero_changed")]
    public HookResult OnPlayerHeroChanged(PlayerHeroChangedEvent args)
    {
        if (!_isActive) return HookResult.Continue;

        Console.WriteLine("[Xf] 检测到玩家切换英雄，重新分配跟随目标");
        ReassignTargets();

        return HookResult.Continue;
    }

    [Command("xf", Description = "每队选一个人作为目标，队友跟随其位置")]
    public void CmdXf(CCitadelPlayerController caller)
    {
        if (_isActive)
        {
            StopXf();
            if (caller != null) caller.PrintToConsole("[Xf] 跟随模式已停止");
            Console.WriteLine("[Xf] 跟随模式已停止");
            return;
        }

        StartXf();
        if (caller != null) caller.PrintToConsole("[Xf] 跟随模式已启动");
        Console.WriteLine("[Xf] 跟随模式已启动");
    }

    private void StartXf()
    {
        if (_isActive) return;

        Console.WriteLine("[Xf] 启动跟随模式");

        Sounds.Play("Stinger.Koth.Announce", RecipientFilter.All, volume: 0.4f);

        _team2Followers.Clear();
        _team3Followers.Clear();
        _team2Target = null;
        _team3Target = null;

        var allPawns = Players.GetAllPawns().ToList();
        if (allPawns.Count < 2)
        {
            Console.WriteLine("[Xf] 玩家数量不足，需要至少2人");
            return;
        }

        var team2Pawns = allPawns.Where(p => p.TeamNum == 2).ToList();
        var team3Pawns = allPawns.Where(p => p.TeamNum == 3).ToList();

        // ========== 处理 Team 2 ==========
        if (team2Pawns.Count >= 2)
        {
            var random = new Random();
            int targetIndex = random.Next(team2Pawns.Count);
            _team2Target = team2Pawns[targetIndex];

            _team2Followers = team2Pawns
                .Where((p, index) => index != targetIndex)
                .ToList();

            Console.WriteLine($"[Xf] Team 2 目标: {GetPlayerName(_team2Target)}, 跟随者: {_team2Followers.Count} 人");
        }
        else if (team2Pawns.Count == 1)
        {
            Console.WriteLine("[Xf] Team 2 只有一个人，无法设置跟随");
        }

        // ========== 处理 Team 3 ==========
        if (team3Pawns.Count >= 2)
        {
            var random = new Random();
            int targetIndex = random.Next(team3Pawns.Count);
            _team3Target = team3Pawns[targetIndex];

            _team3Followers = team3Pawns
                .Where((p, index) => index != targetIndex)
                .ToList();

            Console.WriteLine($"[Xf] Team 3 目标: {GetPlayerName(_team3Target)}, 跟随者: {_team3Followers.Count} 人");
        }
        else if (team3Pawns.Count == 1)
        {
            Console.WriteLine("[Xf] Team 3 只有一个人，无法设置跟随");
        }

        if (_team2Followers.Count == 0 && _team3Followers.Count == 0)
        {
            Console.WriteLine("[Xf] 没有足够的玩家进行跟随模式");
            return;
        }

        _isActive = true;

        SendHUDToAll();

        // ========== 启动每 50ms 移动 ==========
        _moveTimer = Timer.Every(50.Milliseconds(), () =>
        {
            if (!_isActive)
            {
                _moveTimer?.Cancel();
                _moveTimer = null;
                return;
            }

            // ========== 检查目标是否失效，只有失效时才重新分配 ==========
            bool target2Invalid = (_team2Target == null || !_team2Target.IsValid);
            bool target3Invalid = (_team3Target == null || !_team3Target.IsValid);

            if (target2Invalid || target3Invalid)
            {
                Console.WriteLine("[Xf] 检测到目标失效，重新分配");
                ReassignTargets();
                if (!_isActive) return;
            }

            // ========== 移动 Team 2 的跟随者 ==========
            if (_team2Target != null && _team2Target.IsValid)
            {
                Vector3 targetPos = _team2Target.Position;
                foreach (var follower in _team2Followers)
                {
                    if (follower != null && follower.IsValid)
                    {
                        follower.Teleport(targetPos, null, null);
                    }
                }
            }

            // ========== 移动 Team 3 的跟随者 ==========
            if (_team3Target != null && _team3Target.IsValid)
            {
                Vector3 targetPos = _team3Target.Position;
                foreach (var follower in _team3Followers)
                {
                    if (follower != null && follower.IsValid)
                    {
                        follower.Teleport(targetPos, null, null);
                    }
                }
            }
        });
    }

    private void StopXf()
    {
        if (!_isActive) return;

        Console.WriteLine("[Xf] 停止跟随模式");
        _isActive = false;

        _moveTimer?.Cancel();
        _moveTimer = null;

        _team2Target = null;
        _team2Followers.Clear();
        _team3Target = null;
        _team3Followers.Clear();
    }

    private void ReassignTargets()
    {
        if (!_isActive) return;

        Console.WriteLine("[Xf] 重新分配目标和跟随者");

        // 保存旧的跟随者列表（用于重新分配时保留未失效的玩家）
        var oldTeam2Followers = new List<CCitadelPlayerPawn>(_team2Followers);
        var oldTeam3Followers = new List<CCitadelPlayerPawn>(_team3Followers);

        _team2Followers.Clear();
        _team3Followers.Clear();
        _team2Target = null;
        _team3Target = null;

        var allPawns = Players.GetAllPawns().ToList();
        if (allPawns.Count < 2) return;

        var team2Pawns = allPawns.Where(p => p.TeamNum == 2).ToList();
        var team3Pawns = allPawns.Where(p => p.TeamNum == 3).ToList();

        // ========== 重新分配 Team 2 ==========
        if (team2Pawns.Count >= 2)
        {
            // 优先保留旧目标（如果仍然有效）
            CCitadelPlayerPawn? newTarget = null;
            if (_team2Target != null && _team2Target.IsValid && team2Pawns.Contains(_team2Target))
            {
                newTarget = _team2Target;
            }
            else
            {
                var random = new Random();
                newTarget = team2Pawns[random.Next(team2Pawns.Count)];
            }

            _team2Target = newTarget;
            _team2Followers = team2Pawns.Where(p => p != newTarget).ToList();

            Console.WriteLine($"[Xf] Team 2 新目标: {GetPlayerName(_team2Target)}");
        }
        else if (team2Pawns.Count == 1)
        {
            // 只有一个人，没有跟随者
            Console.WriteLine("[Xf] Team 2 只有一个人");
        }

        // ========== 重新分配 Team 3 ==========
        if (team3Pawns.Count >= 2)
        {
            CCitadelPlayerPawn? newTarget = null;
            if (_team3Target != null && _team3Target.IsValid && team3Pawns.Contains(_team3Target))
            {
                newTarget = _team3Target;
            }
            else
            {
                var random = new Random();
                newTarget = team3Pawns[random.Next(team3Pawns.Count)];
            }

            _team3Target = newTarget;
            _team3Followers = team3Pawns.Where(p => p != newTarget).ToList();

            Console.WriteLine($"[Xf] Team 3 新目标: {GetPlayerName(_team3Target)}");
        }
        else if (team3Pawns.Count == 1)
        {
            Console.WriteLine("[Xf] Team 3 只有一个人");
        }

        // 重新发送 HUD
        SendHUDToAll();
    }

    private void SendHUDToAll()
    {
        if (_team2Target != null && _team2Followers.Count > 0)
        {
            var targetController = GetControllerFromPawn(_team2Target);
            string targetName = targetController?.PlayerName ?? "Unknown";

            foreach (var follower in _team2Followers)
            {
                var followerController = GetControllerFromPawn(follower);
                if (followerController != null)
                {
                    var msg = new CCitadelUserMsg_HudGameAnnouncement
                    {
                        TitleLocstring = "🚶 一起行动",
                        DescriptionLocstring = $"跟着队长：{targetName}"
                    };
                    NetMessages.Send(msg, RecipientFilter.Single(followerController.Slot));
                }
            }

            if (targetController != null)
            {
                var msg = new CCitadelUserMsg_HudGameAnnouncement
                {
                    TitleLocstring = "🚶 一起行动",
                    DescriptionLocstring = "你是队长，队友将跟随你"
                };
                NetMessages.Send(msg, RecipientFilter.Single(targetController.Slot));
            }
        }

        if (_team3Target != null && _team3Followers.Count > 0)
        {
            var targetController = GetControllerFromPawn(_team3Target);
            string targetName = targetController?.PlayerName ?? "Unknown";

            foreach (var follower in _team3Followers)
            {
                var followerController = GetControllerFromPawn(follower);
                if (followerController != null)
                {
                    var msg = new CCitadelUserMsg_HudGameAnnouncement
                    {
                        TitleLocstring = "🚶 一起行动",
                        DescriptionLocstring = $"跟着队长：{targetName}"
                    };
                    NetMessages.Send(msg, RecipientFilter.Single(followerController.Slot));
                }
            }

            if (targetController != null)
            {
                var msg = new CCitadelUserMsg_HudGameAnnouncement
                {
                    TitleLocstring = "🚶 一起行动",
                    DescriptionLocstring = "你是队长，队友将跟随你"
                };
                NetMessages.Send(msg, RecipientFilter.Single(targetController.Slot));
            }
        }
    }

    private string GetPlayerName(CCitadelPlayerPawn pawn)
    {
        if (pawn == null) return "Unknown";
        foreach (var controller in Players.GetAll())
        {
            if (controller.GetHeroPawn() == pawn)
                return controller.PlayerName;
        }
        return "Unknown";
    }

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
}
