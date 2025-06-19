using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FinalSuspect.Modules.Core.Game;
using FinalSuspect.Patches.Game_Vanilla;
using InnerNet;

namespace FinalSuspect.Modules.Features.CheckingandBlocking;

public static class BanManager
{
    public static readonly List<string> FACList = [];

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
        if (!player.IsBannedPlayer())
        {
            File.AppendAllText(BAN_LIST_PATH, $"{player?.FriendCode},{player?.GetHashedPuid()},{player.PlayerName}\n");
            SendInGame(string.Format(GetString("Message.AddedPlayerToBanList"), player.PlayerName));
        }
        else Info($"{player?.FriendCode},{player?.GetHashedPuid()},{player.PlayerName} 已经被加入封禁名单", "AddBanPlayer");
    }

    public static void CheckDenyNamePlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost || !Main.KickPlayerWithDenyName.Value) return;
        try
        {
            using StreamReader sr = new(DENY_NAME_LIST_PATH);
            while (sr.ReadLine() is { } line)
            {
                if (line == "") continue;
                if (Main.AllPlayerControls.Any(p => p.IsDev() && line.Contains(p.FriendCode))) continue;
                if (!Regex.IsMatch(player.PlayerName, line)) continue;
                KickPlayer(player.Id, false, "Using denied name");
                NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.KickedByDenyName"),
                    player.PlayerName));
                return;
            }
        }
        catch (Exception ex)
        {
            Exception(ex, "CheckDenyNamePlayer");
        }
    }

    public static void CheckBanPlayer(ClientData player)
    {
        if (!AmongUsClient.Instance.AmHost && !Main.KickPlayerInBanList.Value) return;
        if (player.IsBannedPlayer())
        {
            KickPlayer(player.Id, true, "Player in Banlist");
            NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.BanedByBanList"),
                player.PlayerName));
        }
        else if (player.IsFACPlayer())
        {
            KickPlayer(player.Id, true, "Player in FACList");
            NotificationPopperPatch.NotificationPop(string.Format(GetString("Message.BanedByFACList"),
                player.PlayerName));
        }
    }

    public static bool IsBannedPlayer(this PlayerControl player)
        => player?.GetClient()?.IsBannedPlayer() ?? false;

    public static bool IsBannedPlayer(this ClientData player)
        => CheckBanStatus(player?.FriendCode, player?.GetHashedPuid());

    private static bool CheckBanStatus(string friendCode, string hashedPuid)
    {
        try
        {
            if (!File.Exists(BAN_LIST_PATH)) File.Create(BAN_LIST_PATH).Close();
            using StreamReader sr = new(BAN_LIST_PATH);
            while (sr.ReadLine() is { } line)
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
internal class BanMenuSelectPatch
{
    public static void Postfix(BanMenu __instance, int clientId)
    {
        var recentClient = AmongUsClient.Instance.GetRecentClient(clientId);
        if (recentClient == null) return;
        if (recentClient.IsBannedPlayer())
            __instance.BanButton.GetComponent<ButtonRolloverHandler>().SetEnabledColors();
    }
}