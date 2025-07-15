using AmongUs.GameOptions;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game.PlayerControlExtension;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace FinalSuspect.Patches.System;

[HarmonyPatch]
public class TaskProgressTrackerPatch
{
    private static string TitleText;

    [HarmonyPatch(typeof(ProgressTracker), nameof(ProgressTracker.Start))]
    [HarmonyPostfix]
    public static void ProgressTracker_Start(ProgressTracker __instance)
    {
        TitleText = __instance.gameObject.transform.FindChild("TitleText_TMP").GetComponent<TextMeshPro>().text;
    }

    [HarmonyPatch(typeof(ProgressTracker), nameof(ProgressTracker.FixedUpdate))]
    [HarmonyPostfix]
    public static void ProgressTracker_FixedUpdate(ProgressTracker __instance)
    {
        __instance.TileParent.material.SetColor(399, Color.blue);
        if (!IsInGame) return;
        var instance = GameData.Instance;
        var percentage = instance.CompletedTasks / (float)instance.TotalTasks * 100f;
        var lastPercentage = 0f;
        var data = PlayerControl.LocalPlayer.GetXtremeData();
        switch (GameManager.Instance.LogicOptions.GetTaskBarMode())
        {
            case TaskBarMode.Normal:
                break;
            case TaskBarMode.MeetingOnly:
                if (!MeetingHud.Instance)
                    goto End;
                break;
            case TaskBarMode.Invisible:
            default:
                goto End;
        }

        lastPercentage = percentage;
        End:

        __instance.TileParent.material.SetColor(399,
            data.IsImpostor
                ? ColorHelper.GetColorByPercentage(lastPercentage)
                : PlayerControl.LocalPlayer.GetRoleColor());

        var tmp = __instance.gameObject.transform.FindChild("TitleText_TMP").GetComponent<TextMeshPro>();
        tmp.text = $"{TitleText}({lastPercentage:F1}%)";
        tmp.fontStyle = FontStyles.Bold;
        if (data.IsImpostor) return;
        var comms = IsActive(SystemTypes.Comms);
        var NormalColor = data.TaskCompleted ? Color.green : Color.yellow;
        var TextColor = comms ? Color.gray : NormalColor;
        tmp.color = TextColor;
        if (comms)
            tmp.text = $"{TitleText}(??.?%)";
    }
}