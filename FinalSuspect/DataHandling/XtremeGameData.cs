using System;
using AmongUs.GameOptions;
using FinalSuspect.Modules.Core.Game;
using InnerNet;

namespace FinalSuspect.DataHandling;

public static partial class XtremeGameData
{
    public static bool OtherModClient(this PlayerControl player)
    {
        return OtherModClient(player.PlayerId) ||
               (player.Data.OwnerId == -2
                && !IsFinalSuspect(player.PlayerId)
                && !IsFreePlay);
    }

    public static bool OtherModClient(byte id)
    {
        return GetPlayerVersion(id, out var ver) && Main.ForkId != ver.forkId;
    }

    public static bool ModClient(this PlayerControl player)
    {
        return ModClient(player.PlayerId);
    }

    public static bool ModClient(byte id)
    {
        return GetPlayerVersion(id, out _);
    }

    public static bool IsFinalSuspect(this PlayerControl pc)
    {
        return IsFinalSuspect(pc.PlayerId);
    }

    public static bool IsFinalSuspect(byte id)
    {
        return PlayerVersion.playerVersion.TryGetValue(id, out var ver) && Main.ForkId == ver.forkId;
    }

    public static class GameStates
    {
        private static bool InGame { get; set; }
        private static bool InMeeting { get; set; }

        public static bool OtherModHost
        {
            get
            {
                try
                {
                    return Main.AllPlayerControls.ToArray().FirstOrDefault(x => x.IsHost()
                        && !PlayerControl.LocalPlayer.IsHost()
                        && x.OtherModClient()
                        && !IsFreePlay);
                }
                catch
                {
                    return false;
                }
            }
        }

        public static bool ModHost => Main.AllPlayerControls.ToArray().FirstOrDefault(x =>
            x.IsHost() && x.ModClient());

        public static bool IsLobby =>
            AmongUsClient.Instance.GameState == InnerNetClient.GameStates.Joined && !IsFreePlay;

        public static bool IsInGame => InGame || IsFreePlay;
        public static bool IsNotJoined => AmongUsClient.Instance.GameState == InnerNetClient.GameStates.NotJoined;
        public static bool IsOnlineGame => AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame;
        public static bool IsLocalGame => AmongUsClient.Instance.NetworkMode == NetworkModes.LocalGame;
        public static bool IsFreePlay => AmongUsClient.Instance.NetworkMode == NetworkModes.FreePlay;
        public static bool IsInTask => IsInGame && !MeetingHud.Instance;
        public static bool IsInMeeting => IsInGame && MeetingHud.Instance && InMeeting;

        public static bool IsCountDown => GameStartManager.InstanceExists &&
                                          GameStartManager.Instance.startState ==
                                          GameStartManager.StartingStates.Countdown;

        public static bool IsShip => ShipStatus.Instance;
        public static bool IsCanMove => PlayerControl.LocalPlayer?.CanMove is true;
        public static bool IsDead => PlayerControl.LocalPlayer?.Data?.IsDead is true && !IsLobby;

        public static bool IsNormalGame =>
            GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.Normal or GameModes.NormalFools;

        public static bool IsHideNSeek =>
            GameOptionsManager.Instance.CurrentGameOptions.GameMode is GameModes.HideNSeek or GameModes.SeekFools;

        public static bool IsVanillaServer
        {
            get
            {
                if (!IsOnlineGame) return false;
                const string Domain = "among.us";
                //Reactor.gg
                return ServerManager.Instance.CurrentRegion?.TryCast<StaticHttpRegionInfo>() is { } regionInfo &&
                       regionInfo.PingServer.EndsWith(Domain, StringComparison.Ordinal) &&
                       regionInfo.Servers.All(serverInfo => serverInfo.Ip.EndsWith(Domain, StringComparison.Ordinal));
            }
        }

        public static void UpdateGameState_IsInGame(bool inGame)
        {
            InGame = inGame;
        }

        public static void UpdateGameState_IsInMeeting(bool inMeeting)
        {
            InMeeting = inMeeting;
        }

        public static bool MapIsActive(MapNames name)
        {
            return (MapNames)GameOptionsManager.Instance.CurrentGameOptions.MapId == name;
        }
    }
}

public enum VanillaDeathReason
{
    None,
    Exile,
    Kill,
    Disconnect
}