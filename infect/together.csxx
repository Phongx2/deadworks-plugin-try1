using DeadworksManaged.Api;
using System.Numerics;
using DeadworksManaged.Api.Sounds;

namespace XfPlugin;

public class XfPlugin : DeadworksPluginBase
{
    public override string Name => "Xf";

    private bool _isActive = false;
    private IHandle? _moveTimer = null;

    // 存储每队队长的 SteamId（永久锁定）
    private ulong? _team2TargetSteamId = null;
    private ulong? _team3TargetSteamId = null;

    // 存储每队队长的 Pawn 引用（实时更新）
    private CCitadelPlayerPawn? _team2Target = null;
    private CCitadelPlayerPawn? _team3Target = null;

    // 存储每队的跟随者列表
    private List<CCitadelPlayerPawn> _team2Followers = new List<CCitadelPlayerPawn>();
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

        // 清空所有状态
        _team2Followers.Clear();
        _team3Followers.Clear();
        _team2Target = null;
        _team3Target = null;
        _team2TargetSteamId = null;
        _team3TargetSteamId = null;

        var allPawns = Players.GetAllPawns().ToList();
        if (allPawns.Count < 2)
        {
            Console.WriteLine("[Xf] 玩家数量不足，需要至少2人");
            return;
        }

        var team2Pawns = allPawns.Where(p => p.TeamNum == 2).ToList();
        var team3Pawns = allPawns.Where(p => p.TeamNum == 3).ToList();

        // ========== 随机选 Team 2 队长 ==========
        if (team2Pawns.Count >= 2)
        {
            var random = new Random();
            int targetIndex = random.Next(team2Pawns.Count);
            _team2Target = team2Pawns[targetIndex];

            var controller = GetControllerFromPawn(_team2Target);
            if (controller != null)
            {
                _team2TargetSteamId = controller.PlayerSteamId;
            }

            _team2Followers = team2Pawns
                .Where((p, index) => index != targetIndex)
                .ToList();

            Console.WriteLine($"[Xf] Team 2 队长: {GetPlayerName(_team2Target)} (SteamId: {_team2TargetSteamId})");
        }
        else
        {
            Console.WriteLine("[Xf] Team 2 人数不足，无法设置跟随");
        }

        // ========== 随机选 Team 3 队长 ==========
        if (team3Pawns.Count >= 2)
        {
            var random = new Random();
            int targetIndex = random.Next(team3Pawns.Count);
            _team3Target = team3Pawns[targetIndex];

            var controller = GetControllerFromPawn(_team3Target);
            if (controller != null)
            {
                _team3TargetSteamId = controller.PlayerSteamId;
            }

            _team3Followers = team3Pawns
                .Where((p, index) => index != targetIndex)
                .ToList();

            Console.WriteLine($"[Xf] Team 3 队长: {GetPlayerName(_team3Target)} (SteamId: {_team3TargetSteamId})");
        }
        else
        {
            Console.WriteLine("[Xf] Team 3 人数不足，无法设置跟随");
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

            // ========== 刷新队长状态（检查队长是否死亡/复活） ==========
            RefreshTargets();

            // ========== 移动 Team 2 的跟随者 ==========
            if (_team2Target != null && _team2Target.IsValid && _team2Target.LifeState == LifeState.Alive)
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
            // 队长死亡 → 不移动

            // ========== 移动 Team 3 的跟随者 ==========
            if (_team3Target != null && _team3Target.IsValid && _team3Target.LifeState == LifeState.Alive)
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
            // 队长死亡 → 不移动
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
        _team2TargetSteamId = null;
        _team3TargetSteamId = null;
    }

    // ========== 刷新队长状态 ==========
    private void RefreshTargets()
    {
        // ========== 刷新 Team 2 队长 ==========
        if (_team2TargetSteamId.HasValue)
        {
            // 通过 SteamId 查找队长
            var allPawns = Players.GetAllPawns().ToList();
            var target = allPawns.FirstOrDefault(p =>
            {
                var controller = GetControllerFromPawn(p);
                return controller != null && controller.PlayerSteamId == _team2TargetSteamId.Value;
            });

            if (target != null && target.IsValid)
            {
                _team2Target = target;
                // 更新跟随者列表（排除队长自己）
                var teamPawns = allPawns.Where(p => p.TeamNum == 2).ToList();
                _team2Followers = teamPawns.Where(p => p != target).ToList();
            }
            else
            {
                // 队长不在游戏中（已离开），该队停止跟随
                if (_team2Target != null)
                {
                    Console.WriteLine("[Xf] Team 2 队长已离开游戏");
                    _team2Target = null;
                    _team2Followers.Clear();
                }
            }
        }

        // ========== 刷新 Team 3 队长 ==========
        if (_team3TargetSteamId.HasValue)
        {
            var allPawns = Players.GetAllPawns().ToList();
            var target = allPawns.FirstOrDefault(p =>
            {
                var controller = GetControllerFromPawn(p);
                return controller != null && controller.PlayerSteamId == _team3TargetSteamId.Value;
            });

            if (target != null && target.IsValid)
            {
                _team3Target = target;
                var teamPawns = allPawns.Where(p => p.TeamNum == 3).ToList();
                _team3Followers = teamPawns.Where(p => p != target).ToList();
            }
            else
            {
                if (_team3Target != null)
                {
                    Console.WriteLine("[Xf] Team 3 队长已离开游戏");
                    _team3Target = null;
                    _team3Followers.Clear();
                }
            }
        }
    }

    private void SendHUDToAll()
    {
        if (_team2Target != null && _team2Target.IsValid && _team2Followers.Count > 0)
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

        if (_team3Target != null && _team3Target.IsValid && _team3Followers.Count > 0)
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
