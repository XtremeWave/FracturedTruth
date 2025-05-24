using System;
using System.Threading.Tasks;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using TMPro;
using UnityEngine;

namespace FinalSuspect.Patches.Game_Vanilla;

[HarmonyPatch(typeof(IntroCutscene))]
internal class IntroCutscenePatch
{
    [HarmonyPatch(nameof(IntroCutscene.CoBegin)), HarmonyPrefix]
    public static void CoBegin_Prefix()
    {
        InGame = true;
        Info("Game Start", "IntroCutscene");
    }

    [HarmonyPatch(nameof(IntroCutscene.ShowRole)), HarmonyPrefix]
    public static void ShowRole_Postfix(IntroCutscene __instance)
    {
        //莫名失效
        if (!Main.EnableFinalSuspect.Value) return;

        _ = new MainThreadTask(() =>
        {
            var roleType = PlayerControl.LocalPlayer.Data.Role.Role;
            __instance.YouAreText.color =
                __instance.RoleText.color =
                    __instance.RoleBlurbText.color = GetRoleColor(roleType);
            __instance.RoleText.text = GetRoleName(roleType);
            __instance.RoleText.fontWeight = FontWeight.Thin;
            __instance.RoleText.SetOutlineColor(GetRoleColor(roleType).ShadeColor(0.1f).SetAlpha(0.38f));
            __instance.RoleText.SetOutlineThickness(0.17f);
            __instance.RoleBlurbText.text = roleType.GetRoleInfoForVanilla();
        }, "Override Role Text");
    }

    [HarmonyPatch(nameof(IntroCutscene.BeginImpostor)), HarmonyPostfix]
    public static void BeginImpostor_Postfix(IntroCutscene __instance)
    {
        if (!Main.EnableFinalSuspect.Value) return;

        __instance.ImpostorText.gameObject.SetActive(true);
        var onlyImp = GameManager.Instance.LogicOptions.GetAdjustedNumImpostors(GameData.Instance.PlayerCount) == 1;

        var color = Palette.ImpostorRed;
        var colorcode = onlyImp
            ? ColorHelper.ColorToHex(Palette.DisabledGrey)
            : ColorHelper.ColorToHex(Palette.ImpostorRed);
        __instance.TeamTitle.text = onlyImp
            ? GetString("TeamImpostorOnly")
            : GetString("TeamImpostor");

        __instance.TeamTitle.color = color;
        __instance.ImpostorText.text = $"<color=#{colorcode}>";
        __instance.ImpostorText.text += onlyImp
            ? GetString("ImpostorNumImpOnly")
            : $"{string.Format(GetString("ImpostorNumImp"), GameManager.Instance.LogicOptions.GetAdjustedNumImpostors(GameData.Instance.PlayerCount))}";

        __instance.ImpostorText.text += "\n" + (onlyImp
            ? GetString("ImpostorIntroTextOnly")
            : GetString("ImpostorIntroText"));

        __instance.BackgroundBar.material.color = Palette.DisabledGrey;

        StartFadeIntro(__instance, Palette.DisabledGrey, Palette.ImpostorRed);
    }

    [HarmonyPatch(nameof(IntroCutscene.BeginCrewmate)), HarmonyPostfix]
    public static void BeginCrewmate_Postfix(IntroCutscene __instance)
    {
        if (!Main.EnableFinalSuspect.Value) return;

        __instance.TeamTitle.text = $"{GetString("TeamCrewmate")}";
        __instance.ImpostorText.text =
            $"{string.Format(GetString("ImpostorNumCrew"), GameManager.Instance.LogicOptions.GetAdjustedNumImpostors(GameData.Instance.PlayerCount))}";
        __instance.ImpostorText.text += "\n" + GetString("CrewmateIntroText");
        __instance.TeamTitle.color = new Color32(140, 255, 255, byte.MaxValue);

        StartFadeIntro(__instance, new Color32(140, 255, 255, byte.MaxValue), PlayerControl.LocalPlayer.GetRoleColor());


        if (!Input.GetKey(KeyCode.RightControl)) return;
        __instance.TeamTitle.text = "警告";
        __instance.ImpostorText.gameObject.SetActive(true);
        __instance.ImpostorText.text = "请远离无知的玩家";
        __instance.TeamTitle.color = Color.magenta;
        StartFadeIntro(__instance, Color.red, Color.magenta);
    }

    private static async void StartFadeIntro(IntroCutscene __instance, Color start, Color end)
    {
        try
        {
            await Task.Delay(1000);
            var milliseconds = 0;
            while (true)
            {
                await Task.Delay(20);
                milliseconds += 20;
                var time = milliseconds / (float)500;
                var LerpingColor = Color.Lerp(start, end, time);
                if (!__instance || milliseconds > 500)
                {
                    Info("break", "StartFadeIntro");
                    break;
                }

                __instance.BackgroundBar.material.color = LerpingColor;
            }
        }
        catch
        {
            /* ignored */
        }
    }
}