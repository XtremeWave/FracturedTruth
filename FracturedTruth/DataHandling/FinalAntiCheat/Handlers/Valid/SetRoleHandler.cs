using AmongUs.GameOptions;
using FracturedTruth.Modules.Core.Game;
using FracturedTruth.Modules.Core.Game.PlayerControlExtension;
using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 44
public class SetRoleHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.SetRole
    ];

    public bool HandleLobby(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return true;
    }

    public bool HandleGame_All(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return sender.GetXtremeData().RoleAssgined && !IsGhost((RoleTypes)reader.ReadUInt16());
    }
}