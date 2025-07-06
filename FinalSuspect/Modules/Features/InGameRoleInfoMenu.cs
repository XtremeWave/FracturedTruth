﻿using System.Text;
using FinalSuspect.Attributes;
using FinalSuspect.Helpers;
using FinalSuspect.Modules.Core.Game;
using TMPro;
using UnityEngine;

namespace FinalSuspect.Modules.Features;

public static class InGameRoleInfoMenu
{
    public const string FirstHeaderSize = "130%";
    public const string SecondHeaderSize = "100%";
    public const string BodySize = "70%";
    public const string BlankLineSize = "30%";

    public static GameObject Fill;

    public static GameObject Menu;

    public static GameObject RoleInfo;
    public static GameObject RoleIllustration;
    public static bool Showing => Fill && Fill.active && Menu && Menu.active;
    public static SpriteRenderer FillSP => Fill.GetComponent<SpriteRenderer>();
    public static SpriteRenderer RoleIllustrationSP => RoleIllustration.GetComponent<SpriteRenderer>();

    public static TextMeshPro RoleInfoTMP => RoleInfo.GetComponent<TextMeshPro>();

    [GameModuleInitializer]
    public static void Init()
    {
        var DOBScreen = AccountManager.Instance.transform.FindChild("DOBEnterScreen");

        Fill = new GameObject("FinalSuspect Role Info Menu Fill") { layer = 5 };
        Fill.transform.SetParent(HudManager.Instance.transform.parent, true);
        Fill.transform.localPosition = new Vector3(0f, 0f, -980f);
        Fill.transform.localScale = new Vector3(20f, 10f, 1f);
        Fill.AddComponent<SpriteRenderer>().sprite = DOBScreen.FindChild("Fill").GetComponent<SpriteRenderer>().sprite;
        FillSP.color = new Color(0f, 0f, 0f, 0.75f);

        Menu = Object.Instantiate(DOBScreen.FindChild("InfoPage").gameObject, HudManager.Instance.transform.parent);
        Menu.name = "FinalSuspect Role Info Menu Page";
        Menu.transform.SetLocalZ(-990f);

        Object.Destroy(Menu.transform.FindChild("Title Text").gameObject);
        Object.Destroy(Menu.transform.FindChild("BackButton").gameObject);
        Object.Destroy(Menu.transform.FindChild("EvenMoreInfo").gameObject);

        RoleInfo = Menu.transform.FindChild("InfoText_TMP").gameObject;
        RoleInfo.name = "Role Info";
        RoleInfo.DestroyTranslator();
        RoleInfo.transform.localPosition = new Vector3(-2.3f, 0.8f, 4f);
        RoleInfo.GetComponent<RectTransform>().sizeDelta = new Vector2(4.5f, 10f);
        RoleInfoTMP.alignment = TextAlignmentOptions.Left;
        RoleInfoTMP.fontSize = 2f;

        RoleIllustration = new GameObject("Character Illustration") { layer = 5 };
        RoleIllustration.transform.SetParent(Menu.transform);
        RoleIllustration.AddComponent<SpriteRenderer>();
        RoleIllustration.transform.localPosition = new Vector3(2.3f, 0.8f, 4f);

        ForceHide();
    }

    public static void SetRoleInfoRef(PlayerControl player)
    {
        if (!player) return;
        if (!Fill || !Menu) Init();
        var builder = new StringBuilder(256);
        builder.AppendFormat("<size={0}>\n", BlankLineSize);
        // 职业名
        var role = player.Data.Role.Role;
        builder.AppendFormat("<size={0}>{1}", FirstHeaderSize, GetRoleName(role).Color(GetRoleColor(role)));
        // 职业阵营 / 原版职业
        var roleTeam = player.IsImpostor() ? "Imp" : "Crew";
        builder.AppendFormat("<size={0}> ({1})\n", BodySize, GetString($"RoleType.{roleTeam}"));
        builder.AppendFormat("<size={0}>{1}\n", BodySize, player.GetRoleType().GetRoleInfoForVanilla(true) ?? "");
        RoleInfoTMP.text = builder.ToString();
        var HnSPrefix = "";
        if (!IsNormalGame && player.IsAlive())
            HnSPrefix = "HnS";
        RoleIllustrationSP.sprite = LoadSprite($"CI_{HnSPrefix + role}.png", 320f);
    }

    public static void Show()
    {
        if (!Fill || !Menu) Init();
        if (Showing) return;
        Fill?.SetActive(true);
        Menu?.SetActive(true);
        //HudManager.Instance?.gameObject.SetActive(false);
    }

    public static void Hide()
    {
        if (!Showing) return;
        Fill?.SetActive(false);
        Menu?.SetActive(false);
        //HudManager.Instance?.gameObject?.SetActive(true);
    }

    public static void ForceHide()
    {
        Fill?.SetActive(false);
        Menu?.SetActive(false);
        //HudManager.Instance?.gameObject?.SetActive(true);
    }
}