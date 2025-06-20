using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
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

[HarmonyPatch]
internal class RPCHandlerPatch
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return from type in typeof(InnerNetObject).Assembly.GetTypes()
            where typeof(InnerNetObject).IsAssignableFrom(type) && !type.IsAbstract
            select type.GetMethod("HandleRpc", BindingFlags.Public | BindingFlags.Instance)
            into method
            where method != null && method.GetBaseDefinition() != method
            select method;
    }

    public static bool Prefix(InnerNetObject __instance, [HarmonyArgument(0)] ref byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!__instance) return true;

        var player = GetPlayerFromInstance(__instance, reader);
        if (player?.GetCheatData()?.InComingOverloaded != true)
        {
            var cd = player?.GetCheatData();
            Info(player && player.Data
                ? $"{player.Data.PlayerId}(" +
                  $"Name: {player.Data.PlayerName}," +
                  $"FriendCode: {cd?.FriendCode}," +
                  $"Puid: {cd?.Puid}," +
                  $")" +
                  $"{(player.IsHost() ? "Host" : "")}:{callId}({RPC.GetRpcName(callId)})"
                : $"Call from {__instance.name}:{callId}({RPC.GetRpcName(callId)})", "ReceiveRPC");
        }

        if (!player) return true;

        if (OnPlayerLeftPatch.ClientsProcessed.Contains(player.PlayerId)) return false;

        HandleCheatDetection(player, callId, reader);

        var rpcType = (RpcCalls)callId;
        ProcessRpc(rpcType, player, reader);

        return true;
    }

    public static PlayerControl GetPlayerFromInstance(InnerNetObject instance, MessageReader reader)
    {
        var player = XtremePlayerData.AllPlayerData.FirstOrDefault(x => instance.OwnerId == x.Player.OwnerId)?.Player;
        if (player) return player;

        try
        {
            var sr = MessageReader.Get(reader);
            player = sr.ReadNetObject<PlayerControl>();
        }
        catch
        {
            /* ignored */
        }

        return player;
    }

    private static void HandleCheatDetection(PlayerControl player, byte callId, MessageReader reader)
    {
        if (XtremePlayerData.AllPlayerData.All(data => data.PlayerId != player.Data?.PlayerId)) return;
        if (!ReceiveRpc(player, callId, reader, out var notify, out var reason, out var ban)) return;
        HandleCheater(player, notify, reason, ban, callId);
    }

    private static void HandleCheater(PlayerControl player, bool notify, string reason, bool ban, byte callId)
    {
        if (!player.IsLocalPlayer())
        {
            player.MarkAsCheater();
        }

        if (AmongUsClient.Instance.AmHost)
        {
            KickPlayer(player.PlayerId, ban, reason, KickLevel.None);
            WarnHost();
            if (notify)
            {
                NotificationPopperPatch.NotificationPop(
                    string.Format(GetString("CheatDetected.InvalidSlothRPC"), player.GetRealName(),
                        $"{callId}({RPC.GetRpcName(callId)})"));
            }
        }
        else if (notify)
        {
            NotificationPopperPatch.NotificationPop(
                string.Format(GetString("CheatDetected.InvalidSlothRPC_NotHost"), player.GetRealName(),
                    $"{callId}({RPC.GetRpcName(callId)})"));
        }
    }

    // 处理RPC调用的逻辑
    private static void ProcessRpc(RpcCalls rpcType, PlayerControl player, MessageReader reader)
    {
        var subReader = MessageReader.Get(reader);

        switch (rpcType)
        {
            case RpcCalls.CheckName:
                HandleCheckNameRpc(player, subReader);
                break;
            case RpcCalls.SetName:
                HandleSetNameRpc(player, subReader);
                break;
            case RpcCalls.SendChat:
                HandleSendChatRpc(player, subReader);
                break;
            case RpcCalls.SendQuickChat:
                HandleSendQuickChatRpc(player);
                break;
            case RpcCalls.StartMeeting:
                HandleStartMeetingRpc(player, subReader);
                break;
        }
    }

    private static void HandleCheckNameRpc(PlayerControl player, MessageReader reader)
    {
        var name = reader.ReadString();
        Info("RPC Check Name For Player: " + name, "CheckName");
        if (player.IsHost())
            Main.HostNickName = name;
        if (XtremePlayerData.AllPlayerData.All(data => data.PlayerId != player.PlayerId))
            XtremePlayerData.CreateDataFor(player, name);
    }

    private static void HandleSetNameRpc(PlayerControl player, MessageReader reader)
    {
        reader.ReadUInt32();
        var name = reader.ReadString();
        Info("RPC Set Name For Player: " + player.GetNameWithRole() + " => " + name, "SetName");
    }

    private static void HandleSendChatRpc(PlayerControl player, MessageReader reader)
    {
        var text = reader.ReadString();
        Info($"{player.GetNameWithRole().RemoveHtmlTags()}:{text.RemoveHtmlTags()}", "ReceiveChat");
    }

    private static void HandleSendQuickChatRpc(PlayerControl player)
    {
        Info($"{player.GetNameWithRole().RemoveHtmlTags()}:Some message from quick chat", "ReceiveChat");
    }

    private static void HandleStartMeetingRpc(PlayerControl player, MessageReader reader)
    {
        var p = GetPlayerById(reader.ReadByte());
        Info($"{player.GetNameWithRole()} => {p?.GetNameWithRole() ?? "null"}", "StartMeeting");
    }

    public static void Postfix(InnerNetObject __instance, [HarmonyArgument(0)] byte callId,
        [HarmonyArgument(1)] MessageReader reader)
    {
        if (!__instance) return;
        var netId = __instance.NetId;
        var player = XtremePlayerData.AllPlayerData.FirstOrDefault(x => x.NetId == netId)?.Player;
        if (!player) return;
        if (XtremeGameData.PlayerVersion.playerVersion.ContainsKey(player.PlayerId)) return;

        var rpcType = (RpcCalls)callId;
        switch (rpcType)
        {
            case RpcCalls.CancelPet:
                try
                {
                    Test(0);
                    var version = Version.Parse(reader.ReadString());
                    var tag = reader.ReadString();
                    var forkId = reader.ReadString();

                    _ = RPC.RpcVersionCheck();
                    Test(1);
                    XtremeGameData.PlayerVersion.playerVersion[player.PlayerId] = new XtremeGameData.PlayerVersion(version, tag, forkId);

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
                    XtremeGameData.PlayerVersion.playerVersion[player.PlayerId] = null;
                }

                break;
        }
    }
}

internal static class RPC
{
    private static CancellationTokenSource _rpcCts; // 用于取消异步操作

    public static async Task RpcVersionCheck()
    {
        _rpcCts?.Cancel();
        _rpcCts = new CancellationTokenSource();
        var ct = _rpcCts.Token;

        try
        {
            while (PlayerControl.LocalPlayer == null || AmongUsClient.Instance == null)
            {
                if (ct.IsCancellationRequested) return;
                await Task.Delay(500, ct);
            }

            if (PlayerControl.LocalPlayer == null ||
                AmongUsClient.Instance == null)
            {
                return;
            }

            if (!Main.VersionCheat.Value)
            {
                var writer = AmongUsClient.Instance.StartRpcImmediately(
                    PlayerControl.LocalPlayer.NetId,
                    (byte)RpcCalls.CancelPet,
                    SendOption.Reliable);
                writer.Write(Main.PluginVersion);
                writer.Write($"{Main.GitCommit}({Main.GitBranch})");
                writer.Write(Main.ForkId);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }

            if (XtremeGameData.PlayerVersion.playerVersion != null)
            {
                XtremeGameData.PlayerVersion.playerVersion[PlayerControl.LocalPlayer.PlayerId] =
                    new XtremeGameData.PlayerVersion(
                        Main.PluginVersion,
                        $"{Main.GitCommit}({Main.GitBranch})",
                        Main.ForkId
                    );
            }
        }
        catch (OperationCanceledException)
        {
            /* ignored */
        }
        catch
        {
            /* ignored */
        }
    }

    public static void Cleanup()
    {
        _rpcCts?.Cancel();
        _rpcCts?.Dispose();
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
            rpcName = callId + "(无效)";
        return rpcName;
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

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
public static class MessageReaderGuard
{
    private class MsgRecord
    {
        public long LastReceivedTime;
        public int Count;
        public bool InComingOverloaded;
    }
    
    private static Dictionary<int, MsgRecord> _msgRecords = new();
    
    public static bool Prefix(InnerNetClient __instance, [HarmonyArgument(0)] MessageReader parentReader)
    {
        if (IsNotJoined) return true;
        try
        {
            if (parentReader.Length < 1) return false;
            var messageReader = MessageReader.Get(parentReader);
            var reader = messageReader.ReadMessageAsNewBuffer();
            if (reader.Length < 1) return false;
            var sr1 = MessageReader.Get(reader);
            var sr2 = MessageReader.Get(reader);
            int id;
            PlayerControl _player;
            try
            {
                var num1 = sr1.ReadPackedUInt32();
                if (__instance.allObjects.AllObjectsFast.TryGetValue(num1, out var obj))
                {
                    _player = RPCHandlerPatch.GetPlayerFromInstance(obj, sr1);
                    id = _player.GetClientId();
                    goto CheckForRecords;
                }
            }
            catch
            {
                /* ignored */
            }
        
            var num2 = sr2.ReadPackedInt32();
            var clientData = __instance.FindClientById(num2);
            id = clientData.Id;

            CheckForRecords:
            var currentTime = GetCurrentTimestamp();
            if (_msgRecords.TryGetValue(id, out var record))
            {
                if (_msgRecords[id].InComingOverloaded) return false;
                _player = XtremePlayerData.AllPlayerData.FirstOrDefault(x => x.CheatData.ClientData.Id == id)?.Player;
                var timeDiff = currentTime - record.LastReceivedTime;

                if (timeDiff > 1000)
                {
                    record.Count = 1;
                    record.LastReceivedTime = currentTime;
                }
                else
                {
                    record.Count++;
                    Test($"{record.Count} {40}");
                    
                    if (record.Count > 40)
                    {
                        _player?.MarkAsCheater();
                        record.Count = 0;
                        _msgRecords[id].InComingOverloaded = true;
                        Warn($"InComingMsg Overloaded: {_player.GetDataName() ?? ""}", "FAC");
                        return false;
                    }
                }
                
                _msgRecords[id] = record;
            }
            else
            {
                _msgRecords[id] = new MsgRecord
                {
                    LastReceivedTime = currentTime,
                    Count = 1,
                };
            }

            return true;
        }
        catch
        {
            _msgRecords = new Dictionary<int, MsgRecord>();
            return !OnGameJoinedPatch.JoinedCompleted;
        }
    }
}

[HarmonyPatch(typeof(InnerNetClient._HandleGameDataInner_d__165), nameof(InnerNetClient._HandleGameDataInner_d__165.MoveNext))]
public static class MessageReaderGuard_Inner
{
    private class MsgRecord
    {
        public long LastReceivedTime;
        public int Count;
        public bool InComingOverloaded;
    }
    
    private static Dictionary<int, MsgRecord> _msgRecords = new();
    
    public static bool Prefix(InnerNetClient._HandleGameDataInner_d__165 __instance)
    {
        if (IsNotJoined) return true;
        try
        {
            var innerNetClient = __instance.__4__this;
            if (__instance.reader.Length < 1) return false;
            var sr1 = MessageReader.Get(__instance.reader);
            var sr2 = MessageReader.Get(__instance.reader);
            int id;
            PlayerControl _player;
            try
            {
                var num1 = sr1.ReadPackedUInt32();
                if (innerNetClient.allObjects.AllObjectsFast.TryGetValue(num1, out var obj))
                {
                    _player = RPCHandlerPatch.GetPlayerFromInstance(obj, sr1);
                    id = _player.GetClientId();
                    goto CheckForRecords;
                }
            }
            catch
            {
                /* ignored */
            }
        
            var num2 = sr2.ReadPackedInt32();
            var clientData = innerNetClient.FindClientById(num2);
            id = clientData.Id;

            CheckForRecords:
            var currentTime = GetCurrentTimestamp();
            if (_msgRecords.TryGetValue(id, out var record))
            {
                if (_msgRecords[id].InComingOverloaded) return false;
                _player = XtremePlayerData.AllPlayerData.FirstOrDefault(x => x.CheatData.ClientData.Id == id)?.Player;
                var timeDiff = currentTime - record.LastReceivedTime;

                if (timeDiff > 1000)
                {
                    record.Count = 1;
                    record.LastReceivedTime = currentTime;
                }
                else
                {
                    record.Count++;
                    Test($"{record.Count} {40}");
                    if (record.Count > 40)
                    {
                        _player?.MarkAsCheater();
                        record.Count = 0;
                        _msgRecords[id].InComingOverloaded = true;
                        Warn($"InComingMsg_Inner Overloaded: {_player?.GetDataName() ?? ""}", "FAC");
                        return false;
                    }
                }
                
                _msgRecords[id] = record;
            }
            else
            {
                _msgRecords[id] = new MsgRecord
                {
                    LastReceivedTime = currentTime,
                    Count = 1,
                };
            }

            return true;
        }
        catch
        {
            _msgRecords = new Dictionary<int, MsgRecord>();
            return !OnGameJoinedPatch.JoinedCompleted;
        }
    }
}
