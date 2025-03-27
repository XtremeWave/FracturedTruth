using System;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace FinalSuspect.Patches.Game_Vanilla;

[HarmonyPatch]
internal class ServerDropdownPatch
{
    [HarmonyPatch(typeof(ServerDropdown))]
    [HarmonyPatch(nameof(ServerDropdown.FillServerOptions))]
    [HarmonyPrefix]
    internal static bool FillServerOptions_Prefix(ServerDropdown __instance)
    {
        // 调整背景板
        __instance.background.size = new Vector2(5, 1);

        int num = 0;
        int column = 0;
        
        const int maxPerColumn = 6;     // 每列最大按钮数
        const float columnWidth = 2.8f; // 列宽度
        const float buttonSpacing = 0.5f; // 按钮间距

        // 获取可用服务器
        var regions = DestroyableSingleton<ServerManager>.Instance.AvailableRegions
            .OrderBy(ServerManager.DefaultRegions.Contains)
            .ToList();

        int totalColumns = Mathf.Max(1, Mathf.CeilToInt(regions.Count / (float)maxPerColumn));
        int rowsInLastColumn = regions.Count % maxPerColumn;
        int maxRows = (regions.Count > maxPerColumn) ? maxPerColumn : regions.Count;

        foreach (IRegionInfo regionInfo in regions)
        {
            if (DestroyableSingleton<ServerManager>.Instance.CurrentRegion.Name == regionInfo.Name)
            {
                __instance.defaultButtonSelected = __instance.firstOption;
                __instance.firstOption.ChangeButtonText(DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(
                    regionInfo.TranslateName,
                    regionInfo.Name,
                    new Il2CppReferenceArray<Il2CppSystem.Object>(0)));
                continue;
            }

            IRegionInfo region = regionInfo;
            ServerListButton serverListButton = __instance.ButtonPool.Get<ServerListButton>();

            float xPos = (column - (totalColumns - 1) / 2f) * columnWidth;
            float yPos = __instance.y_posButton - buttonSpacing * (num % maxPerColumn);

            // 设置按钮位置
            serverListButton.transform.localPosition = new Vector3(xPos, yPos, -1f);
            serverListButton.transform.localScale = Vector3.one;

            // 设置按钮文本
            serverListButton.Text.text = DestroyableSingleton<TranslationController>.Instance.GetStringWithDefault(
                regionInfo.TranslateName,
                regionInfo.Name,
                new Il2CppReferenceArray<Il2CppSystem.Object>(0));

            serverListButton.Text.ForceMeshUpdate(false, false);

            serverListButton.Button.OnClick.RemoveAllListeners();
            serverListButton.Button.OnClick.AddListener((Action)(() => __instance.ChooseOption(region))); // 点击时选择对应区域

            __instance.controllerSelectable.Add(serverListButton.Button);

            num++;
            if (num % maxPerColumn == 0)
            {
                column++;
            }
        }

        // 背景板最终大小
        float backgroundHeight = 1.2f + buttonSpacing * (maxRows - 1);
        float backgroundWidth = (totalColumns > 1) ?
        (columnWidth * (totalColumns - 1) + __instance.background.size.x) :
        __instance.background.size.x;

        // 居中背景板
        __instance.background.transform.localPosition = new Vector3(
            0f,
        __instance.initialYPos - (backgroundHeight - 1.2f) / 2f,
            0f);
        __instance.background.size = new Vector2(backgroundWidth, backgroundHeight);

        return false;
    }
}