using System;
using System.IO;
using Microsoft.Win32;

namespace FinalSuspect.Modules.Core.Plugin;

# pragma warning disable CA1416
public static class RegistryManager
{
    public static RegistryKey Keys = SoftwareKeys.OpenSubKey("AU-FinalSuspect", true);
    public static Version LastVersion;
    public static RegistryKey SoftwareKeys => Registry.CurrentUser.OpenSubKey("Software", true);

    public static void Init()
    {
        if (Keys == null)
        {
            Info("Create FinalSuspect Registry Key", "Registry Manager");
            Keys = SoftwareKeys.CreateSubKey("AU-FinalSuspect", true);
        }

        if (Keys == null)
        {
            Error("Create Registry Failed", "Registry Manager");
            return;
        }

        if (Keys.GetValue("Last launched version") is not string regLastVersion)
            LastVersion = new Version(0, 0, 0);
        else LastVersion = Version.Parse(regLastVersion);

        Keys.SetValue("Last launched version", Main.version.ToString());
        Keys.SetValue("Path", Path.GetFullPath("./"));

        List<string> FoldersNFileToDel = ["./TOH_DATA"];

        Info("上次启动的FinalSuspect版本：" + LastVersion, "Registry Manager");

        if (LastVersion < new Version(1, 0, 0))
        {
            Warn("v1.0 New Version Operation Needed", "Registry Manager");
            FoldersNFileToDel.Add("./BepInEx/config");
        }

        FoldersNFileToDel.DoIf(Directory.Exists, p =>
        {
            Warn("Delete Useless Directory:" + p, "Registry Manager");
            Directory.Delete(p, true);
        });
        FoldersNFileToDel.DoIf(File.Exists, p =>
        {
            Warn("Delete Useless File:" + p, "Registry Manager");
            File.Delete(p);
        });
    }
}