using System;
using System.IO;
using System.Reflection;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class DllChecker
{
    private static void Prefix(MainMenuManager __instance)
    {
        // 针对基于BepInEx注入检测
        // 获取当前Dll启动目录
        string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        // 检查该目录下的所有文件
        foreach (string path in Directory.EnumerateFiles(directoryPath, "*.*"))
        {
            string fileName = Path.GetFileName(path);

            // 检查是否是非目标文件
            if (fileName != "FinalSuspect.dll")
            {
                // 记录日志
                Error($"检测到非法文件: {fileName}！游戏将被强制终止。", "FAC");

                // 终止游戏运行
                Environment.Exit(1);
            }
        }

        // 基于version注入检测
        string rootPath = Environment.CurrentDirectory;

        // SM的文件名是写死的
        string[] suspiciousFiles = { "SickoMenu.dll", "version.dll" };

        // 检查根目录每个文件是否存在该文件
        foreach (string fileName in suspiciousFiles)
        {
            string fullPath = Path.Combine(rootPath, fileName);

            if (File.Exists(fullPath))
            {
                // 记录日志
                Error($"检测到非法文件: {fileName}！游戏将被强制终止。", "FAC");

                // 终止游戏运行
                Environment.Exit(1);
            }
        }
    }
}