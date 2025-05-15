using System;
using System.IO;
using System.Reflection;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class DllChecker
{
    private static void Prefix(MainMenuManager __instance)
    {
        // SM的文件名是写死的
        string[] suspiciousFiles = { "SickoMenu.dll", "version.dll" };
        // 获取当前Dll启动目录
        string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        // 获取游戏根目录
        string rootPath = Environment.CurrentDirectory;
        // 针对基于BepInEx注入检测
        foreach (string path in Directory.EnumerateFiles(directoryPath, "*.*"))
        {
            string fileName = Path.GetFileName(path);

            if (fileName != "FinalSuspect.dll")
            {
                Error($"检测到非法文件: {fileName}！游戏将被强制终止。", "FAC");
                Environment.Exit(1);
            }
        }

        // 针对基于version注入检测
        foreach (string fileName in suspiciousFiles)
        {
            string fullPath = Path.Combine(rootPath, fileName);

            if (File.Exists(fullPath))
            {
                Error($"检测到非法文件: {fileName}！游戏将被强制终止。", "FAC");
                Environment.Exit(1);
            }
        }
    }
}