using FinalSuspect.Helpers;
using FinalSuspect.Modules.ClientActions.FeatureItems.MainMenuStyle;
using Il2CppSystem;
using UnityEngine;

namespace FinalSuspect.Patches.System;

[HarmonyPatch]
public class MainMenuButtonHoverAnimation
{
    private static Dictionary<GameObject, (Vector3, bool)> AllButtons = new();

    public static bool Active = true;

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
    [HarmonyPostfix]
    [HarmonyPriority(Priority.Last)]
    private static void Start_Postfix(MainMenuManager __instance)
    {
        var mainButtons = GameObject.Find("Main Buttons");

        mainButtons.ForEachChild((Action<GameObject>)Init);
    }

    private static void SetButtonStatus(GameObject obj, bool active)
    {
        AllButtons.TryAdd(obj, (obj.transform.position, active));
        AllButtons[obj] = (AllButtons[obj].Item1, active);
    }

    [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.LateUpdate))]
    [HarmonyPostfix]
    private static void Update_Postfix(MainMenuManager __instance)
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            VersionShowerStartPatch.ModLogo.SetActive(Active);
            VersionShowerStartPatch.TeamLogo.SetActive(Active);
            Active = !Active;
            __instance.mainMenuUI.SetActive(Active);
            VersionShowerStartPatch.CreditTextCredential.gameObject.SetActive(Active);
            VersionShowerStartPatch.VisitText.gameObject.SetActive(Active);
            DestroyableSingleton<AccountTab>.Instance.gameObject.SetActive(Active);
            TitleLogoPatch.ModStamp.SetActive(Active);
        }

        if (!GameObject.Find("MainUI")) return;

        var style = MainMenuStyleManager.BackGroundStyles[Main.CurrentBackgroundId.Value];
        FormatButtonColor(__instance, __instance.newsButton,
            !ModNewsHistory.AnnouncementLoadComplete
                ? ColorHelper.ConvertToLightGray(style.MainUIColors[1])
                : style.MainUIColors[1], new Color(0f, 0f, 0f, 0f), Color.white, Color.white);

        __instance.newsButton.enabled = ModNewsHistory.AnnouncementLoadComplete;
        foreach (var (button, value) in AllButtons.Where(x => x.Key != null && x.Key.active))
        {
            var pos = button.transform.position;
            var targetPos = value.Item1 + new Vector3(value.Item2 ? 0.35f : 0f, 0f, 0f);
            if (value.Item2 && pos.x > value.Item1.x + 0.2f) continue;
            button.transform.position = value.Item2
                ? Vector3.Lerp(pos, targetPos, Time.deltaTime * 2f)
                : Vector3.MoveTowards(pos, targetPos, Time.deltaTime * 2f);
        }
    }

    public static void RefreshButtons(GameObject obj)
    {
        AllButtons = new Dictionary<GameObject, (Vector3, bool)>();
        obj.ForEachChild((Action<GameObject>)Init);
    }

    private static void Init(GameObject obj)
    {
        if (obj.name is "BottomButtonBounds" or "Divider") return;
        if (AllButtons.ContainsKey(obj)) return;
        SetButtonStatus(obj, false);
        var pb = obj.GetComponent<PassiveButton>();
        pb.OnMouseOver.AddListener((global::System.Action)(() => SetButtonStatus(obj, true)));
        pb.OnMouseOut.AddListener((global::System.Action)(() => SetButtonStatus(obj, false)));
    }
}