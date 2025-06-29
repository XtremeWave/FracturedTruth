using System.Text;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using TMPro;
using UnityEngine;

namespace FinalSuspect.Modules.Features
{
    public static class InGameSettingsMenu
    {
        public static bool Showing => Fill && Fill.active && Menu && Menu.active;

        public static GameObject Fill;
        public static SpriteRenderer FillSP => Fill.GetComponent<SpriteRenderer>();

        public static GameObject Menu;

        public static GameObject Settings;

        public static TextMeshPro SettingsTMP => Settings.GetComponent<TextMeshPro>();

        public static void Init()
        {
            var DOBScreen = AccountManager.Instance.transform.FindChild("DOBEnterScreen");

            Fill = new GameObject("FinalSuspect Settings Menu Fill") { layer = 5 };
            Fill.transform.SetParent(HudManager.Instance.transform.parent, true);
            Fill.transform.localPosition = new Vector3(0f, 0f, -980f);
            Fill.transform.localScale = new Vector3(20f, 10f, 1f);
            Fill.AddComponent<SpriteRenderer>().sprite = DOBScreen.FindChild("Fill").GetComponent<SpriteRenderer>().sprite;
            FillSP.color = new Color(0f, 0f, 0f, 0.75f);

            Menu = Object.Instantiate(DOBScreen.FindChild("InfoPage").gameObject, HudManager.Instance.transform.parent);
            Menu.name = "FinalSuspect Settings Menu Page";
            Menu.transform.SetLocalZ(-990f);

            Object.Destroy(Menu.transform.FindChild("Title Text").gameObject);
            Object.Destroy(Menu.transform.FindChild("BackButton").gameObject);
            Object.Destroy(Menu.transform.FindChild("EvenMoreInfo").gameObject);

            Settings = Menu.transform.FindChild("InfoText_TMP").gameObject;
            Settings.name = "Settings";
            Settings.DestroyTranslator();
            Settings.transform.localPosition = new Vector3(-2.3f, 0.8f, 4f);
            Settings.GetComponent<RectTransform>().sizeDelta = new Vector2(4.5f, 10f);
            SettingsTMP.alignment = TextAlignmentOptions.Left;
            SettingsTMP.fontSize = 2f;
        }

        public static void SetSettingsRef(PlayerControl player)
        {
            if (!player) return;
            if (!Fill || !Menu) Init();
            var builder = new StringBuilder(256);
            builder.AppendFormat("<size={0}>{1}\n", BodySize, GameOptionsManager.Instance.CurrentGameOptions.ToHudString(PlayerControl.AllPlayerControls.Count));
            SettingsTMP.text = builder.ToString();
        }

        public static void Show()
        {
            if (!Fill || !Menu) Init();
            if (!Showing)
            {
                Fill?.SetActive(true);
                Menu?.SetActive(true);
            }
            //HudManager.Instance?.gameObject.SetActive(false);
        }

        public static void Hide()
        {
            if (Showing)
            {
                Fill?.SetActive(false);
                Menu?.SetActive(false);
            }
            //HudManager.Instance?.gameObject?.SetActive(true);
        }

        public const string BodySize = "70%";
    }
}
