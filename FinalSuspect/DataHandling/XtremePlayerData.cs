using System;
using AmongUs.GameOptions;
using FinalSuspect.Attributes;
using FinalSuspect.DataHandling.FinalAntiCheat.Core;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using UnityEngine;

namespace FinalSuspect.DataHandling;

public class XtremePlayerData : IDisposable
{
    #region PLAYER_INFO

    public static List<XtremePlayerData> AllPlayerData;
    public PlayerControl Player { get; private set; }

    public string Name { get; private set; }
    public int ColorId { get; private set; }
    public byte PlayerId { get; private set; }
    public uint NetId { get; private set; }

    public bool IsImpostor { get; private set; }
    public bool IsDead { get; private set; }
    public bool DeathByDisconnected => RealDeathReason == VanillaDeathReason.Disconnect;
    public bool IsDisconnected { get; private set; }

    public RoleTypes? RoleWhenAlive { get; private set; }
    public RoleTypes? RoleAfterDeath { get; private set; }
    public bool RoleAssgined { get; private set; }

    public VanillaDeathReason RealDeathReason { get; private set; }
    public XtremePlayerData RealKiller { get; private set; }

    public int ProcessInt { get; private set; }
    public int TotalTaskCount { get; private set; }
    public bool TaskCompleted => TotalTaskCount == ProcessInt;

    public PlayerCheatData CheatData { get; private set; }

    private XtremePlayerData(PlayerControl player, string playername, int colorid)
    {
        Player = player;
        Name = playername;
        ColorId = colorid;
        CheatData = new PlayerCheatData(player);
        PlayerId = player.PlayerId;
        NetId = player.NetId;
        IsImpostor = IsDead = RoleAssgined = false;
        ProcessInt = TotalTaskCount = 0;
        RealDeathReason = VanillaDeathReason.None;
        RealKiller = null;
    }

    public SpriteRenderer Rend { get; set; }
    public SpriteRenderer Deadbodyrend { get; set; }
    public Vector3? PreMeetingPosition { get; set; }

    #endregion

    ///////////////FUNCTIONS\\\\\\\\\\\\\\\

    public static XtremePlayerData GetXtremeDataById(byte id)
    {
        try
        {
            return AllPlayerData.FirstOrDefault(data => data.PlayerId == id);
        }
        catch
        {
            return null;
        }
    }

    public static PlayerControl GetPlayerById(byte id) => GetXtremeDataById(id).Player;
    public static string GetPlayerNameById(byte id) => GetXtremeDataById(id).Name;

    public static RoleTypes GetRoleById(byte id)
    {
        var data = GetXtremeDataById(id);
        var dead = data?.IsDead ?? false;
        RoleTypes nullrole;

        if (dead && !IsFreePlay)
        {
            nullrole = data.IsImpostor ? RoleTypes.ImpostorGhost : RoleTypes.CrewmateGhost;
        }
        else
        {
            nullrole = GetPlayerById(id).Data.Role.Role;
        }

        var role = (dead ? data.RoleAfterDeath : data?.RoleWhenAlive) ?? nullrole;
        return role;
    }

    public void AdjustPlayerId()
    {
        PlayerId = Player.PlayerId;
    }

    public static int GetLongestNameByteCount() => AllPlayerData.Select(data => data.Name.GetByteCount())
        .OrderByDescending(byteCount => byteCount).FirstOrDefault();

    public void SetName(string name) => Name = name;

    public void SetDead()
    {
        IsDead = true;
        Info($"Set Death For {Player.GetNameWithRole()}", "Data");
    }

    public void SetDisconnected()
    {
        if (IsLobby)
        {
            Dispose();
            AllPlayerData.Remove(this);
            return;
        }

        Info($"Set Disconnect For {Player.GetNameWithRole()}", "Data");
        IsDisconnected = true;
        SetDead();
        SetDeathReason(VanillaDeathReason.Disconnect);
    }

    public void SetRole(RoleTypes role)
    {
        if (!RoleAssgined)
        {
            RoleWhenAlive = role;
            SetAsImp(IsImpostor(role));
        }
        else
        {
            SetDead();
            RoleAfterDeath = role;
        }

        RoleAssgined = !IsFreePlay;
        Info("Set Role For Player: " + Name + " => " + role, "SetRole");
    }

    public void SetDeathReason(VanillaDeathReason deathReason, bool focus = false)
    {
        if (IsDead && RealDeathReason == VanillaDeathReason.None || focus)
            RealDeathReason = deathReason;
        Info($"Set Death Reason For {Player.GetNameWithRole()}; Death Reason: {deathReason}", "Data");
    }

    public void SetRealKiller(XtremePlayerData killer)
    {
        SetDead();
        SetDeathReason(VanillaDeathReason.Kill);
        killer.UpdateProcess();
        RealKiller = killer;
        Info($"Set Real Killer For {Player.GetNameWithRole()}, Killer: {killer.Player.GetNameWithRole()}", "Data");
    }

    public void SetTaskTotalCount(int count) => TotalTaskCount = count;
    public void UpdateProcess() => ProcessInt++;
    private void SetAsImp(bool isimp) => IsImpostor = isimp;

    [GameModuleInitializer]
    public static void InitializeAll()
    {
        DisposeAll();
        AllPlayerData = [];
        if (IsFreePlay)
        {
            foreach (var data in GameData.Instance.AllPlayers)
            {
                CreateDataFor(data.Object);
            }
        }
        else
        {
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                CreateDataFor(pc);
            }
        }
    }

    public static void CreateDataFor(PlayerControl player, string playername = null)
    {
        try
        {
            var colorId = player.Data.DefaultOutfit.ColorId;
            playername ??= player.GetRealName();

            AllPlayerData.Add(new XtremePlayerData(player, playername, colorId));
            Info(
                $"Creating XtremePlayerData For {player.GetClient().PlayerName ?? "Playername null"}({player.GetClient().FriendCode ?? "Friendcode null"})",
                "Data");
        }
        catch
        {
            /* ignored */
        }
    }
#pragma warning disable CA1816
    public void Dispose()
    {
        Info($"Disposing XtremePlayerData For {Name}", "Data");
        Player = null;
        CheatData = null;
        Name = null;
        ColorId = -1;
        IsImpostor = IsDead = RoleAssgined = false;
        ProcessInt = TotalTaskCount = -1;
        RealDeathReason = VanillaDeathReason.None;
        RealKiller = null;
        Deadbodyrend = Rend = null;
        PreMeetingPosition = null;
    }

    public static void DisposeAll()
    {
        try
        {
            AllPlayerData.Do(data => data.Dispose());
            AllPlayerData.Clear();
        }
        catch
        {
            /* ignored */
        }
    }
}
#pragma warning restore CA1816

public static class XtremePlayerDataExtensions
{
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

    public static bool IsAlive(this PlayerControl pc) => pc?.GetXtremeData()?.IsDead == false || !IsInGame;

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

    public static void SetDead(this PlayerControl pc) => pc.GetXtremeData().SetDead();

    public static void SetDisconnected(this PlayerControl pc)
    {
        pc.GetXtremeData().SetDisconnected();
        XtremePlayerData.AllPlayerData.Do(_data => _data.AdjustPlayerId());
    }

    public static void SetRole(this PlayerControl pc, RoleTypes role) => pc.GetXtremeData().SetRole(role);

    public static void SetDeathReason(this PlayerControl pc, VanillaDeathReason deathReason, bool focus = false)
        => pc.GetXtremeData().SetDeathReason(deathReason, focus);

    public static void SetRealKiller(this PlayerControl pc, PlayerControl killer)
    {
        if (pc.GetXtremeData().RealKiller != null || !pc.Data.IsDead) return;
        pc.GetXtremeData().SetRealKiller(killer.GetXtremeData());
    }

    public static void SetTaskTotalCount(this PlayerControl pc, int TaskTotalCount) =>
        pc.GetXtremeData().SetTaskTotalCount(TaskTotalCount);

    public static void OnCompleteTask(this PlayerControl pc) => pc.GetXtremeData().UpdateProcess();
}