using System;
using AmongUs.GameOptions;
using FinalSuspect.Attributes;
using FinalSuspect.DataHandling.FinalAntiCheat.Core;
using FinalSuspect.DataHandling.XtremeGameData;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using FinalSuspect.Modules.Core.Game.PlayerControlExtension;
using UnityEngine;

namespace FinalSuspect.DataHandling;

public class XtremePlayerData : IDisposable
{
    ///////////////FUNCTIONS\\\\\\\\\\\\\\\

    public void AdjustPlayerId()
    {
        PlayerId = Player.PlayerId;
    }

    public void SetName(string name)
    {
        Name = name;
    }

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
        if ((IsDead && RealDeathReason == VanillaDeathReason.None) || focus)
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

    public void SetTaskTotalCount(int count)
    {
        TotalTaskCount = count;
    }

    public void UpdateProcess()
    {
        ProcessInt++;
    }

    private void SetAsImp(bool isimp)
    {
        IsImpostor = isimp;
    }

    [GameModuleInitializer]
    public static void InitializeAll()
    {
        DisposeAll();
        AllPlayerData = [];
        if (IsFreePlay)
            foreach (var data in GameData.Instance.AllPlayers)
                CreateDataFor(data.Object);
        else
            foreach (var pc in PlayerControl.AllPlayerControls)
                CreateDataFor(pc);
    }

    public static void CreateDataFor(PlayerControl player, string playername = null)
    {
        try
        {
            var colorId = player.Data.DefaultOutfit.ColorId;
            playername ??= player.GetRealName();
            playername = playername.TrimEnd();
            playername = playername.Replace(" ", "_");

            var existingNames = new HashSet<string>(AllPlayerData.Select(data => data.Name));
            var baseName = playername;
            var suffix = 0;

            while (existingNames.Contains(playername))
            {
                suffix++;
                playername = $"{baseName}_{suffix}";

                if (suffix <= 1000) continue;
                playername = $"{baseName}_{Guid.NewGuid().ToString("N")[..4]}";
                break;
            }

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
    public SpriteRenderer Rend_DeadBody { get; set; }
    public Vector3? PreMeetingPosition { get; set; }

    #endregion

#pragma warning disable CA1816
    public void Dispose()
    {
        Info($"Disposing XtremePlayerData For {Name}", "Data");
        Player = null;
        CheatData.Dispose();
        CheatData = null;
        Name = null;
        ColorId = -1;
        IsImpostor = IsDead = RoleAssgined = false;
        ProcessInt = TotalTaskCount = -1;
        RealDeathReason = VanillaDeathReason.None;
        RealKiller = null;
        Rend_DeadBody = Rend = null;
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