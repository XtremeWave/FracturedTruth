using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

public class SnapToHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.SnapTo
    ];

    public int MaxiReceivedNumPerSecond()
    {
        return 30;
    }
}