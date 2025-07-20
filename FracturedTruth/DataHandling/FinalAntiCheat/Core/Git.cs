using System.IO;
using UnityEngine;

namespace FracturedTruth.DataHandling.FinalAntiCheat.Core;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
internal static class Git
{
    public static void Prefix(MainMenuManager __instance)
    {
        // 获取当前Dll启动目录
        var DirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        // 针对基于BepInEx注入检测
        if (DirectoryPath == null) return;
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.*"))
        {
            var fileName = Path.GetFileName(path);

            if (fileName is not "FracturedTruth.dll" and not "PolarNight.dll") Application.Quit(1);
        }
    }
}