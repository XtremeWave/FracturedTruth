using System.Collections;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils;
using UnityEngine;

namespace FinalSuspect.Patches.Game_Vanilla;

[HarmonyPatch]
public class MapRealTimeLocationPatch
{
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show)), HarmonyPostfix]
    public static void ShowMapAfter(MapBehaviour __instance, [HarmonyArgument(0)] MapOptions opts)
    {
        XtremeLocalHandling.ShowMap(__instance, opts);
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Awake)), HarmonyPostfix]
    public static void AwakeAfter(MapBehaviour __instance)
    {
        AmongUsClient.Instance.StartCoroutine(CreateTargetRends(__instance));
    }

    private static IEnumerator CreateTargetRends(MapBehaviour mapBehaviour)
    {
        while (XtremePlayerData.AllPlayerData.Count < Main.AllPlayerControls.Count()) yield return null;
        foreach (var data in XtremePlayerData.AllPlayerData)
        {
            var rend = Object.Instantiate(mapBehaviour.HerePoint, mapBehaviour.HerePoint.transform.parent, true);
            rend.gameObject.SetActive(false);
            data.Rend = rend;
            data.Deadbodyrend = Object.Instantiate(rend, rend.transform.parent);
            data.Deadbodyrend.flipY = true;
        }
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.FixedUpdate)), HarmonyPostfix]
    public static void FixedUpdateAfter(MapBehaviour __instance)
    {
        XtremeLocalHandling.UpdateMap();
    }

    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.SetPreMeetingPosition)), HarmonyPostfix]
    public static void SetPreMeetingPositionAfter(MapBehaviour __instance, [HarmonyArgument(0)] Vector3 preMeetingPosition)
    {
        foreach (var data in XtremePlayerData.AllPlayerData)
        {
            if (data.IsDisconnected)continue;
            data.PreMeetingPosition = data.Player.GetTruePosition();
        }
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.GenericShow)), HarmonyPostfix]
    public static void GenericShowAfter(MapBehaviour __instance)
    {
        foreach (var data in XtremePlayerData.AllPlayerData)
        {
            if (data.IsDisconnected)continue;
            data.Rend.material.SetInt(PlayerMaterial.MaskLayer, 255);
        }
    }
    [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Close)), HarmonyPostfix]
    public static void CloseAfter(MapBehaviour __instance)
    {
        foreach (var data in XtremePlayerData.AllPlayerData)
        {
            data.Rend.enabled = true;
        }
    }
}