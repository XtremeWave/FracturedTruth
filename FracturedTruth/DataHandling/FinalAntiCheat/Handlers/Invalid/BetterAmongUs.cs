using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Invalid;

public class BetterAmongUs : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        151,
        152
    ];

    public bool HandleInvalidRPC(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        reason = "BetterAmongUs";
        return false;
    }
}