using FinalSuspect.Attributes;
using UnityEngine;

namespace FinalSuspect.Patches.System;

[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
internal class ShipStatusStartPatch
{
    public static void Postfix()
    {
        Info("-----------游戏开始-----------", "Phase");
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameEnd))]
internal class AmongUsClientOnGameEndPatch
{
    public static void Postfix()
    {
        InGame = false;
        Info("-----------游戏结束-----------", "Phase");
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
[HarmonyPriority(Priority.First)]
internal class MeetingHudStartPatch
{
    public static void Prefix()
    {
        Info("------------会议开始------------", "Phase");
    }
}

[HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.OnDestroy))]
internal class MeetingHudOnDestroyPatch
{
    public static void Postfix()
    {
        Info("------------会议结束------------", "Phase");
    }
}

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
internal class CoStartGamePatch
{
    public static void Postfix()
    {
        IntroCutsceneOnDestroyPatch.IntroDestroyed = false;
        GameModuleInitializerAttribute.InitializeAll();
        DestroyableSingleton<LobbyInfoPane>.Instance.gameObject.GetComponent<AspectPosition>().DistanceFromEdge += Vector3.forward * -30;
    }
}

/*[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGameHost))]
internal class CoStartGameHPatch
{
    public static void Prefix()
    {
        foreach (var client in AmongUsClient.Instance.allClients)
        {
            client.IsReady = true;
        }
    }

    public static void Postfix()
    {
        var clientData = GetPlayerById(1).GetXtremeData().CheatData.ClientData;

        AmongUsClient.Instance.SendLateRejection(clientData.Id, DisconnectReasons.ClientTimeout);
        clientData.IsReady = true;
        AmongUsClient.Instance.OnPlayerLeft(clientData, DisconnectReasons.ClientTimeout);
    }
}*/

[HarmonyPatch(typeof(IntroCutscene), nameof(IntroCutscene.OnDestroy))]
public static class IntroCutsceneOnDestroyPatch
{
    public static bool IntroDestroyed;

    public static void Postfix()
    {
        IntroDestroyed = true;
        Info("OnDestroy", "IntroCutscene");
    }
}