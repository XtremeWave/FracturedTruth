using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Unity.IL2CPP;

namespace FinalSuspect.DataHandling.FinalAntiCheat.Core;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class DllChecker
{
    private static void Prefix(MainMenuManager __instance)
    {
        // SM的文件名是写死的
        string[] SuspiciousFiles = { "SickoMenu.dll", "version.dll" };
        // 获取当前Dll启动目录
        string DirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        // 获取游戏根目录
        string AmongUsPath = Environment.CurrentDirectory;
        // 针对基于BepInEx注入检测
        foreach (string path in Directory.EnumerateFiles(DirectoryPath, "*.*"))
        {
            string fileName = Path.GetFileName(path);

            if (fileName != "FinalSuspect.dll")
            {
                Error($"检测到非法文件: {fileName}！游戏将被强制终止。", "FAC");
                Environment.Exit(1);
            }
        }

        // 针对基于version注入检测
        foreach (string fileName in SuspiciousFiles)
        {
            string fullPath = Path.Combine(AmongUsPath, fileName);

            if (File.Exists(fullPath))
            {
                Error($"检测到非法/模组文件: {fileName}！游戏将被强制终止。", "FAC");
                Environment.Exit(1);
            }
        }
    }
    public static void Init()
    {
        // 什么都不干，只是让main.cs有个引用防止被反编译大蛇删除
    }
}

[HarmonyPatch(typeof(IL2CPPChainloader), nameof(IL2CPPChainloader.LoadPlugin))]
public static class DisableOtherPlugins
{
    public static bool Prefix([HarmonyArgument(0)] PluginInfo pluginInfo, [HarmonyArgument(1)] Assembly pluginAssembly)
    {
        Test(pluginInfo.Metadata.GUID);
        if (pluginInfo.Metadata.GUID == "com.sinai.unityexplorer")
            return true;
        return false;
    }
}
