using AmongUs.GameOptions;
using FracturedTruth.Modules.Core.Game;
using FracturedTruth.Modules.Core.Game.PlayerControlExtension;
using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 46, 55, 56
public class ShapeShifterHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.Shapeshift,
        (byte)RpcCalls.CheckShapeshift,
        (byte)RpcCalls.RejectShapeshift
    ];

    public bool HandleLobby(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return true;
    }

    public bool HandleGame_All(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return sender.GetRoleType() is not RoleTypes.Shapeshifter and not RoleTypes.ImpostorGhost;
    }
}