using AmongUs.GameOptions;
using FinalSuspect.Modules.Core.Game;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace FinalSuspect.Patches.Game_Vanilla;

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
class MurderPlayerPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
    {
        target.SetRealKiller(__instance);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CoSetRole))]
class CoSetRolePatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleTypes)
    {
        try
        {
            __instance.SetRole(roleTypes);
        }
        catch
        {
            /* ignored */
        }
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
class PlayerStartPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var topText = Object.Instantiate(__instance.cosmetics.nameText, __instance.cosmetics.nameText.transform, true);
        var bottomText = Object.Instantiate(__instance.cosmetics.nameText, __instance.cosmetics.nameText.transform, true);
        topText.transform.localPosition = bottomText.transform.localPosition = new Vector3(0f, 0.2f, 0f);
        topText.transform.localScale = bottomText.transform.localScale = new Vector3(1f, 1f, 1f);
        topText.fontSize = bottomText.fontSize = Main.RoleTextSize;
        topText.text = topText.gameObject.name = "TopText";
        bottomText.text = bottomText.gameObject.name = "BottomText";
        topText.enabled = bottomText.enabled = false;
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetTasks))]
class PlayerControlSetTasksPatch
{
    public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] List<NetworkedPlayerInfo.TaskInfo> tasks)
    {
        // 自由模式假人处理
        if (__instance.GetXtremeData() == null)
            XtremePlayerData.CreateDataFor(__instance);
        __instance.SetTaskTotalCount(tasks.Count);
    }
}

[HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
class PlayerControlCompleteTaskPatch
{
    public static void Postfix(PlayerControl __instance)
    {
        var pc = __instance;
        Info($"TaskComplete:{pc.GetNameWithRole()}", "CompleteTask");
        pc.OnCompleteTask();

        GameData.Instance.RecomputeTaskCounts();
        Info($"TotalTaskCounts = {GameData.Instance.CompletedTasks}/{GameData.Instance.TotalTasks}",
            "TaskState.Update");
    }
}