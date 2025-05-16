using System;
using System.Linq;
using System.Threading.Tasks;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using FinalSuspect.Patches.Game_Vanilla;
using FinalSuspect.Patches.System;
using Hazel;
using InnerNet;

namespace FinalSuspect.Modules.Features.CheckingandBlocking;

public enum Sounds
{
    KillSound,
    TaskComplete,
    TaskUpdateSound,
    ImpTransform,
    Yeehawfrom,
}

[HarmonyPatch(typeof(InnerNetObject), nameof(InnerNetObject.HandleRpc))]
internal class RPCHandlerPatch
{
    public static bool Prefix(InnerNetObject __instance, [HarmonyArgument(0)] ref byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!__instance) return true;
        var netId = __instance.NetId;
        var player = XtremePlayerData.AllPlayerData.FirstOrDefault(x => x.NetId == netId)?.Player;
        if (!player) return true;
        if (OnPlayerLeftPatch.ClientsProcessed.Contains(player.PlayerId)) return false;

        Info($"{player.Data?.PlayerId}" +
             $"({player.Data?.PlayerName})" +
             $"{(player.IsHost() ? "Host" : "")}" +
             $":{callId}({RPC.GetRpcName(callId)})",
            "ReceiveRPC");
        
        if (XtremePlayerData.AllPlayerData.Any(data => data.PlayerId == player.Data?.PlayerId))
            if (ReceiveRpc(player, callId, reader, out var notify, out var reason, out var ban))
            {
                if (!player.IsLocalPlayer())
                {
                    player.MarkAsCheater();
                }

                if (AmongUsClient.Instance.AmHost)
                {
                    KickPlayer(player.PlayerId, ban, reason);
                    WarnHost();
                    if (notify)
                        NotificationPopperPatch.NotificationPop
                        (string.Format(GetString("Warning.InvalidSlothRPC"), player.GetRealName(),
                            $"{callId}({RPC.GetRpcName(callId)})"));
                }
                else if (notify)
                    NotificationPopperPatch.NotificationPop
                    (string.Format(GetString("Warning.InvalidSlothRPC_NotHost"), player.GetRealName(),
                        $"{callId}({RPC.GetRpcName(callId)})"));

                return false;
            }

        var subReader = MessageReader.Get(reader);
        var rpcType = (RpcCalls)callId;


        switch (rpcType)
        {
            case RpcCalls.CheckName: //CheckNameRPC
                var name = subReader.ReadString();
                Info("RPC Check Name For Player: " + name, "CheckName");
                if (player.IsHost())
                    Main.HostNickName = name;
                if (XtremePlayerData.AllPlayerData.All(data => data.PlayerId != player.PlayerId))
                    XtremePlayerData.CreateDataFor(player, name);
                break;
            case RpcCalls.SetName: //SetNameRPC
                subReader.ReadUInt32();
                name = subReader.ReadString();
                Info("RPC Set Name For Player: " + player.GetNameWithRole() + " => " + name, "SetName");
                break;
            case RpcCalls.SendChat: // Free chat
                var text = subReader.ReadString();
                Info($"{player.GetNameWithRole().RemoveHtmlTags()}:{text.RemoveHtmlTags()}", "ReceiveChat");
                break;
            case RpcCalls.SendQuickChat:
                Info($"{player.GetNameWithRole().RemoveHtmlTags()}:Some message from quick chat", "ReceiveChat");
                break;
            case RpcCalls.StartMeeting:
                var p = GetPlayerById(subReader.ReadByte());
                Info($"{player.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
                break;
        }

        return true;
    }

    public static void Postfix(InnerNetObject __instance, [HarmonyArgument(0)] byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!__instance) return;
        var netId = __instance.NetId;
        var player = XtremePlayerData.AllPlayerData.FirstOrDefault(x => x.NetId == netId)?.Player;
        if (!player) return;
        
        var rpcType = (RpcCalls)callId;
        switch (rpcType)
        {
            case RpcCalls.CancelPet:
                try
                {
                    var version = Version.Parse(reader.ReadString());
                    var tag = reader.ReadString();
                    var forkId = reader.ReadString();

                    XtremeGameData.PlayerVersion.playerVersion[player.PlayerId] =
                        new XtremeGameData.PlayerVersion(version, tag, forkId);

                    if (!XtremeGameData.PlayerVersion.playerVersion.ContainsKey(player.PlayerId))
                        RPC.RpcVersionCheck();

                    if (Main.VersionCheat.Value && AmongUsClient.Instance.AmHost)
                        XtremeGameData.PlayerVersion.playerVersion[player.PlayerId] =
                            XtremeGameData.PlayerVersion.playerVersion[0];

                    // Kick Unmached Player Start
                    /*if (AmongUsClient.Instance.AmHost && tag != $"{Main.GitCommit}({Main.GitBranch})")
                    {
                        if (forkId != Main.ForkId)
                            _ = new LateTask(() =>
                            {
                                if (__instance?.Data?.Disconnected is not null and not true)
                                {
                                    var msg = string.Format(GetString("KickBecauseDiffrentVersionOrMod"), __instance?.Data?.PlayerName);
                                    Warn(msg, "Version Kick");
                                    NotificationPopperPatch.NotificationPop(msg);
                                    KickPlayer(__instance.GetClientId(), false, "ModVersionIncorrect");
                                }
                            }, 5f, "Kick");
                    }*/
                }
                catch
                {
                    /* ignored */
                }

                break;
        }
    }
}

[HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.HandleRpc))]
internal class PlayerPhysicsRPCHandlerPatch
{
    public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] ref byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!__instance) return true;
        var player = __instance.myPlayer;
        if (OnPlayerLeftPatch.ClientsProcessed.Contains(player.PlayerId)) return false;
        //Info($"{player.Data?.PlayerId}" +
        //     $"({player.Data?.PlayerName})" +
        //     $"{(player.IsHost() ? "Host" : "")}" +
        //     $":{callId}({RPC.GetRpcName(callId)})",
        //    "ReceiveRPC");

        if (XtremePlayerData.AllPlayerData.All(data => data.PlayerId != player.Data?.PlayerId)) return true;
        if (!ReceiveRpc(player, callId, reader, out var notify, out var reason, out var ban)) return true;
        if (!player.IsLocalPlayer())
        {
            player.MarkAsCheater();
        }

        if (AmongUsClient.Instance.AmHost)
        {
            KickPlayer(player.PlayerId, ban, reason);
            WarnHost();
            if (notify)
                NotificationPopperPatch.NotificationPop
                (string.Format(GetString("Warning.InvalidSlothRPC"), player.GetRealName(),
                    $"{callId}({RPC.GetRpcName(callId)})"));
        }
        else if (notify)
            NotificationPopperPatch.NotificationPop
            (string.Format(GetString("Warning.InvalidSlothRPC_NotHost"), player.GetRealName(),
                $"{callId}({RPC.GetRpcName(callId)})"));

        return false;
    }
}

internal static class RPC
{
    public static async void RpcVersionCheck()
    {
        try
        {
            while (!PlayerControl.LocalPlayer) await Task.Delay(500);
            if (!Main.VersionCheat.Value)
            {
                var writer = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.CancelPet);
                writer.Write(Main.PluginVersion);
                writer.Write($"{Main.GitCommit}({Main.GitBranch})");
                writer.Write(Main.ForkId);
                writer.EndMessage();
            }

            XtremeGameData.PlayerVersion.playerVersion[PlayerControl.LocalPlayer.PlayerId] =
                new XtremeGameData.PlayerVersion(Main.PluginVersion, $"{Main.GitCommit}({Main.GitBranch})",
                    Main.ForkId);
        }
        catch
        {
            /* ignored */
        }
    }

    public static void SendRpcLogger(uint targetNetId, byte callId, int targetClientId = -1)
    {
        if (!DebugModeManager.AmDebugger) return;
        var rpcName = GetRpcName(callId);
        var from = targetNetId.ToString();
        var target = targetClientId.ToString();
        try
        {
            target = targetClientId < 0 ? "All" : AmongUsClient.Instance.GetClient(targetClientId).PlayerName;
            from = Main.AllPlayerControls.FirstOrDefault(c => c.NetId == targetNetId)?.Data?.PlayerName;
        }
        catch
        {
            /* ignored */
        }

        Info($"FromNetID:{targetNetId}({from}) TargetClientID:{targetClientId}({target}) CallID:{callId}({rpcName})",
            "SendRPC");
    }

    public static string GetRpcName(byte callId)
    {
        string rpcName;
        if ((rpcName = Enum.GetName(typeof(RpcCalls), callId)) == null)
            rpcName = callId + " 无效";
        return rpcName;
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.StartRpc))]
internal class StartRpcPatch
{
    public static void Prefix([HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId)
    {
        RPC.SendRpcLogger(targetNetId, callId);
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.StartRpcImmediately))]
internal class StartRpcImmediatelyPatch
{
    public static void Prefix([HarmonyArgument(0)] uint targetNetId, [HarmonyArgument(1)] byte callId,
        [HarmonyArgument(3)] int targetClientId = -1)
    {
        RPC.SendRpcLogger(targetNetId, callId, targetClientId);
    }
}

[HarmonyPatch(typeof(MessageReader), nameof(MessageReader.ReadUInt16))]
[HarmonyPatch(typeof(MessageReader), nameof(MessageReader.ReadPackedUInt32))]
[HarmonyPriority(Priority.First)]
internal class HazelPatch
{
    public static bool Prefix(MessageReader __instance)
    {
        return __instance.Length > 0;
    }
}
