using FinalSuspect.DataHandling.FinalAntiCheat.Interfaces;
using FinalSuspect.Modules.Core.Game;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Handlers.Valid;

// 6
public class HostRpcHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.SetName,
        (byte)RpcCalls.SetTasks,
        (byte)RpcCalls.SetStartCounter,
    ];
    
    public int MaxiReceivedNumPerSecond() => 999;
    
    public bool Condition(PlayerControl player) => player.IsHost();
}