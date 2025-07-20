using AmongUs.GameOptions;
using FracturedTruth.Modules.Core.Game;
using FracturedTruth.Modules.Core.Game.PlayerControlExtension;
using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 19, 20
public class VentHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.EnterVent,
        (byte)RpcCalls.ExitVent
    ];

    public bool HandleAll(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return !sender.IsImpostor() &&
               sender.GetRoleType() is not RoleTypes.Engineer and not RoleTypes.CrewmateGhost
                   and not RoleTypes.GuardianAngel;
    }
}