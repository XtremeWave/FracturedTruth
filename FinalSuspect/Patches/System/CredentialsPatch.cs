using System.Collections.Generic;
using System.Text;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Resources;
using FinalSuspect.Patches.Game_Vanilla;
using FinalSuspect.Templates;
using Il2CppSystem;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ColorHelper = FinalSuspect.Helpers.ColorHelper;
using Object = UnityEngine.Object;

namespace FinalSuspect.Patches.System;

[HarmonyPatch(typeof(PingTracker), nameof(PingTracker.Update))]
internal class PingTrackerUpdatePatch
{
    private static float deltaTime;
    public static string ServerName = "";
    private static TextMeshPro CreditTextCredential;
    private static AspectPosition CreditTextCredentialAspectPos;

    public static void Postfix(PingTracker __instance)
    {
        if (!CreditTextCredential)
        {
            var uselessPingTracker = Object.Instantiate(__instance, __instance.transform.parent);
            CreditTextCredential = uselessPingTracker.GetComponent<TextMeshPro>();
            Object.Destroy(uselessPingTracker);
            CreditTextCredential.alignment = TextAlignmentOptions.TopRight;
            CreditTextCredential.color = new Color(1f, 1f, 1f, 0.7f);
            CreditTextCredential.rectTransform.pivot = new Vector2(1f, 1f); // 将中心点设定在右上角
            CreditTextCredentialAspectPos = CreditTextCredential.GetComponent<AspectPosition>();
            CreditTextCredentialAspectPos.Alignment = AspectPosition.EdgeAlignments.RightTop;
        }

        if (CreditTextCredentialAspectPos)
        {
            CreditTextCredentialAspectPos.DistanceFromEdge =
                DestroyableSingleton<HudManager>.InstanceExists &&
                DestroyableSingleton<HudManager>.Instance.Chat.chatButton.gameObject.active
                    ? new Vector3(2.5f, 0f, -800f)
                    : new Vector3(1.8f, 0f, -800f);
        }

        StringBuilder sb = new();

        sb.Append(Main.CredentialsText);

        CreditTextCredential.text = sb.ToString();
        if (
            (GameSettingMenu.Instance?.gameObject.active ?? false)
            || IsMeeting
            || (FriendsListUI.Instance?.gameObject.active ?? false)
            || (HudManagerPatch.showHideButton?.Button?.gameObject.active ?? false) && Main.ShowResults.Value)
            CreditTextCredential.text = "";

        var ping = AmongUsClient.Instance.Ping;
        var color = ping switch
        {
            < 50 => "#44dfcc",
            < 100 => "#7bc690",
            < 200 => "#f3920e",
            < 400 => "#ff146e",
            _ => "#ff4500"
        };

        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
        var fps = Mathf.Ceil(1.0f / deltaTime);

        __instance.text.alignment = TextAlignmentOptions.TopGeoAligned;
        __instance.text.text =
            $"<color={color}>{GetString("Ping")}:{ping} <size=60%>ms</size></color>" + "  "
            + $"<color=#00a4ff>{GetString("FrameRate")}:{fps} <size=60%>FPS</size></color>" +
            $"{"    <color=#FFDCB1>◈</color>" + (IsOnlineGame ? ServerName : GetString("Local"))}";
    }
}

[HarmonyPatch(typeof(VersionShower), nameof(VersionShower.Start))]
public class VersionShowerStartPatch
{
    public static GameObject OVersionShower;
    public static TextMeshPro VisitText;
    public static TextMeshPro CreditTextCredential;
    public static GameObject ModLogo;
    public static GameObject TeamLogo;

    public static void Postfix(VersionShower __instance)
    {
        TMPTemplate.SetBase(__instance.text);

        Main.CredentialsText = $"\r\n<size=120%>" +
                               $"<color={ColorHelper.TeamColor}>==</color> <color={ColorHelper.ModColor}>{Main.ModName}</color> <color={ColorHelper.TeamColor}>==</color>"
                               + "</size>";
        Main.CredentialsText += "\r\n <color=#fffcbe> By </color><color=#cdfffd>XtremeWave</color></size>";
        Main.CredentialsText += $"\r\n<color=#C8FF78>v{Main.DisplayedVersion}</color>";


#if !DEBUG
        var additionalCredentials = GetString("TextBelowVersionText");
        if (additionalCredentials != null && additionalCredentials != "*" && additionalCredentials != "")
        {
            Main.CredentialsText += $"\r\n{additionalCredentials}";
        }
#endif
#if !RELEASE
        Main.CredentialsText += $"\r\n<color={ColorHelper.ModColor}>{Main.GitBranch}</color> - {Main.GitCommit}";
#endif

        ErrorText.Create(__instance.text);
        if (Main.hasArgumentException && ErrorText.Instance)
            ErrorText.Instance.AddError(ErrorCode.Main_DictionaryError);

        if ((OVersionShower = GameObject.Find("VersionShower")) && !VisitText)
        {
            CreateVisitText(__instance);
        }

        if ((OVersionShower = GameObject.Find("VersionShower")) && !CreditTextCredential)
        {
            var credentialsText = string.Format(GetString("MainMenuCredential"),
                $"<color={ColorHelper.TeamColor}>XtremeWave</color>");
            credentialsText += "\n";
#if DEBUG
            var versionText = $"<color={ColorHelper.ModColor}>{Main.GitBranch}</color> - {Main.GitCommit}";
#elif RELEASE
            var versionText =
                $"<color={ColorHelper.ModColor}>FS</color> - <color=#C8FF78>v{Main.DisplayedVersion}</color>";
#elif OPENBETA
            var versionText =
                $"<color={ColorHelper.ModColor}>{Main.GitBranch}</color> - {Main.GitCommit}\n" +
                $"<color={ColorHelper.ModColor}>FS</color> - <color=#C8FF78>v{Main.DisplayedVersion}</color>";
#endif
            credentialsText += versionText;

            CreditTextCredential = Object.Instantiate(__instance.text);
            CreditTextCredential.name = "FinalSuspect CreditText";
            CreditTextCredential.alignment = TextAlignmentOptions.Right;
            CreditTextCredential.text = credentialsText;
            CreditTextCredential.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);

            CreditTextCredential.enabled = GameObject.Find("FinalSuspect Background");
            CreditTextCredential.SetOutlineColor(ColorHelper.ShadeColor(ColorHelper.ModColor32, 0.75f));
            CreditTextCredential.SetOutlineThickness(0.20f);
            CreditTextCredential.fontStyle = FontStyles.Bold;
            var ap_credit = CreditTextCredential.gameObject.AddComponent<AspectPosition>();
            ap_credit.Alignment = AspectPosition.EdgeAlignments.RightBottom;
            ap_credit.DistanceFromEdge = new Vector3(5f, 0.4f);
            ap_credit.updateAlways = true;
        }

        TeamLogo = new GameObject
        {
            layer = 5,
            name = "Team Logo"
        };
        TeamLogo.AddComponent<SpriteRenderer>().sprite = LoadSprite("TeamLogo.png", 400f);
        TeamLogo.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 120);
        TeamLogo.transform.SetParent(VisitText.transform.parent);
        var ap_teamLogo = TeamLogo.gameObject.AddComponent<AspectPosition>();
        ap_teamLogo.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        ap_teamLogo.DistanceFromEdge = new Vector3(0.6f, 0.5f);
        ap_teamLogo.updateAlways = true;

        TeamLogo.SetActive(false);
        ModLogo = new GameObject
        {
            layer = 5,
            name = "Mod Logo"
        };
        ModLogo.AddComponent<SpriteRenderer>().sprite = LoadSprite("FinalSuspect-Logo.png", 250f);
        ModLogo.GetComponent<SpriteRenderer>().color = new Color32(255, 255, 255, 120);
        var ap_modLogo = ModLogo.gameObject.AddComponent<AspectPosition>();
        ap_modLogo.Alignment = AspectPosition.EdgeAlignments.RightBottom;
        ap_modLogo.DistanceFromEdge = new Vector3(1.6f, 0.4f);
        ap_modLogo.updateAlways = true;
        ModLogo.SetActive(false);
    }

    private static VersionShower Instance;

    public static void CreateVisitText(VersionShower __instance)
    {
        if (!__instance)
            __instance = Instance;
        else
        {
            Instance = __instance;
        }

        VisitText = Object.Instantiate(__instance.text);
        VisitText.name = "FinalSuspect VisitText";
        VisitText.alignment = TextAlignmentOptions.Left;
        VisitText.text = VersionChecker.isChecked
            ? string.Format(GetString("FinalSuspectWelcomeText"), ColorHelper.ModColor)
            : GetString("ConnectToFinalSuspectServerFailed");
        VisitText.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        VisitText.enabled = GameObject.Find("FinalSuspect Background");

        __instance.text.alignment = TextAlignmentOptions.Left;
        var ap1 = OVersionShower.GetComponent<AspectPosition>();
        ap1.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        ap1.DistanceFromEdge = new Vector3(0.4f, -0.3f);
        ap1.updateAlways = true;

        var ap2 = VisitText.gameObject.AddComponent<AspectPosition>();
        ap2.Alignment = AspectPosition.EdgeAlignments.LeftBottom;
        ap2.DistanceFromEdge = new Vector3(1.4f, 0.1f);
        ap2.updateAlways = true;
    }
}

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPriority(Priority.First)]
internal class TitleLogoPatch
{
    public static GameObject ModStamp;
    public static GameObject FinalSuspect_Background;
    public static GameObject Ambience;
    public static GameObject Starfield;
    public static GameObject LeftPanel;
    public static GameObject RightPanel;
    public static GameObject CloseRightButton;
    public static GameObject Tint;
    public static GameObject Sizer;
    public static GameObject AULogo;
    public static GameObject BottomButtonBounds;

    public static Vector3 RightPanelOp = new(2.8f, -0.4f, -5.0f);

    public static void Postfix(MainMenuManager __instance)
    {
        GameObject.Find("BackgroundTexture")?.SetActive(!MainMenuManagerPatch.ShowedBak);

        Color shade = new(0f, 0f, 0f, 0f);
        var standardActiveSprite = __instance.newsButton.activeSprites.GetComponent<SpriteRenderer>().sprite;
        var minorActiveSprite = __instance.quitButton.activeSprites.GetComponent<SpriteRenderer>().sprite;

        var friendsButton = AwakeFriendCodeUIPatch.FriendsButton.GetComponent<PassiveButton>();
        Dictionary<List<PassiveButton>, (Sprite, Color, Color, Color, Color)> mainButtons = new()
        {
            {
                [__instance.playButton, __instance.inventoryButton, __instance.shopButton],
                (standardActiveSprite, new Color(0.5216f, 1f, 0.9490f, 0.8f), shade, Color.white, Color.white)
            },
            {
                [__instance.newsButton, __instance.myAccountButton, __instance.settingsButton],
                (minorActiveSprite, new Color(0.5216f, 0.7765f, 1f, 0.8f), shade, Color.white, Color.white)
            },
            {
                [__instance.creditsButton, __instance.quitButton],
                (minorActiveSprite, new Color(0.7294f, 0.6353f, 1.0f, 0.8f), shade, Color.white, Color.white)
            },
            {
                [friendsButton],
                (minorActiveSprite, new Color(0.0235f, 0f, 0.8f, 0.8f), shade, Color.white, Color.white)
            },
        };

        // ReSharper disable once UnusedParameter.Local


        foreach (var kvp in mainButtons)
        {
            kvp.Key.Do(button =>
            {
                FormatButtonColor(__instance, button, kvp.Value.Item2, kvp.Value.Item3, kvp.Value.Item4,
                    kvp.Value.Item5);
            });
        }

        try
        {
            mainButtons.Keys.Flatten()?.DoIf(x => x, x => x.buttonText.color = Color.white);
        }
        catch
        {
            /* ignored */
        }

        if (!(ModStamp = GameObject.Find("ModStamp"))) return;
        ModStamp.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
        ModStamp.GetComponent<SpriteRenderer>().sprite = LoadSprite("ModStamp.png", 100f);

        FinalSuspect_Background = new GameObject("FinalSuspect Background")
        {
            transform =
            {
                position = new Vector3(0, 0, 520f)
            }
        };
        var bgRenderer = FinalSuspect_Background.AddComponent<SpriteRenderer>();
        bgRenderer.sprite = LoadSprite("FinalSuspect-BG-MiraHQ.jpg", 179f);

        if (!(Ambience = GameObject.Find("Ambience"))) return;
        if (!(Starfield = Ambience.transform.FindChild("starfield").gameObject)) return;
        var starGen = Starfield.GetComponent<StarGen>();
        starGen.SetDirection(new Vector2(0, -2));
        Starfield.transform.SetParent(FinalSuspect_Background.transform);
        Object.Destroy(Ambience);

        if (!(LeftPanel = GameObject.Find("LeftPanel"))) return;
        LeftPanel.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        LeftPanel.ForEachChild((Action<GameObject>)ResetParent);
        LeftPanel.SetActive(false);

        GameObject.Find("Divider")?.SetActive(false);

        if (!(RightPanel = GameObject.Find("RightPanel"))) return;
        var rpap = RightPanel.GetComponent<AspectPosition>();
        if (rpap) Object.Destroy(rpap);
        RightPanel.transform.localPosition = RightPanelOp + new Vector3(10f, 0f, 0f);
        RightPanel.GetComponent<SpriteRenderer>().color = new Color(1f, 0.78f, 0.9f, 1f);

        CloseRightButton = new GameObject("CloseRightPanelButton");
        CloseRightButton.transform.SetParent(RightPanel.transform);
        CloseRightButton.transform.localPosition = new Vector3(-4.78f * GetResolutionOffset(), 1.3f, 1f);
        CloseRightButton.transform.localScale = new Vector3(1f, 1f, 1f);
        CloseRightButton.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 1.5f);
        var closeRightSpriteRenderer = CloseRightButton.AddComponent<SpriteRenderer>();
        closeRightSpriteRenderer.sprite = LoadSprite("RightPanelCloseButton.png", 100f);
        closeRightSpriteRenderer.color = new Color(1f, 0.78f, 0.9f, 1f);
        var closeRightPassiveButton = CloseRightButton.AddComponent<PassiveButton>();
        closeRightPassiveButton.OnClick = new Button.ButtonClickedEvent();
        closeRightPassiveButton.OnClick.AddListener((global::System.Action)MainMenuManagerPatch.HideRightPanel);
        closeRightPassiveButton.OnMouseOut = new UnityEvent();
        closeRightPassiveButton.OnMouseOut.AddListener((global::System.Action)(() =>
            closeRightSpriteRenderer.color = new Color(1f, 0.78f, 0.9f, 1f)));
        closeRightPassiveButton.OnMouseOver = new UnityEvent();
        closeRightPassiveButton.OnMouseOver.AddListener((global::System.Action)(() =>
            closeRightSpriteRenderer.color = new Color(1f, 0.68f, 0.99f, 1f)));

        Tint = __instance.screenTint.gameObject;
        var ttap = Tint.GetComponent<AspectPosition>();
        if (ttap) Object.Destroy(ttap);
        Tint.transform.SetParent(RightPanel.transform);
        Tint.transform.localPosition = new Vector3(-0.0824f * GetResolutionOffset(), 0.0513f, Tint.transform.localPosition.z);
        Tint.transform.localScale = new Vector3(1f, 1f, 1f);

        var creditsScreen = __instance.creditsScreen;
        if (creditsScreen)
        {
            var csto = creditsScreen.GetComponent<TransitionOpen>();
            if (csto) Object.Destroy(csto);
            var closeButton = creditsScreen.transform.FindChild("CloseButton");
            closeButton?.gameObject.SetActive(false);
        }

        if (!(Sizer = GameObject.Find("Sizer"))) return;
        if (!(AULogo = GameObject.Find("LOGO-AU"))) return;
        Sizer.transform.localPosition =
            new Vector3(-4.0f * GetResolutionOffset(), 1.4f, -1.0f);
        AULogo.transform.localScale = new Vector3(0.66f, 0.67f, 1f);
        AULogo.transform.position += new Vector3(0f, 0.1f, 0f);
        var logoRenderer = AULogo.GetComponent<SpriteRenderer>();
        logoRenderer.sprite = LoadSprite("FinalSuspect-Logo.png");

        if (!(BottomButtonBounds = GameObject.Find("BottomButtonBounds"))) return;
        BottomButtonBounds.transform.localPosition -= new Vector3(0f, 0.1f, 0f);

        var mainButtonsobj = GameObject.Find("Main Buttons");
        mainButtonsobj.transform.position = new Vector3(-3.4f * GetResolutionOffset(),
            mainButtonsobj.transform.position.y, mainButtonsobj.transform.position.z);
        return;
        static void ResetParent(GameObject obj) => obj.transform.SetParent(LeftPanel.transform.parent);
    }
}

[HarmonyPatch(typeof(ModManager), nameof(ModManager.LateUpdate))]
internal class ModManagerLateUpdatePatch
{
    private static bool firstRun;

    public static void Prefix(ModManager __instance)
    {
        __instance.ShowModStamp();
        if (!firstRun)
        {
            OptionsMenuBehaviourStartPatch.SetCursor();
            __instance.ModStamp.sprite = LoadSprite("ModStamp.png", 100f);
            firstRun = true;
        }

        LateTask.Update(Time.deltaTime);
        MainThreadTask.Update();
    }

    public static void Postfix(ModManager __instance)
    {
        var offset_y = HudManager.InstanceExists ? 1.6f : 0.9f;
        __instance.ModStamp.transform.position = AspectPosition.ComputeWorldPosition(
            __instance.localCamera, AspectPosition.EdgeAlignments.RightTop,
            new Vector3(0.4f, offset_y, __instance.localCamera.nearClipPlane + 0.1f));
    }
}

[HarmonyPatch(typeof(CreditsScreenPopUp))]
internal class CreditsScreenPopUpPatch
{
    [HarmonyPatch(nameof(CreditsScreenPopUp.OnEnable))]
    public static void Postfix(CreditsScreenPopUp __instance)
    {
        __instance.BackButton.transform.parent.FindChild("Background").gameObject.SetActive(false);
    }
}

[HarmonyPatch(typeof(ResolutionManager))]
internal class ResolutionManagerPatch
{
    [HarmonyPatch(nameof(ResolutionManager.SetResolution))]
    public static void Postfix(int width, int height)
    {
        _ = new LateTask(() =>
        {
            if (!GameObject.Find("MainUI")) return;
            var offset = GetResolutionOffset();
            TitleLogoPatch.CloseRightButton.transform.localPosition = new Vector3(-4.78f * offset, 1.3f, 1.0f);
            TitleLogoPatch.Tint.transform.localPosition =
                new Vector3(-0.0824f * offset, 0.0513f, TitleLogoPatch.Tint.transform.localPosition.z);
            TitleLogoPatch.Sizer.transform.localPosition = new Vector3(-4.0f * offset, 1.4f, -1.0f);
            var mainButtons = GameObject.Find("Main Buttons");
            mainButtons.transform.position = new Vector3(-3.4f * offset, mainButtons.transform.position.y,
                mainButtons.transform.position.z);
            MainMenuButtonHoverAnimation.RefreshButtons(mainButtons);

            List<GameObject> nullObj = [];
            foreach (var button in MainMenuManagerPatch.MainMenuCustomButtons)
            {
                if (!button)
                {
                    nullObj.Add(button);
                    continue;
                }

                var scale = MainMenuManagerPatch.Instance.quitButton.transform.localScale;
                button.transform.localScale = new Vector3(scale.x * GetResolutionOffset(), button.transform.localScale.y);
            }

            foreach (var obj in nullObj)
            {
                MainMenuManagerPatch.MainMenuCustomButtons.Remove(obj);
            }

            TitleLogoPatch.CloseRightButton.transform.localPosition =
                new Vector3(-4.78f * GetResolutionOffset(), 1.3f, 1f);
        }, 0.01f, "RefreshMenu");
    }
}