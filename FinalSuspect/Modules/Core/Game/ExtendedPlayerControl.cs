using System;
using System.Security.Cryptography;
using System.Text;
using AmongUs.GameOptions;
using FinalSuspect.DataHandling.FinalAntiCheat.Core;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Features.CheckingandBlocking;
using InnerNet;
using UnityEngine;

namespace FinalSuspect.Modules.Core.Game;

internal static class ExtendedPlayerControl
{
    #region Identify

    public static bool IsLocalPlayer(this PlayerControl player)
    {
        return PlayerControl.LocalPlayer == player;
    }

    #endregion

    #region ClientData

    public static ClientData GetClient(this PlayerControl player)
    {
        try
        {
            var client = AmongUsClient.Instance.allClients
                .ToArray().FirstOrDefault(cd => cd.Character.PlayerId == player.PlayerId);
            return client;
        }
        catch
        {
            return null;
        }
    }

    public static int GetClientId(this PlayerControl player)
    {
        if (!player) return -1;
        var client = player.GetClient();
        return client?.Id ?? -1;
    }

    #endregion

    #region Role&Name

    public static RoleTypes GetRoleType(this PlayerControl player)
    {
        return Utils.GetRoleType(player.PlayerId);
    }

    public static bool IsImpostor(this PlayerControl pc)
    {
        if (IsLobby) return false;
        return pc.GetRoleType() switch
        {
            RoleTypes.Impostor or RoleTypes.Shapeshifter or RoleTypes.Phantom or RoleTypes.ImpostorGhost => true,
            _ => false
        };
    }

    public static string GetNameWithRole(this PlayerControl player, bool forUser = false)
    {
        var ret = $"{player?.Data?.PlayerName}{(IsInGame ?
            $"({GetRoleName(player.GetRoleType())})" : "")}";
        return forUser ? ret : ret.RemoveHtmlTags();
    }

    public static Color GetRoleColor(this PlayerControl player)
    {
        return Utils.GetRoleColor(player.GetRoleType());
    }

    public static string GetRealName(this PlayerControl player, bool isMeeting = false)
    {
        if (player == null) return null;

        string dataName = null;
        try
        {
            var data = player.GetXtremeData();
            if (data != null)
                dataName = player.GetDataName();
        }
        catch
        {
            /* ignored */
        }

        var realName = isMeeting ? player.Data?.PlayerName : player.name;
        return realName ?? dataName;
    }

    #endregion

    #region Cheat

    public static bool IsFACPlayer(this PlayerControl player)
    {
        return player?.GetClient()?.IsFACPlayer() ?? false;
    }

    public static bool IsBannedPlayer(this PlayerControl player)
    {
        return player?.GetClient()?.IsBannedPlayer() ?? false;
    }

    public static bool IsBannedPlayer(this ClientData player)
    {
        return BanManager.CheckBanStatus(player?.FriendCode, player?.GetHashedPuid());
    }

    public static void MarkAsCheater(this PlayerControl pc)
    {
        pc.GetXtremeData().CheatData.MarkAsCheater();
    }

    public static void MarkAsHacker(this PlayerControl pc)
    {
        pc.GetXtremeData().CheatData.MarkAsHacker();
    }

    public static string GetHashedPuid(this PlayerControl player)
    {
        return player.GetClient().GetHashedPuid();
    }

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

    #endregion

    #region ModClient

    public static bool OtherModClient(this PlayerControl player)
    {
        return Utils.OtherModClient(player.PlayerId) ||
               (player.Data.OwnerId == -2
                && !Utils.IsFinalSuspect(player.PlayerId)
                && !IsFreePlay
                && !IsLocalGame);
    }

    public static bool ModClient(this PlayerControl player)
    {
        return Utils.ModClient(player.PlayerId);
    }

    public static bool IsFinalSuspect(this PlayerControl pc)
    {
        return Utils.IsFinalSuspect(pc.PlayerId);
    }

    #endregion

    #region LocalHandling

    public static string CheckAndGetNameWithDetails(
        this PlayerControl player,
        out Color topcolor,
        out Color bottomcolor,
        out string toptext,
        out string bottomtext,
        bool topswap = false)
    {
        return XtremeLocalHandling.CheckAndGetNameWithDetails(player.PlayerId, out topcolor, out bottomcolor,
            out toptext, out bottomtext,
            topswap);
    }

    public static bool IsHost(this PlayerControl player)
    {
        try
        {
            return AmongUsClient.Instance.GetHost().Id == player.GetClient().Id;
        }
        catch
        {
            return false;
        }
    }

    public static string GetPlatform(this PlayerControl player)
    {
        try
        {
            var color = "";
            var name = "";
            string text;
            switch (player.GetClient().PlatformData.Platform)
            {
                case Platforms.StandaloneEpicPC:
                    color = "#905CDA";
                    name = "Epic";
                    break;
                case Platforms.StandaloneSteamPC:
                    color = "#4391CD";
                    name = "Steam";
                    break;
                case Platforms.StandaloneMac:
                    color = "#e3e3e3";
                    name = "Mac.";
                    break;
                case Platforms.StandaloneWin10:
                    color = "#0078d4";
                    name = GetString("Platform.MicrosoftStore");
                    break;
                case Platforms.StandaloneItch:
                    color = "#E35F5F";
                    name = "Itch";
                    break;
                case Platforms.IPhone:
                    color = "#e3e3e3";
                    name = GetString("Platform.IPhone");
                    break;
                case Platforms.Android:
                    color = "#1EA21A";
                    name = GetString("Platform.Android");
                    break;
                case Platforms.Switch:
                    name = "<color=#00B2FF>Nintendo</color><color=#ff0000>Switch</color>";
                    break;
                case Platforms.Xbox:
                    color = "#07ff00";
                    name = "Xbox";
                    break;
                case Platforms.Playstation:
                    color = "#0014b4";
                    name = "PlayStation";
                    break;
                case Platforms.Unknown:
                default:
                    break;
            }

            if (color != "" && name != "")
                text = $"<color={color}>{name}</color>";
            else
                text = name;
            return text;
        }
        catch
        {
            return "";
        }
    }

    #endregion

    #region XtremePlayerData

    public static XtremePlayerData GetXtremeData(this PlayerControl pc)
    {
        try
        {
            return XtremePlayerData.GetXtremeDataById(pc.PlayerId);
        }
        catch
        {
            try
            {
                return XtremePlayerData.AllPlayerData.FirstOrDefault(data => data.Player == pc);
            }
            catch
            {
                return null;
            }
        }
    }

    public static PlayerCheatData GetCheatData(this PlayerControl pc)
    {
        try
        {
            return GetCheatDataById(pc.PlayerId);
        }
        catch
        {
            try
            {
                return pc.GetXtremeData().CheatData;
            }
            catch
            {
                return null;
            }
        }
    }

    public static bool IsAlive(this PlayerControl pc)
    {
        return pc?.GetXtremeData()?.IsDead == false || !IsInGame;
    }

    public static string GetDataName(this PlayerControl pc)
    {
        try
        {
            return XtremePlayerData.GetPlayerNameById(pc.PlayerId);
        }
        catch
        {
            return null;
        }
    }

    public static string GetColoredName(this PlayerControl pc)
    {
        try
        {
            var data = XtremePlayerData.GetXtremeDataById(pc.PlayerId);
            return StringHelper.ColorString(Palette.PlayerColors[data.ColorId], data.Name);
        }
        catch
        {
            return null;
        }
    }

    public static void SetDead(this PlayerControl pc)
    {
        pc.GetXtremeData().SetDead();
    }

    public static void SetDisconnected(this PlayerControl pc)
    {
        pc.GetXtremeData().SetDisconnected();
        XtremePlayerData.AllPlayerData.Do(_data => _data.AdjustPlayerId());
    }

    public static void SetRole(this PlayerControl pc, RoleTypes role)
    {
        pc.GetXtremeData().SetRole(role);
    }

    public static void SetDeathReason(this PlayerControl pc, VanillaDeathReason deathReason, bool focus = false)
    {
        pc.GetXtremeData().SetDeathReason(deathReason, focus);
    }

    public static void SetRealKiller(this PlayerControl pc, PlayerControl killer)
    {
        if (pc.GetXtremeData().RealKiller != null || !pc.Data.IsDead) return;
        pc.GetXtremeData().SetRealKiller(killer.GetXtremeData());
    }

    public static void SetTaskTotalCount(this PlayerControl pc, int TaskTotalCount)
    {
        pc.GetXtremeData().SetTaskTotalCount(TaskTotalCount);
    }

    public static void OnCompleteTask(this PlayerControl pc)
    {
        pc.GetXtremeData().UpdateProcess();
    }

    #endregion
}