using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmongUs.Data;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using FinalSuspect.Modules.Features.CheckingandBlocking;
using FinalSuspect.Patches.Game_Vanilla;
using InnerNet;

namespace FinalSuspect.Patches.System;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
internal class OnGameJoinedPatch
{
    public static void Postfix(AmongUsClient __instance)
    {
        HudManagerPatch.Init();

        Info($"{__instance.GameId} 加入房间", "OnGameJoined");
        XtremeGameData.PlayerVersion.playerVersion = new Dictionary<byte, XtremeGameData.PlayerVersion>();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
        XtremePlayerData.InitializeAll();
        RPC.RpcVersionCheck();
        InGame = false;
        ErrorText.Instance.Clear();
        ServerAddManager.SetServerName();

        Init();
        if (AmongUsClient.Instance.AmHost)
        {
            GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
            //Main.NewLobby = true;
        }
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
class DisconnectInternalPatch
{
    public static void Prefix(InnerNetClient __instance, DisconnectReasons reason, string stringReason)
    {
        try
        {
            ShowDisconnectPopupPatch.Reason = reason;
            ShowDisconnectPopupPatch.StringReason = stringReason;

            Info($"断开连接(理由:{reason}:{stringReason}，Ping:{__instance.Ping})", "Session");
            HudManagerPatch.Init();
            XtremePlayerData.DisposeAll();

            ErrorText.Instance.CheatDetected = false;
            ErrorText.Instance.SBDetected = false;
            ErrorText.Instance.Clear();
            //Cloud.StopConnect();
        }
        catch
        {
            /* ignored */
        }
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
public class OnPlayerJoinedPatch
{
    private static readonly Regex ValidFormatRegex = new(
        @"^[a-z]{7,10}#\d{4}$",
        RegexOptions.Compiled
    );

    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        Info($"{client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}) 加入房间", "Session");
        if (AmongUsClient.Instance.AmHost && Main.KickPlayerWhoFriendCodeNotExist.Value)
        {
            //用于检测是否为xxx#1145/xxx#1337的重复代码前缀
            //InnerSloth的好友代码不会出现前端重复 如果有前端重复一定是UE或者SM黑客
            var currentPrefixes = AmongUsClient.Instance.allClients
                .ToArray()
                .Where(c => c.Id != client.Id)
                .Select(c =>
                {
                    if (string.IsNullOrEmpty(c.FriendCode)) return null;
                    var parts = c.FriendCode.Split('#');
                    return parts.Length > 0 ? parts[0].ToLowerInvariant() : null;
                })
                .Where(prefix => !string.IsNullOrEmpty(prefix))
                .ToHashSet();

            var newPrefixParts = client.FriendCode.Split('#');
            if (newPrefixParts.Length < 1)
            {
                KickPlayer(client.Id, false, "NotLogin");
                return;
            }

            var newPrefix = newPrefixParts[0].ToLowerInvariant();
            if (currentPrefixes.Contains(newPrefix))
            {
                KickPlayer(client.Id, false, "NotLogin");
                NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.KickedByNoFriendCode"),
                    client.PlayerName));
                Info($"重复好友代码前缀的玩家 {client.PlayerName} 已被踢出", "Kick");
                return;
            }
        }

        if (AmongUsClient.Instance.AmHost && client.FriendCode == "" && Main.KickPlayerWhoFriendCodeNotExist.Value)
        {
            KickPlayer(client.Id, false, "NotLogin");
            NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.KickedByNoFriendCode"),
                client.PlayerName));
            Info($"没有好友代码的玩家 {client.PlayerName} 已被踢出。", "Kick");
        }

        if (DestroyableSingleton<FriendsListManager>.Instance.IsPlayerBlockedUsername(client.FriendCode) &&
            AmongUsClient.Instance.AmHost && Main.KickPlayerInBanList.Value)
        {
            KickPlayer(client.Id, true, "BanList");
            Info($"已封禁的玩家 {client.PlayerName} ({client.FriendCode})", "BAN");
        }

        if (AmongUsClient.Instance.AmHost && !ValidFormatRegex.IsMatch(client.FriendCode) && client.FriendCode != "")
        {
            KickPlayer(client.Id, false, "NotLogin");
            NotificationPopperPatch.NotificationPop(string.Format(GetString("Warning.Cheater"), client.PlayerName));
            Info($"没有好友代码的玩家 {client.PlayerName} 已被踢出。", "Kick");
        }

        BanManager.CheckBanPlayer(client);
        BanManager.CheckDenyNamePlayer(client);

        RPC.RpcVersionCheck();
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
class OnPlayerLeftPatch
{
    public static readonly List<int> ClientsProcessed = [];

    public static void Add(int id)
    {
        ClientsProcessed.Remove(id);
        ClientsProcessed.Add(id);
    }

    public static void Postfix([HarmonyArgument(0)] ClientData data, [HarmonyArgument(1)] DisconnectReasons reason)
    {
        try
        {
            if (data == null)
            {
                Error("错误的客户端数据：数据为空", "Session");
                return;
            }

            data.Character?.SetDisconnected();

            Info(
                $"{data.PlayerName}(ClientID:{data.Id}/FriendCode:{data.FriendCode})断开连接(理由:{reason}，Ping:{AmongUsClient.Instance.Ping})",
                "Session");
            var id = data.Character?.Data?.DefaultOutfit?.ColorId ?? XtremePlayerData.AllPlayerData
                .Where(playerData => playerData.CheatData.ClientData.Id == data.Id).FirstOrDefault()!.ColorId;
            var color = Palette.PlayerColors[id];
            var name = StringHelper.ColorString(color, data.PlayerName);
            // 附加描述掉线原因
            switch (reason)
            {
                case DisconnectReasons.Hacking:
                    NotificationPopperPatch.NotificationPop(string.Format(GetString("PlayerLeftByAU-Anticheat"), name));
                    break;
                case DisconnectReasons.Error:
                    NotificationPopperPatch.NotificationPop(string.Format(GetString("PlayerLeftCuzError"), name));
                    break;
                case DisconnectReasons.Kicked:
                case DisconnectReasons.Banned:
                    break;
                case DisconnectReasons.ExitGame:
                    NotificationPopperPatch.NotificationPop(string.Format(GetString("PlayerLeft"), name));
                    break;
                case DisconnectReasons.ClientTimeout:
                    NotificationPopperPatch.NotificationPop(string.Format(GetString("PlayerLeftCuzTimeout"), name));
                    break;
                default:
                    if (!ClientsProcessed.Contains(data.Id))
                        NotificationPopperPatch.NotificationPop(string.Format(GetString("PlayerLeft"), name));
                    break;
            }

            Dispose(data.Character?.PlayerId ?? 255);

            XtremeGameData.PlayerVersion.playerVersion.Remove(data.Character?.PlayerId ?? 255);
            ClientsProcessed.Remove(data.Id);
        }
        catch
        {
            /* ignored */
        }
    }
}