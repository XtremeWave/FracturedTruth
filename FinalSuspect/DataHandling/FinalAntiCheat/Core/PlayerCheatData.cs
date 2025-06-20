using System;
using FinalSuspect.Modules.Core.Game;
using FinalSuspect.Modules.Features.CheckingandBlocking;
using FinalSuspect.Patches.Game_Vanilla;
using InnerNet;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

public class PlayerCheatData
{
    public bool IsSuspectCheater { get; private set; }
    public ClientData ClientData { get; private set; }
    public string FriendCode => ClientData?.FriendCode ?? string.Empty;
    public string Puid => ClientData?.GetHashedPuid() ?? string.Empty;
    public bool InComingOverloaded { get; private set; }

    private readonly PlayerControl _player;

    public PlayerCheatData(PlayerControl player)
    {
        _player = player;
        ClientData = _player.GetClient();
    }

    public void MarkAsCheater()
    {
        if (IsSuspectCheater) return;
        IsSuspectCheater = true;
        Warn($"Suspect Cheater: {_player.GetXtremeData().Name}," +
             $"FriendCode: {FriendCode}," +
             $"Puid: {Puid},",
            "FAC");
    }

    private void HandleLobbyPosition()
    {
        if (!IsLobby) return;
        var pos = _player.GetTruePosition();
        if (pos.x > 3.5f || pos.x < -3.5f || pos.y > 4f || pos.y < -1f)
            MarkAsCheater();
    }

    private void HandleBan()
    {
        if (ClientData.IsFACPlayer() || ClientData.IsBannedPlayer())
            MarkAsCheater();
    }

    private void HandleSuspectCheater()
    {
        if (Main.DisableFAC.Value || !IsSuspectCheater ||
            _lastHandleCheater != -1 && _lastHandleCheater + 1 >= GetTimeStamp()) return;
        _lastHandleCheater = GetTimeStamp();
        if (!AmongUsClient.Instance.AmHost)
        {
            NotificationPopperPatch.NotificationPop(string.Format(GetString("CheatDetected.Cheater_NotHost"),
                _player.GetColoredName()));
            return;
        }
        
        KickPlayer(_player.PlayerId, false, "Cheater", KickLevel.Warning);
    }

    private readonly Dictionary<byte, RpcRecord> _rpcRecords = new();

    private struct RpcRecord
    {
        public long LastReceivedTime;
        public int Count;
        public int MaxiCount;
    }

    public bool HandleIncomingRpc(byte rpcId)
    {
        if (InComingOverloaded) return true;
        var currentTime = GetCurrentTimestamp();

        if (_rpcRecords.TryGetValue(rpcId, out var record))
        {
            var timeDiff = currentTime - record.LastReceivedTime;

            if (timeDiff > 1000)
            {
                record.Count = 1;
                record.LastReceivedTime = currentTime;
            }
            else
            {
                record.Count++;

                if (record.Count > record.MaxiCount)
                {
                    MarkAsCheater();
                    record.Count = 0;
                    InComingOverloaded = true;
                    Warn($"InComingRpc Overloaded: {_player.GetDataName()}", "FAC");
                    return true;
                }
            }

            Test($"{record.Count} {record.MaxiCount}");
            _rpcRecords[rpcId] = record;
        }
        else
        {
            _rpcRecords[rpcId] = new RpcRecord
            {
                LastReceivedTime = currentTime,
                Count = 1,
                MaxiCount = _handlers.FirstOrDefault(x => x.TargetRpcs.Contains(rpcId))?.Handlers.FirstOrDefault()?.MaxiReceivedNumPerSecond() ?? 5
            };
        }

        return false;
    }

    public void HandleCheatData()
    {
        try
        {
            ClientData ??= _player.GetClient();
            HandleBan();
            HandleLobbyPosition();
            HandleSuspectCheater();
        }
        catch
        {
            /* ignored */
        }
    }
}