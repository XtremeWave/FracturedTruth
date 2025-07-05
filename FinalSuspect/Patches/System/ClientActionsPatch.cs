using System;
using System.IO;
using BepInEx.Configuration;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.ClientActions;
using FinalSuspect.Modules.ClientActions.FeatureItems;
using FinalSuspect.Modules.ClientActions.FeatureItems.NameTag;
using FinalSuspect.Modules.ClientActions.FeatureItems.Resources;
using FinalSuspect.Modules.SoundInterface;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FinalSuspect.Patches.System;

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Start))]
public static class OptionsMenuBehaviourStartPatch
{
    private static ClientOptionItem<bool> UnlockFPS;
    private static ClientOptionItem<OutfitType> SwitchOutfitType;
    private static ClientOptionItem<bool> KickPlayerWithAbnormalFriendCode;
    private static ClientOptionItem<bool> KickPlayerWithDenyName;
    private static ClientOptionItem<bool> KickPlayerInBanList;
    private static ClientOptionItem<bool> SpamDenyWord;
    private static ClientOptionItem<bool> AutoStartGame;
    private static ClientOptionItem<bool> AutoEndGame;

    private static ClientOptionItem<bool> DisableVanillaSound;
    private static ClientOptionItem<bool> DisableFAC;
    private static ClientOptionItem<bool> ShowPlayerInfo;
    private static ClientOptionItem<bool> UseModCursor;
    private static ClientOptionItem<bool> FastLaunchMode;
    private static ClientFeatureItem ClearAutoLogs;
    private static ClientFeatureItem DumpLog;
    private static ClientFeatureItem UnloadMod;
    private static ClientOptionItem<bool> VersionCheat;
    private static ClientOptionItem<bool> GodMode;
    private static ClientOptionItem<bool> NoGameEnd;

    //public static ClientFeatureItem SoundBtn;
    //public static ClientFeatureItem AudioManagementBtn;
    private static ClientFeatureItem ResourceBtn;
    private static ClientFeatureItem DisplayNameBtn;
    public static OptionsMenuBehaviour Instance { get; private set; }
    private static bool reseted;
    public static bool recreate;

    public static void Postfix(OptionsMenuBehaviour __instance)
    {
        if (!__instance.DisableMouseMovement) return;
        Instance = __instance;

        if (!reseted || !DebugModeManager.AmDebugger)
        {
            reseted = true;
            Main.VersionCheat.Value = false;
            Main.GodMode.Value = false;
            Main.NoGameEnd.Value = false;
        }

        if (recreate)
        {
            ClientActionItem.ModOptionsButton.gameObject.SetActive(false);
            Object.Destroy(ClientActionItem.ModOptionsButton);
            Object.Destroy(ClientActionItem.CustomBackground);
            ClientFeatureItem.ModOptionsButton.gameObject.SetActive(false);
            Object.Destroy(ClientFeatureItem.ModOptionsButton);
            Object.Destroy(ClientFeatureItem.CustomBackground);

            Object.Destroy(ModUnloaderScreen.Popup);
            //Object.Destroy(MyMusicPanel.CustomBackground);
            //Object.Destroy(SoundManagementPanel.CustomBackground);
            ClientActionItem.ModOptionsButton = null;
            ClientActionItem.CustomBackground = null;

            ClientFeatureItem.ModOptionsButton = null;
            ClientFeatureItem.CustomBackground = null;

            ModUnloaderScreen.Popup = null;
            //MyMusicPanel.CustomBackground = null;
            //SoundManagementPanel.CustomBackground = null;
        }

        CreateOptionItem(ref UnlockFPS, "UnlockFPS", Main.UnlockFPS, __instance, UnlockFPSButtonToggle);
        CreateOptionItem(ref SwitchOutfitType, "SwitchOutfitType", Main.SwitchOutfitType, __instance, SwitchMode);
        CreateOptionItem(ref KickPlayerWithAbnormalFriendCode, "KickPlayerWithAbnormalFriendCode",
            Main.KickPlayerWithAbnormalFriendCode, __instance);
        CreateOptionItem(ref KickPlayerInBanList, "KickPlayerInBanList", Main.KickPlayerInBanList, __instance);
        CreateOptionItem(ref KickPlayerWithDenyName, "KickPlayerWithDenyName", Main.KickPlayerWithDenyName, __instance);
        CreateOptionItem(ref SpamDenyWord, "SpamDenyWord", Main.SpamDenyWord, __instance);
        CreateOptionItem(ref AutoStartGame, "AutoStartGame", Main.AutoStartGame, __instance, AutoStartButtonToggle);
        CreateOptionItem(ref AutoEndGame, "AutoEndGame", Main.AutoEndGame, __instance);
        //CreateOptionItem<bool>(ref PrunkMode, "PrunkMode", Main.PrunkMode, __instance);
        CreateOptionItem(ref DisableVanillaSound, "DisableVanillaSound", Main.DisableVanillaSound, __instance, () =>
        {
            if (Main.DisableVanillaSound.Value)
                CustomSoundsManager.StopPlayVanilla();
            else
            {
                CustomSoundsManager.StartPlayVanilla();
            }
        });
        CreateOptionItem(ref DisableFAC, "DisableFAC", Main.DisableFAC, __instance);
        CreateOptionItem(ref ShowPlayerInfo, "ShowPlayerInfo", Main.ShowPlayerInfo, __instance);
        CreateOptionItem(ref UseModCursor, "UseModCursor", Main.UseModCursor, __instance, SetCursor);
        CreateOptionItem(ref FastLaunchMode, "FastLaunchMode", Main.FastLaunchMode, __instance);
        if (DebugModeManager.AmDebugger)
        {
            CreateOptionItem(ref VersionCheat, "VersionCheat", Main.VersionCheat, __instance);
            CreateOptionItem(ref GodMode, "GodMode", Main.GodMode, __instance);
            CreateOptionItem(ref NoGameEnd, "NoGameEnd", Main.NoGameEnd, __instance);
        }

        CreateFeatureItem(ref DumpLog, "DumpLog", () => { DumpLog(); }, __instance);
        CreateFeatureItem(ref ClearAutoLogs, "ClearAutoLogs", () =>
        {
            ClearAutoLogs();
            SetFeatureItemDisabled(ClearAutoLogs);
        }, __instance);
        CreateFeatureItem(ref UnloadMod, "UnloadMod", ModUnloaderScreen.Show, __instance);

        //CreateFeatureItem(ref SoundBtn, "SoundOption",
            //() => { MyMusicPanel.CustomBackground?.gameObject.SetActive(true); }, __instance);
        //CreateFeatureItem(ref AudioManagementBtn, "SoundManager",
            //() => { SoundManagementPanel.CustomBackground?.gameObject.SetActive(true); }, __instance);
        CreateFeatureItem(ref ResourceBtn, "ResourceManager",
            () => { ResourcesPanel.CustomBackground?.gameObject.SetActive(true); }, __instance);
        CreateFeatureItem(ref DisplayNameBtn, "DisplayName",
            () => { NameTagPanel.CustomBackground?.gameObject.SetActive(true); }, __instance);

        //SetFeatureItemTextAndColor(SoundBtn, "SoundOptions");
        //SetFeatureItemTextAndColor(AudioManagementBtn, "AudioManagementOptions");
        SetFeatureItemTextAndColor(ResourceBtn, "ResourceManager");
        SetFeatureItemTextAndColor(DisplayNameBtn, "NameTagPanel");
        if (!IsNotJoined)
        {
            //SetOptionItemDisabled_Menu(SwitchOutfitType);
            SetFeatureItemDisabled_Menu(ResourceBtn);
            //SetFeatureItemDisabled_Menu(AudioManagementBtn);
        }

        if (Directory.GetFiles(GetLogFolder(true).FullName + "/Final Suspect-logs").Length <= 0)
        {
            SetFeatureItemDisabled(ClearAutoLogs);
        }

        Modules.SoundInterface.SoundManager.ReloadTag();
        //MyMusicPanel.Init(__instance);
        //SoundManagementPanel.Init(__instance);
        ResourcesPanel.Init(__instance);
        NameTagPanel.Init(__instance);

        if (!ModUnloaderScreen.Popup)
            ModUnloaderScreen.Init(__instance);
        recreate = false;
    }

    private static void CreateOptionItem<T>(ref ClientOptionItem<T> item, string name, ConfigEntry<T> value,
        OptionsMenuBehaviour instance, Action toggleAction = null)
    {
        if (recreate)
        {
            Object.Destroy(item.ToggleButton.gameObject);
            item = null;
        }

        if (item == null || !item.ToggleButton)
        {
            item = ClientOptionItem<T>.Create(name, value, instance, toggleAction);
        }
    }

    /*private static void CreateActionItem(ref ClientActionItem item, string name, Action action, OptionsMenuBehaviour instance)
    {
        if (recreate)
        {
            Object.Destroy(item.ToggleButton.gameObject);
            item = null;
        }

        if (item == null || !item.ToggleButton)
        {
            item = ClientActionItem.Create(name, action, instance);
        }
    }*/

    private static void CreateFeatureItem(ref ClientFeatureItem item, string name, Action action,
        OptionsMenuBehaviour instance)
    {
        if (recreate)
        {
            Object.Destroy(item.ToggleButton.gameObject);
            item = null;
        }

        if (item == null || !item.ToggleButton)
        {
            item = ClientFeatureItem.Create(name, action, instance);
        }
    }

    private static void SetFeatureItemTextAndColor(ClientFeatureItem item, string text)
    {
        item.ToggleButton.Text.text = GetString("ClientFeature." + text);
        item.ToggleButton.GetComponent<PassiveButton>().enabled = true;
        item.ToggleButton.Background.color = ColorHelper.ClientFeatureColor;
    }

    /*private static void SetOptionItemDisabled(ClientOptionItem_Boolean item)
    {
        item.ToggleButton.Text.text += $"\n|{GetString("OnlyAvailableInMainMenu")}|";
        item.ToggleButton.GetComponent<PassiveButton>().enabled = false;
        item.ToggleButton.Background.color = ColorHelper.ClientOptionColor_CanNotUse;
    }*/

    private static void SetOptionItemDisabled_Menu<T>(ClientOptionItem<T> item)
    {
        item.Rename();
        item.ToggleButton.Text.text += $"\n|{GetString("Tip.OnlyAvailableInMainMenu")}|";
        item.ToggleButton.GetComponent<PassiveButton>().enabled = false;
        item.ToggleButton.Background.color = ColorHelper.ClientOptionColor_CanNotUse;
    }

    private static void SetFeatureItemDisabled_Menu(ClientFeatureItem item)
    {
        item.ToggleButton.Text.text += $"\n|{GetString("Tip.OnlyAvailableInMainMenu")}|";
        SetFeatureItemDisabled(item);
    }

    private static void SetFeatureItemDisabled(ClientFeatureItem item)
    {
        item.ToggleButton.GetComponent<PassiveButton>().enabled = false;
        item.ToggleButton.Background.color = ColorHelper.ClientFeatureColor_CanNotUse;
    }

    /*private static void SetFeatureItemEnable(ClientFeatureItem item)
    {
        item.ToggleButton.GetComponent<PassiveButton>().enabled = true;
        item.ToggleButton.Background.color = ColorHelper.ClientFeatureColor;
    }*/

    private static void UnlockFPSButtonToggle()
    {
        Application.targetFrameRate = Main.UnlockFPS.Value ? 165 : 60;
        SendInGame(string.Format(GetString("Notification.FPSSetTo"), Application.targetFrameRate));
    }

    private static void SwitchMode()
    {
        foreach (var pc in Main.AllPlayerControls)
        {
            pc.MyPhysics.SetBodyType(pc.BodyType);
            if (pc.BodyType == PlayerBodyTypes.Normal) pc.cosmetics.currentBodySprite.BodySprite.transform.localScale = new(0.5f, 0.5f, 1f);
        }
    }

    private static void AutoStartButtonToggle()
    {
        if (Main.AutoStartGame.Value == false && IsCountDown)
        {
            GameStartManager.Instance.ResetStartState();
        }
    }

    public static void SetCursor()
    {
        try
        {
            var sprite = LoadSprite("Cursor.png");
            Cursor.SetCursor(Main.UseModCursor.Value ? sprite.texture : null, Vector2.zero, CursorMode.Auto);
        }
        catch
        {
            Main.UseModCursor.Value = false;
        }
    }
}

[HarmonyPatch(typeof(OptionsMenuBehaviour), nameof(OptionsMenuBehaviour.Close))]
public static class OptionsMenuBehaviourClosePatch
{
    public static void Postfix()
    {
        ClientActionItem.CustomBackground?.gameObject.SetActive(false);
        ClientFeatureItem.CustomBackground?.gameObject.SetActive(false);
        ModUnloaderScreen.Hide();
        ResourcesPanel.Hide();
        NameTagPanel.Hide();
        //MyMusicPanel.Hide();
        //SoundManagementPanel.Hide();
    }
}

[HarmonyPatch(typeof(LanguageSetter), nameof(LanguageSetter.SetLanguage))]
public static class LanguageSetterSetLanguagePatch
{
    public static void Postfix()
    {
        OptionsMenuBehaviourStartPatch.recreate = true;
        try
        {
            Object.Destroy(VersionShowerStartPatch.VisitText);
        }
        catch
        {
            /* ignored */
        }

        VersionShowerStartPatch.VisitText = null;
        VersionShowerStartPatch.CreateVisitText(null);
        OptionsMenuBehaviourStartPatch.Postfix(OptionsMenuBehaviourStartPatch.Instance);
    }
}