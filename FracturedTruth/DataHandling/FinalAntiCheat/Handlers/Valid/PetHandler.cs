using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;
using Hazel;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 49, 50
public class PetHandler : IRpcHandler
{
    //private static readonly Dictionary<byte, int> _counters = new();
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.Pet,
        (byte)RpcCalls.CancelPet
    ];

    public bool HandleAll(PlayerControl sender, MessageReader reader,
        ref bool notify, ref string reason, ref bool ban)
    {
        return false;
    }

    public int MaxiReceivedNumPerSecond()
    {
        return 20;
    }
}