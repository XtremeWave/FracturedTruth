using System;
using System.Threading.Tasks;
using AmongUs.GameOptions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using FinalSuspect;
using FinalSuspect.Attributes;
using FinalSuspect.DataHandling.FinalAntiCheat.Core;
using FinalSuspect.Helpers;
using FinalSuspect.Internal;
using FinalSuspect.Modules.Random;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

// ReSharper disable MemberCanBePrivate.Global

[assembly: AssemblyFileVersion(Main.PluginVersion)]
[assembly: AssemblyInformationalVersion(Main.PluginVersion)]
[assembly: AssemblyVersion(Main.PluginVersion)]

namespace FinalSuspect;

[BepInPlugin(PluginGuid, "FinalSuspect", PluginVersion)]
[BepInProcess("Among Us.exe")]
public class Main : BasePlugin
{
    // == 程序基本设定 / Program Config ==
    public const string ModName = "Final Suspect";
    public const string ForkId = "Final Suspect";
    public const string PluginVersion = "1.1.100";
    public const string PluginGuid = "cn.xtremewave.finalsuspect";
    public const int PluginCreation = 0;
    public const string DebugKeyHash = "c0fd562955ba56af3ae20d7ec9e64c664f0facecef4b3e366e109306adeae29d";
    public const string DebugKeySalt = "59687b";

    // == 版本相关设定 / Version Config ==
    public const string LowestSupportedVersion = "2025.6.10"; // 16.1.0

    private const string DisplayedVersion_Head = "1.2";

    private const string DisplayedVersion_Date = BuildTime.Date;

    /// <summary>
    ///     表示当前显示的版本类型。
    /// </summary>
    private const VersionTypes DisplayedVersion_Type = VersionTypes.Alpha;

    private const int DisplayedVersion_TestCreation = 3;


    // == 链接相关设定 / Link Config ==
    //public static readonly string WebsiteUrl = IsChineseLanguageUser ? "https://www.xtreme.net.cn/project/FS/" : "https://www.xtreme.net.cn/en/project/FS/";
    public const string QQInviteUrl = "https://qm.qq.com/q/GNbm9UjfCa";
    public const string DiscordInviteUrl = "https://discord.gg/kz787Zg7h8/";
    public const string GithubRepoUrl = "https://github.com/XtremeWave/FinalSuspect/";
    public const float RoleTextSize = 2f;

    public static readonly string DisplayedVersion =
#if RELEASE
        $"{DisplayedVersion_Head}_{DisplayedVersion_Date}";
#else
        $"{DisplayedVersion_Head}_{DisplayedVersion_Date}_{DisplayedVersion_Type}_{DisplayedVersion_TestCreation}";
#endif
    public static readonly Version version = Version.Parse(PluginVersion);
    public static ManualLogSource Logger;
    public static bool hasArgumentException;
    public static string ExceptionMessage;
    public static bool ExceptionMessageIsShown;
    public static string CredentialsText;

    public static Dictionary<RoleTypes, string> roleColors;
    public static List<int> clientIdList = [];

    public static string HostNickName = "";
    public static bool IsInitialRelease = DateTime.Now.Month == 8 && DateTime.Now.Day is 14;
    public static bool IsAprilFools = DateTime.Now.Month == 4 && DateTime.Now.Day is 1;

    public static Main Instance;

    //public static bool NewLobby = false;

    public static readonly List<string> TName_Snacks_CN =
    [
        "冰激凌", "奶茶", "巧克力", "蛋糕", "甜甜圈", "可乐", "柠檬水", "冰糖葫芦", "果冻", "糖果", "牛奶",
        "抹茶", "烧仙草", "菠萝包", "布丁", "椰子冻", "曲奇", "红豆土司", "三彩团子", "艾草团子", "泡芙", "可丽饼",
        "桃酥", "麻薯", "鸡蛋仔", "马卡龙", "雪梅娘", "炒酸奶", "蛋挞", "松饼", "西米露", "奶冻", "奶酥", "可颂", "奶糖"
    ];

    public static readonly List<string> TName_Snacks_EN =
    [
        "Ice cream", "Milk tea", "Chocolate", "Cake", "Donut", "Coke", "Lemonade", "Candied haws", "Jelly", "Candy",
        "Milk",
        "Matcha", "Burning Grass Jelly", "Pineapple Bun", "Pudding", "Coconut Jelly", "Cookies", "Red Bean Toast",
        "Three Color Dumplings", "Wormwood Dumplings", "Puffs", "Can be Crepe", "Peach Crisp", "Mochi", "Egg Waffle",
        "Macaron",
        "Snow Plum Niang", "Fried Yogurt", "Egg Tart", "Muffin", "Sago Dew", "panna cotta", "soufflé", "croissant",
        "toffee"
    ];

    // == 认证设定 / Authentication Config ==
    public static HashAuth DebugKeyAuth { get; private set; }
    public static ConfigEntry<string> DebugKeyInput { get; private set; }

    // ==========
    public Harmony Harmony { get; } = new(PluginGuid);
    public static NormalGameOptionsV09 NormalOptions => GameOptionsManager.Instance.currentNormalGameOptions;
    public static HideNSeekGameOptionsV09 HideNSeekOptions => GameOptionsManager.Instance.currentHideNSeekGameOptions;

    //Client Options
    public static ConfigEntry<bool> KickPlayerWithAbnormalFriendCode { get; private set; }
    public static ConfigEntry<bool> KickPlayerWithDenyName { get; private set; }
    public static ConfigEntry<bool> KickPlayerInBanList { get; private set; }
    public static ConfigEntry<bool> SpamDenyWord { get; private set; }
    public static ConfigEntry<bool> UnlockFPS { get; private set; }
    public static ConfigEntry<OutfitType> SwitchOutfitType { get; private set; }
    public static ConfigEntry<bool> AutoStartGame { get; private set; }
    public static ConfigEntry<bool> AutoEndGame { get; private set; }
    public static ConfigEntry<bool> DisableVanillaSound { get; private set; }
    public static ConfigEntry<bool> DisableFAC { get; private set; }
    public static ConfigEntry<bool> ShowPlayerInfo { get; private set; }
    public static ConfigEntry<bool> UseModCursor { get; private set; }
    public static ConfigEntry<bool> FastLaunchMode { get; private set; }
    public static ConfigEntry<bool> VersionCheat { get; private set; }
    public static ConfigEntry<bool> GodMode { get; private set; }
    public static ConfigEntry<bool> NoGameEnd { get; private set; }

    //Other Configs
    public static ConfigEntry<string> HideName { get; private set; }
    public static ConfigEntry<string> HideColor { get; private set; }
    public static ConfigEntry<bool> ShowResults { get; private set; }
    public static ConfigEntry<string> WebhookURL { get; private set; }
    public static ConfigEntry<bool> EnableFinalSuspect { get; private set; }
    public static ConfigEntry<string> LastStartVersion { get; private set; }
    public static ConfigEntry<BypassType> LanguageUpdateBypass { get; private set; }
    public static ConfigEntry<int> CurrentBackgroundId { get; private set; }

    public static IEnumerable<PlayerControl> AllPlayerControls =>
        PlayerControl.AllPlayerControls.ToArray().Where(p => p);

    public static IEnumerable<PlayerControl> AllAlivePlayerControls =>
        PlayerControl.AllPlayerControls.ToArray().Where(p => p && p.IsAlive() && !p.Data.Disconnected);

    public static string Get_TName_Snacks =>
        TranslationController.Instance.currentLanguage.languageID is SupportedLangs.SChinese or SupportedLangs.TChinese
            ? TName_Snacks_CN[IRandom.Instance.Next(0, TName_Snacks_CN.Count)]
            : TName_Snacks_EN[IRandom.Instance.Next(0, TName_Snacks_EN.Count)];

    public override void Load()
    {
        Instance = this;

        //Configs
        HideName = Config.Bind("Xtreme System", "Hide Game Code Name", "Final Suspect");
        HideColor = Config.Bind("Xtreme System", "Hide Game Code Color", $"{ColorHelper.ModColor}");
        EnableFinalSuspect = Config.Bind("Xtreme System", "Enable Final Suspect", true);
        ShowResults = Config.Bind("Xtreme System", "Show Results", true);
        LastStartVersion = Config.Bind("Xtreme System", "Last Start Version", "0.0.0");
        LanguageUpdateBypass = Config.Bind("Xtreme System", "Language Update Bypass", BypassType.Dont);
        CurrentBackgroundId = Config.Bind("Xtreme System", "BG Id", 0);

        DebugKeyInput = Config.Bind("Authentication", "Debug Key", "");

        UnlockFPS = Config.Bind("Client Options", "Unlock FPS", false);
        SwitchOutfitType = Config.Bind("Client Options", "Switch Outfit", OutfitType.BeanMode);
        KickPlayerWithAbnormalFriendCode = Config.Bind("Client Options", "Kick Player FriendCode Not Exist", true);
        KickPlayerInBanList = Config.Bind("Client Options", "Kick Player In BanList", true);
        KickPlayerWithDenyName = Config.Bind("Client Options", "Kick Player With Deny Name", true);
        SpamDenyWord = Config.Bind("Client Options", "Spam Deny Word", true);
        AutoStartGame = Config.Bind("Client Options", "Auto Start Game", false);
        AutoEndGame = Config.Bind("Client Options", "Auto End Game", false);
        DisableVanillaSound = Config.Bind("Client Options", "Disable Vanilla Sound", false);
        DisableFAC = Config.Bind("Client Options", "Disable FAC", false);
        //PrunkMode = Config.Bind("Client Options", "Prunk Mode", false);
        ShowPlayerInfo = Config.Bind("Client Options", "Show Player Info", true);
        UseModCursor = Config.Bind("Client Options", "Use Mod Cursor", true);
        FastLaunchMode = Config.Bind("Client Options", "Fast Launch Mode", false);
        VersionCheat = Config.Bind("Client Options", "Version Cheat", false);
        GodMode = Config.Bind("Client Options", "God Mode", false);
        NoGameEnd = Config.Bind("Client Options", "No Game End", false);

        Logger = BepInEx.Logging.Logger.CreateLogSource("FinalSuspect");
        Enable();
        Disable("SwitchSystem");
        Disable("ModNews");
        Disable("CancelPet");
        if (!DebugModeManager.AmDebugger)
        {
            Disable("Download Resources");
            Disable("GetAnnouncements");
            Disable("GetConfigs");
        }

        isDetail = true;

        // 認証関連-初期化
        DebugKeyAuth = new HashAuth(DebugKeyHash, DebugKeySalt);

        // 認証関連-認証
        DebugModeManager.Auth(DebugKeyAuth, DebugKeyInput.Value);

        WebhookURL = Config.Bind("hook", "WebhookURL", "none");

        hasArgumentException = false;
        ExceptionMessage = "";
        try
        {
            roleColors = new Dictionary<RoleTypes, string>
            {
                { RoleTypes.CrewmateGhost, "#8CFFFF" },
                { RoleTypes.GuardianAngel, "#8CFFDB" },
                { RoleTypes.Crewmate, "#8CFFFF" },
                { RoleTypes.Scientist, "#F8FF8C" },
                { RoleTypes.Engineer, "#A5A8FF" },
                { RoleTypes.Noisemaker, "#FFC08C" },
                { RoleTypes.Tracker, "#93FF8C" },
                { RoleTypes.ImpostorGhost, "#FF1919" },
                { RoleTypes.Impostor, "#FF1919" },
                { RoleTypes.Shapeshifter, "#FF819E" },
                { RoleTypes.Phantom, "#CA8AFF" }
            };
        }
        catch (ArgumentException ex)
        {
            Error("错误：字典出现重复项", "LoadDictionary");
            Exception(ex, "LoadDictionary");
            hasArgumentException = true;
            ExceptionMessage = ex.Message;
            ExceptionMessageIsShown = false;
        }

        RegistryManager.Init(); // 这是优先级最高的模块初始化方法，不能使用模块初始化属性
        DllChecker.Init();

        PluginModuleInitializerAttribute.InitializeAll();

        IRandom.SetInstance(new NetRandomWrapper());

        Info($"{Application.version}", "AmongUs Version");

        var handler = Handler("GitVersion");
        handler.Info($"{nameof(GitBaseTag)}: {GitBaseTag}");
        handler.Info($"{nameof(GitCommit)}: {GitCommit}");
        handler.Info($"{nameof(GitCommits)}: {GitCommits}");
        handler.Info($"{nameof(GitIsDirty)}: {GitIsDirty}");
        handler.Info($"{nameof(GitSha)}: {GitSha}");
        handler.Info($"{nameof(GitTag)}: {GitTag}");

        ClassInjector.RegisterTypeInIl2Cpp<ErrorText>();

        Task.Run(SystemEnvironment.SetEnvironmentVariablesAsync);

        Harmony.PatchAll();

        if (DebugModeManager.AmDebugger) ConsoleManager.CreateConsole();
        else ConsoleManager.DetachConsole();

        Msg("========= FinalSuspect loaded! =========", "Plugin Load");
        Application.quitting += new Action(SaveNowLog);
    }

#pragma warning disable CS0618 // 类型或成员已过时
    public const string GitBaseTag = ThisAssembly.Git.BaseTag;
    public const string GitCommit = ThisAssembly.Git.Commit;
    public const string GitCommits = ThisAssembly.Git.Commits;
    public const string GitBranch = ThisAssembly.Git.Branch;
    public const bool GitIsDirty = ThisAssembly.Git.IsDirty;
    public const string GitSha = ThisAssembly.Git.Sha;
    public const string GitTag = ThisAssembly.Git.Tag;
#pragma warning restore CS0618
}

/// <summary>
///     表示软件版本的不同类型。
/// </summary>
public enum VersionTypes
{
    /// <summary>早期内测版。</summary>
    Alpha,

    /// <summary>内测版。</summary>
    Beta,

    /// <summary>测试版（不稳定）。</summary>
    Canary,

    /// <summary>开发版。</summary>
    Dev,

    /// <summary>发行候选版 (Release Candidate)。</summary>
    RC,

    /// <summary>预览/预发行版。</summary>
    Preview,

    /// <summary>废弃版。</summary>
    Scrapter,

    /// <summary>
    ///     正式发行版。
    ///     除此之外若要发行，全部使用OpenBeta。
    /// </summary>
    Release
}

public enum BypassType
{
    Dont,
    Once,
    LongTerm
}

public enum OutfitType
{
    BeanMode,
    HorseMode,
    LongMode
}