using AmongUs.GameOptions;
using FracturedTruth.Modules.Core.Game;
using FracturedTruth.Modules.Core.Game.PlayerControlExtension;
using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 62~65
public class PhantomHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.CheckVanish,
        (byte)RpcCalls.StartVanish,
        (byte)RpcCalls.CheckAppear,
        (byte)RpcCalls.StartAppear
    ];

    public bool HandleLobby(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return true;
    }

    public bool HandleGame_All(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return sender.GetRoleType() is not RoleTypes.Phantom;
    }
}