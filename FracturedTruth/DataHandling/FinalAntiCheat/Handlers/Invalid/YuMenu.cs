using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Invalid;

public class YuMenu : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        unchecked((byte)520)
    ];

    public bool HandleInvalidRPC(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        reason = "YuMenu";
        return true;
    }
}