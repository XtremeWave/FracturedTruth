using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FinalSuspect.Modules.Core.Game;
using FinalSuspect.Patches.Game_Vanilla;
using InnerNet;

namespace FinalSuspect.Modules.Features.CheckingandBlocking;

public static class BanManager
{
    private static readonly string BAN_LIST_PATH = GetBanFilesPath("BanList.json");
    public static List<string> FACList = [];

    public static string GetHashedPuid(this PlayerControl player)
        => player.GetClient().GetHashedPuid();

    public static string GetHashedPuid(this ClientData player)
    {
        if (player == null) return null;
        var puid = player.ProductUserId;
        if (string.IsNullOrEmpty(puid)) return puid;

        using var sha256 = SHA256.Create();
        var sha256Hash = BitConverter.ToString(sha256.ComputeHash(Encoding.UTF8.GetBytes(puid))).Replace("-", "")
            .ToLower();
        return string.Concat(sha256Hash.AsSpan(0, 5), sha256Hash.AsSpan(sha256Hash.Length - 4));
    }

    public static void AddBanPlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || player == null) return;
        if (player.IsBannedPlayer())
        {
            File.AppendAllText(BAN_LIST_PATH, $"{player.FriendCode},{player.GetHashedPuid()},{player.PlayerName}\n");
            SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
        }
    }

    public static void CheckDenyNamePlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Main.KickPlayerWithDenyName.Value) return;
        try
        {
            using StreamReader sr = new(SpamManager.DENY_NAME_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (line == "") continue;
                if (Main.AllPlayerControls.Any(p => p.IsDev() && line.Contains(p.FriendCode))) continue;
                if (Regex.IsMatch(player.PlayerName, line))
                {
                    KickPlayer(player.Id, false, "DenyName");
                    NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.KickedByDenyName"),
                        player.PlayerName, line));
                    Info($"{player.PlayerName} 因为名字与「{line}」一致而被踢出", "Kick");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            Exception(ex, "CheckDenyNamePlayer");
        }
    }

    public static void CheckBanPlayer(ClientData player)
    {
        if (AmongUsClient.Instance.AmHost)
        {
            if (!Main.KickPlayerInBanList.Value) return;
            if (player.IsBannedPlayer())
            {
                KickPlayer(player.Id, true, "BanList");
                NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.BanedByBanList"),
                    player.PlayerName));
                Info($"{player.PlayerName}因过去已被封禁而被封禁", "BAN");
            }
            else if (player.IsFACPlayer())
            {
                KickPlayer(player.Id, true, "FACList");
                NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.BanedByFACList"),
                    player.PlayerName));
                Info($"{player.PlayerName}存在于FAC封禁名单", "BAN");
            }
        }
    }

    public static bool IsBannedPlayer(this PlayerControl player)
        => player?.GetClient()?.IsBannedPlayer() ?? false;

    public static bool IsBannedPlayer(this ClientData player)
        => CheckBanStatus(player?.FriendCode, player?.GetHashedPuid());

    public static bool CheckBanStatus(string friendCode, string hashedPuid)
    {
        try
        {
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
            using StreamReader sr = new(BAN_LIST_PATH);
            string line;
            while ((line = sr.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!string.IsNullOrWhiteSpace(friendCode) && line.Contains(friendCode)) return true;
                if (!string.IsNullOrWhiteSpace(hashedPuid) && line.Contains(hashedPuid)) return true;
            }
        }
        catch (Exception ex)
        {
            Exception(ex, "CheckBanList");
        }

        return false;
    }

    public static bool IsFACPlayer(this PlayerControl player)
        => player?.GetClient()?.IsFACPlayer() ?? false;

    public static bool IsFACPlayer(this ClientData player)
        => CheckFACStatus(player?.FriendCode, player?.GetHashedPuid());

    public static bool CheckFACStatus(string friendCode, string hashedPuid)
        => FACList.Any(line =>
            !string.IsNullOrWhiteSpace(friendCode) && line.Contains(friendCode) ||
            !string.IsNullOrWhiteSpace(hashedPuid) && line.Contains(hashedPuid));
}

[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.Select))]
class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        var recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (recentClient.IsBannedPlayer())
            __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
    }
}