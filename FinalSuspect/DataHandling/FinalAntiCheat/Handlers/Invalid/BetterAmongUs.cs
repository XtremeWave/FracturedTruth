using System.Collections.Generic;
using FinalSuspect.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Handlers.Invalid;

public class BetterAmongUs: IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        150,
        151
    ];
    
    public bool HandleInvalidRPC(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        reason = "BetterAmongUs";
        return false;
    }
}