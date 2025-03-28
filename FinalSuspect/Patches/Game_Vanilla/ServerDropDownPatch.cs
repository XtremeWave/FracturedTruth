using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalSuspect.Patches.Game_Vanilla;

[HarmonyPatch]
public static class ServerDropDownPatch
{
    [HarmonyPatch(typeof(ServerDropdown), nameof(ServerDropdown.FillServerOptions))]
    [HarmonyPrefix]
    internal static bool FillServerOptions_Prefix(ServerDropdown __instance)
    {
        if (SceneManager.GetActiveScene().name == "FindAGame") return true;

        // 调整背景大小
        __instance.background.size = new Vector2(5, 1);

        int num = 0;
        int column = 0;
        const int maxPerColumn = 6;       // 每列最大按钮数
        const float columnWidth = 4.15f;  // 列宽度
        const float buttonSpacing = 0.5f; // 按钮间距

        var regions = DestroyableSingleton<ServerManager>.Instance.AvailableRegions.OrderBy(ServerManager.DefaultRegions.Contains).ToList();
        int totalColumns = Mathf.Max(1, Mathf.CeilToInt(regions.Count / (float)maxPerColumn));
        int rowsInLastColumn = regions.Count % maxPerColumn;
        int maxRows = (regions.Count > maxPerColumn) ? maxPerColumn : regions.Count;

        foreach (IRegionInfo regionInfo in regions)
        {
            if (DestroyableSingleton<ServerManager>.Instance.CurrentRegion.Name == regionInfo.Name)
            {
                __instance.defaultButtonSelected = __instance.firstOption;
                __instance.firstOption.ChangeButtonText(DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(regionInfo.TranslateName, regionInfo.Name, new Il2CppReferenceArray<Il2CppSystem.Object>(0)));
                continue;
            }

            // 创建服务器按钮
            IRegionInfo region = regionInfo;
            ServerListButton serverListButton = __instance.ButtonPool.Get<ServerListButton>();

            // 按钮位置
            float xPos = (column - (totalColumns - 1) / 2f) * columnWidth;
            float yPos = __instance.y_posButton - buttonSpacing * (num % maxPerColumn);

            // 按钮位置和缩放
            serverListButton.transform.localPosition = new Vector3(xPos, yPos, -1f);
            serverListButton.transform.localScale = Vector3.one;
            
            // 设置按钮
            serverListButton.Text.text = DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(
                regionInfo.TranslateName,
                regionInfo.Name,
                new Il2CppReferenceArray<Il2CppSystem.Object>(0));
            serverListButton.Text.ForceMeshUpdate(false, false);
            serverListButton.Button.OnClick.RemoveAllListeners();
            serverListButton.Button.OnClick.AddListener((Action)(() => __instance.ChooseOption(region)));
            __instance.controllerSelectable.Add(serverListButton.Button);

            num++;
            if (num % maxPerColumn == 0)
            {
                column++;
            }
        }

        // 调整背景大小和位置
        float backgroundHeight = 1.2f + buttonSpacing * (maxRows - 1);
        float backgroundWidth = (totalColumns > 1) ?
            (columnWidth * (totalColumns - 1) + __instance.background.size.x) :
            __instance.background.size.x;

        __instance.background.transform.localPosition = new Vector3(
            0f,
            __instance.initialYPos - (backgroundHeight - 1.2f) / 2f,
            0f);
        __instance.background.size = new Vector2(backgroundWidth, backgroundHeight);

        return false;
    }

    [HarmonyPatch(typeof(ServerDropdown), nameof(ServerDropdown.FillServerOptions))]
    [HarmonyPostfix]
    internal static void FillServerOptions_Postfix(ServerDropdown __instance)
    {
        // 仅在搜索界面生效
        if (SceneManager.GetActiveScene().name != "FindAGame") return;

        float buttonSpacing = 0.6f;
        float columnSpacing = 5.75f;

        // 按钮按Y轴排序
        List<ServerListButton> allButtons = [.. __instance.GetComponentsInChildren<ServerListButton>().OrderByDescending(b => b.transform.localPosition.y)];
        if (allButtons.Count == 0)
            return;

        const int buttonsPerColumn = 7;
        int columnCount = (allButtons.Count + buttonsPerColumn - 1) / buttonsPerColumn;
        Vector3 startPosition = new(0, -buttonSpacing, 0);

        for (int i = 0; i < allButtons.Count; i++)
        {
            int col = i / buttonsPerColumn;
            int row = i % buttonsPerColumn;
            allButtons[i].transform.localPosition = startPosition + new Vector3(col * columnSpacing, -row * buttonSpacing, 0f);
        }

        // 计算背景大小和位置
        int maxRows  = Math.Min(buttonsPerColumn, allButtons.Count);
        float backgroundHeight = 1.2f + buttonSpacing * (maxRows - 1);
        float backgroundWidth = (columnCount > 1) ?
            (columnSpacing * (columnCount - 1) + 5) : 5;

            __instance.background.transform.localPosition = new Vector3(
            0f,
            __instance.initialYPos - (backgroundHeight - 1.2f) / 2f,
            0f);
        __instance.background.size = new Vector2(backgroundWidth, backgroundHeight);
        __instance.background.transform.localPosition += new Vector3(4f, 0, 0);
    }
}