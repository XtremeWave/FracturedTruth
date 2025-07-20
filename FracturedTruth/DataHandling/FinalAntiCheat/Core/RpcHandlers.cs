using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Core;

public class RpcHandlers(List<byte> targetRpcs)
{
    public readonly List<IRpcHandler> Handlers = [];
    public readonly List<byte> TargetRpcs = targetRpcs;
}