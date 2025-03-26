using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace FinalSuspect.Patches.Game_Vanilla;
//From: https://github.com/EnhancedNetwork/TownofHost-Enhanced/blob/main/Patches/RegionMenuPatch.cs

[HarmonyPatch]
public static class ServerDropDownPatch
{
    [HarmonyPatch(typeof(ServerDropdown), nameof(ServerDropdown.FillServerOptions))]
    [HarmonyPostfix]
    public static void AdjustButtonPositions(ServerDropdown __instance)
    {
        List<ServerListButton> allButtons = [.. __instance.GetComponentsInChildren<ServerListButton>()];

        if (allButtons.Count == 0)
            return;

        // 按钮间的垂直间距，可根据实际需求调整
        float buttonSpacing = 0.6f;
        // 列之间的水平间距，可根据实际需求调整
        float columnSpacing = 4.25f;

        if (SceneManager.GetActiveScene().name == "FindAGame")
        {
            const int buttonsPerColumn = 7;
            _ = (allButtons.Count + buttonsPerColumn - 1) / buttonsPerColumn;

            Vector3 startPosition = new(0, -buttonSpacing, 0);

            for (int i = 0; i < allButtons.Count; i++)
            {
                int col = i / buttonsPerColumn;
                int row = i % buttonsPerColumn;
                allButtons[i].transform.localPosition = startPosition + new Vector3(col * columnSpacing, -row * buttonSpacing, 0f);
            }
        }
        else
        {
            const int buttonsInFirstColumn = 5;

            int buttonsInSecondColumn = allButtons.Count - buttonsInFirstColumn;

            if (buttonsInFirstColumn <= 0 || buttonsInSecondColumn <= 0)
            {
                for (int i = 0; i < allButtons.Count; i++)
                {
                    allButtons[i].transform.localPosition = new Vector3(0, -i * buttonSpacing, 0);
                }
                return;
            }

            Vector3 startPosition = new(0, -buttonSpacing, 0);

            for (int i = 0; i < buttonsInFirstColumn; i++)
            {
                allButtons[i].transform.localPosition = startPosition + new Vector3(0, -i * buttonSpacing, 0);
            }

            float secondColumnStartY = 0;
            if (buttonsInSecondColumn > 1)
            {
                // Last button in second column should be at the same height as the last button in the first column
                secondColumnStartY = -(buttonsInFirstColumn - buttonsInSecondColumn) * buttonSpacing;
            }

            for (int i = 0; i < buttonsInSecondColumn; i++)
            {
                int buttonIndex = buttonsInFirstColumn + i;
                allButtons[buttonIndex].transform.localPosition = startPosition + new Vector3(columnSpacing, secondColumnStartY - i * buttonSpacing, 0);
            }
        }
    }
}