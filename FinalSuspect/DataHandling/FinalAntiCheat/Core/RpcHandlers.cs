using System.Collections.Generic;
using FinalSuspect.DataHandling.FinalAntiCheat.Interfaces;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

public class RpcHandlers(List<byte> targetRpcs)
{
    public readonly List<byte> TargetRpcs = targetRpcs;
    public readonly List<IRpcHandler> Handlers = [];
}