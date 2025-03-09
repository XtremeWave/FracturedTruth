using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FinalSuspect.Attributes;
using FinalSuspect.Modules.Core.Game;
using Newtonsoft.Json.Linq;

namespace FinalSuspect.Modules.Features.CheckingandBlocking;

public static class SpamManager
{
    private static readonly string BANEDWORDS_FILE_PATH = LocalPath_Data + "BanWords.txt";
    public static readonly string DENY_NAME_LIST_PATH = GetBanFilesPath("DenyName.txt");
    public static List<string> BanWords = [];

    [PluginModuleInitializer]
    public static void Init()
    {
        foreach (var url in GetInfoFileUrlList())
        {
            if (!GetVersionInfo(url + "fs_info.json").GetAwaiter().GetResult()) continue;
            break;
        }

        try
        {
            CreateIfNotExists();
            BanWords = ReturnAllNewLinesInFile(BANEDWORDS_FILE_PATH);
        }
        catch
        {
        }
    }

    private static void CreateIfNotExists()
    {
        if (!File.Exists(BANEDWORDS_FILE_PATH))
        {
            try
            {
                if (File.Exists(@"./BanWords.txt")) File.Move(@"./BanWords.txt", BANEDWORDS_FILE_PATH);
                else
                {
                    var fileName = GetUserLangByRegion().ToString();
                    XtremeLogger.Warn($"Create New BanWords: {fileName}", "SpamManager");
                }
            }
            catch (Exception ex)
            {
                XtremeLogger.Exception(ex, "SpamManager");
            }
        }

        if (File.Exists(DENY_NAME_LIST_PATH)) return;
        {
            try
            {
                if (!Directory.Exists(@"Final Suspect_Data")) Directory.CreateDirectory(@"Final Suspect_Data");
                if (File.Exists(@"./DenyName.txt")) File.Move(@"./DenyName.txt", BANEDWORDS_FILE_PATH);
            }
            catch (Exception ex)
            {
                XtremeLogger.Exception(ex, "SpamManager");
            }
        }
    }

    public static List<string> ReturnAllNewLinesInFile(string filename)
    {
        if (!File.Exists(filename)) return [];
        using StreamReader sr = new(filename, Encoding.GetEncoding("UTF-8"));
        string text;
        List<string> sendList = [];
        while ((text = sr.ReadLine()) != null)
            if (text.Length >= 1 && text != "")
                sendList.Add(text.Replace("\\n", "\n").ToLower());
        return sendList;
    }

    public static void CheckSpam(ref string text)
    {
        if (!Main.SpamDenyWord.Value) return;
        try
        {
            var mt = text;
            var banned = BanWords.Any(mt.ToLower().Contains);

            if (banned)
            {
                foreach (var word in BanWords)
                {
                    if (text.ToLower().Contains(word.ToLower()))
                    {
                        text = text.Replace(word, $"<color=#E57373>{new string('*', word.Length)}</color>");
                    }
                }
            }
        }
        catch
        {
        }
    }

    public static async Task<bool> GetVersionInfo(string url)
    {
        try
        {
            string result;
            if (url.StartsWith("file:///"))
            {
                result = await File.ReadAllTextAsync(url[8..]);
            }
            else
            {
                using HttpClient client = new();
                client.DefaultRequestHeaders.Add("User-Agent", "FinalSuspect Updater");
                client.DefaultRequestHeaders.Add("Referer", "api.xtreme.net.cn");
                using var response = await client.GetAsync(new Uri(url), HttpCompletionOption.ResponseContentRead);
                if (!response.IsSuccessStatusCode || response.Content == null)
                {
                    XtremeLogger.Error($"Failed: {response.StatusCode}", "CheckRelease");
                    return false;
                }

                result = await response.Content.ReadAsStringAsync();
                result = result.Replace("\r", string.Empty).Replace("\n", string.Empty).Trim();
            }

            var data = JObject.Parse(result);

            ProcessBanWords(data);
            ProcessDenyNames(data);
            ProcessFacList(data);


            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ProcessBanWords(JObject data)
    {
        var newWords = GetTokens(data["words"])
            .Where(word => !BanWords.Contains(word))
            .ToList();

        UpdateBanWords(newWords);
    }

    private static void ProcessDenyNames(JObject data)
    {
        var existingNames = ReturnAllNewLinesInFile(DENY_NAME_LIST_PATH);
        var newNames = GetTokens(data["denynames"])
            .Except(existingNames, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (newNames.Count > 0)
        {
            File.AppendAllLines(DENY_NAME_LIST_PATH, newNames);
        }
    }

    private static void ProcessFacList(JObject data)
    {
        var facList = GetTokens(data["Cheats"])
            .Concat(GetTokens(data["Griefer"]))
            .Where(line => ShouldAddToFacList(line))
            .ToList();

        BanManager.FACList.AddRange(facList);
    }

    private static IEnumerable<string> GetTokens(JToken token)
    {
        var jarray = token as JArray;
        var tokens = new List<string>();
        for (int i = 0; i < jarray.Count; i++)
        {
            tokens.Add(jarray[i].ToString());
        }

        return tokens?
            .Where(t => t != null) // 过滤空元素
            .Select(t => t.ToString());
    }

    private static void UpdateBanWords(List<string> newWords)
    {
        if (newWords.Count == 0) return;

        BanWords.AddRange(newWords);
        File.AppendAllLines(BANEDWORDS_FILE_PATH, newWords);
    }

    private static bool ShouldAddToFacList(string line)
    {
        return !Main.AllPlayerControls
            .Where(p => p.IsDev())
            .Any(p => line.Contains(p.FriendCode, StringComparison.OrdinalIgnoreCase));
    }
}
