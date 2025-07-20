using FracturedTruth.Modules.Core.Game;
using FracturedTruth.Modules.Core.Game.PlayerControlExtension;
using FracturedTruth.DataHandling.FinalAntiCheat.Interfaces;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Handlers.Valid;

// 6
public class HostRpcHandler : IRpcHandler
{
    public List<byte> TargetRpcs =>
    [
        (byte)RpcCalls.SetName,
        (byte)RpcCalls.SetTasks,
        (byte)RpcCalls.SetStartCounter,
        (byte)RpcCalls.SyncSettings
    ];

    public int MaxiReceivedNumPerSecond()
    {
        return 999;
    }

    public bool Condition(PlayerControl player)
    {
        return player.IsHost();
    }
}