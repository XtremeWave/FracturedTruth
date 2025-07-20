using AmongUs.GameOptions;
using FracturedTruth.DataHandling;
using FracturedTruth.DataHandling.FinalAntiCheat.Core;
using FracturedTruth.DataHandling.XtremeGameData;
using FracturedTruth.Helpers;
using UnityEngine;

namespace FracturedTruth.Modules.Core.Game.PlayerControlExtension;

public static class _Data
{
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

    public static XtremePlayerData GetXtremeData(this PlayerControl pc)
    {
        try
        {
            return GetXtremeDataById(pc.PlayerId);
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

    public static string GetDataName(this PlayerControl pc)
    {
        try
        {
            return GetPlayerNameById(pc.PlayerId);
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
            var data = GetXtremeDataById(pc.PlayerId);
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
}