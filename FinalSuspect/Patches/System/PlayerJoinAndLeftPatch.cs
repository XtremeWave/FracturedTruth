using AmongUs.Data;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using FinalSuspect.Modules.Features.CheckingandBlocking;
using FinalSuspect.Patches.Game_Vanilla;
using InnerNet;

namespace FinalSuspect.Patches.System;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
public class OnGameJoinedPatch
{
    public static bool JoinedCompleted;

    public static void Postfix(AmongUsClient __instance)
    {
        HudManagerPatch.Init();

        Info($"{__instance.GameId} 加入房间", "OnGameJoined");
        XtremeGameData.PlayerVersion.playerVersion = new Dictionary<byte, XtremeGameData.PlayerVersion>();
        SoundManager.Instance.ChangeAmbienceVolume(DataManager.Settings.Audio.AmbienceVolume);
        XtremePlayerData.InitializeAll();
        UpdateGameState_IsInGame(false);
        UpdateGameState_IsInMeeting(false);
        ErrorText.Instance.Clear();
        ServerAddManager.SetServerName();
        JoinedCompleted = false;
        Init_FAC();

        _ = new LateTask(() => { JoinedCompleted = true; }, 4f, "SyncJoined");

        if (AmongUsClient.Instance.AmHost) GameStartManagerPatch.GameStartManagerUpdatePatch.exitTimer = -1;
        //Main.NewLobby = true;
    }
}

[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
internal class DisconnectInternalPatch
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
    public static void Postfix(AmongUsClient __instance, [HarmonyArgument(0)] ClientData client)
    {
        Info($"{client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}) 加入房间", "Session");

        BanManager.CheckFriendCode(client);
        BanManager.CheckBanPlayer(client);
        BanManager.CheckDenyNamePlayer(client);
        KickUnspawnedPlayers(client);
    }

    private static void KickUnspawnedPlayers(ClientData client)
    {
        _ = new LateTask(() =>
        {
            try
            {
                if (!AmongUsClient.Instance.AmHost || AmongUsClient.Instance.allClients.Contains(client) ||
                    !client.Character.Data.IsIncomplete) return;
                SendInGame(GetString("Warning.InvalidColor") +
                           $" {client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode})");
                AmongUsClient.Instance.KickPlayer(client.Id, false);
                Info(
                    $"Kicked {client.PlayerName}(ClientID:{client.Id}/FriendCode:{client.FriendCode}) due to it was unspawned",
                    "OnPlayerJoinedPatchPostfix");
                return;
            }
            catch
            {
                /* ignored */
            }
        }, 4.5f, "Kick Unspawned Players");
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
internal class OnPlayerLeftPatch
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
            var id = data.ColorId;
            var color = Palette.PlayerColors[id];
            var name = StringHelper.ColorString(color, data.PlayerName);

            // 附加描述掉线原因
            switch (reason)
            {
                case DisconnectReasons.Hacking:
                    NotificationPopperPatch.NotificationPop(
                        string.Format(GetString("Notification.PlayerLeftByAU-Anticheat"), name));
                    break;
                case DisconnectReasons.Error:
                    NotificationPopperPatch.NotificationPop(string.Format(GetString("Notification.PlayerLeftCuzError"),
                        name));
                    break;
                case DisconnectReasons.Kicked:
                case DisconnectReasons.Banned:
                    break;
                case DisconnectReasons.ClientTimeout:
                    NotificationPopperPatch.NotificationPop(
                        string.Format(GetString("Notification.PlayerLeftCuzTimeout"), name));
                    break;
                default:
                    if (!ClientsProcessed.Contains(data.Id))
                        NotificationPopperPatch.NotificationPop(string.Format(GetString("Notification.PlayerLeft"),
                            name));
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