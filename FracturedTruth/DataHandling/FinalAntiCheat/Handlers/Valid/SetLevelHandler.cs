using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 38
public class SetLevelHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.SetLevel
    ];

    public bool HandleGame_All(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return true;
    }
}