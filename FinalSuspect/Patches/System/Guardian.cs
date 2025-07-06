using System;
using FinalSuspect.Modules.Core.Game;
using Hazel;
using InnerNet;

namespace FinalSuspect.Patches.System;

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
public static class HandleGameDataPatch
{
    public static bool Prefix(InnerNetClient __instance, [HarmonyArgument(0)] MessageReader parentReader)
    {
        if (!IsLobby || IsNotJoined || !OnGameJoinedPatch.JoinedCompleted) return true;

        try
        {
            if (parentReader.Length < 1) return false;
            var messageReader = MessageReader.Get(parentReader);
            var reader = messageReader.ReadMessageAsNewBuffer();
            return reader.Length >= 1;
        }
        catch
        {
            return PlayerControl.LocalPlayer?.GetClient() == null;
        }
    }
}

[HarmonyPatch(typeof(InnerNetClient._HandleGameDataInner_d__165), nameof(InnerNetClient._HandleGameDataInner_d__165.MoveNext))]
public static class HandleGameDataInnerPatch
{
    public static bool Prefix(InnerNetClient._HandleGameDataInner_d__165 __instance)
    {
        if (!IsLobby || IsNotJoined || !OnGameJoinedPatch.JoinedCompleted) return true;

        try
        {
            return __instance.reader.Length >= 1;
        }
        catch
        {
            return PlayerControl.LocalPlayer?.GetClient() == null;
        }
    }
}

[HarmonyPatch(typeof(InnerNetServer), nameof(InnerNetServer.HandleMessage))]
internal class HandleMessagePatch
{
    private static readonly Dictionary<int, RpcCounter> playerRpcCounters = new();

    public static bool Prefix(InnerNetServer.Player client, MessageReader reader)
    {
        if (!playerRpcCounters.TryGetValue(client.Id, out var counter))
        {
            counter = new RpcCounter();
            playerRpcCounters[client.Id] = counter;
        }

        if (counter.IncomingOverload) return false;
        counter.Update(reader.Tag);

        if (counter.TotalRpcLastSecond <= 100 && counter.GetRpcCount(reader.Tag) <= 40) return true;

        counter.IncomingOverload = true;
        var _player = XtremePlayerData.AllPlayerData.FirstOrDefault(x => x.CheatData.ClientData.Id == client.Id)?.Player;
        Warn($"Incoming Msg Overloaded: {_player?.GetDataName() ?? ""}", "FAC");
        _player?.MarkAsCheater();
        return false;
    }

    private class RpcCounter
    {
        public int TotalRpcLastSecond;
        private readonly Dictionary<byte, int> RpcTypeCounts = new();
        private DateTime lastReset = DateTime.UtcNow;
        public bool IncomingOverload;

        public void Update(byte rpcType)
        {
            if ((DateTime.UtcNow - lastReset).TotalSeconds >= 1)
            {
                TotalRpcLastSecond = 0;
                RpcTypeCounts.Clear();
                lastReset = DateTime.UtcNow;
            }

            TotalRpcLastSecond++;
            RpcTypeCounts[rpcType] = RpcTypeCounts.TryGetValue(rpcType, out var count) ? count + 1 : 1;
        }

        public int GetRpcCount(byte rpcType) => RpcTypeCounts.GetValueOrDefault(rpcType, 0);
    }
}