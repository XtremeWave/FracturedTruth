using System;
using FinalSuspect.Helpers;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FinalSuspect.ClientActions.Core;

public class ClientFeatureItem
{
    private static int numItems;

    protected ClientFeatureItem(string name, OptionsMenuBehaviour optionsMenuBehaviour)
    {
        try
        {
            var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

            // 1つ目のボタンの生成時に背景も生成
            if (!CustomBackground)
            {
                numItems = 0;
                CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
                CustomBackground.name = "More Options Background";
                CustomBackground.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
                CustomBackground.transform.localPosition += Vector3.back * 8;
                CustomBackground.gameObject.SetActive(false);

                var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
                closeButton.transform.localPosition = new Vector3(1.3f, -2.3f, -6f);
                closeButton.name = "Close";
                closeButton.Text.text = GetString("Close");
                closeButton.Background.color = Color.red;
                var closePassiveButton = closeButton.GetComponent<PassiveButton>();
                closePassiveButton.OnClick = new Button.ButtonClickedEvent();
                closePassiveButton.OnClick.AddListener(new Action(() =>
                {
                    CustomBackground.gameObject.SetActive(false);
                }));

                UiElement[] selectableButtons = optionsMenuBehaviour.ControllerSelectable.ToArray();
                PassiveButton leaveButton = null;
                PassiveButton returnButton = null;
                foreach (var button in selectableButtons)
                {
                    if (button == null) continue;

                    switch (button.name)
                    {
                        case "LeaveGameButton":
                            leaveButton = button.GetComponent<PassiveButton>();
                            break;
                        case "ReturnToGameButton":
                            returnButton = button.GetComponent<PassiveButton>();
                            break;
                    }
                }

                var generalTab = mouseMoveToggle.transform.parent.parent.parent;

                ModOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
                var pos = leaveButton?.transform.localPosition;
                ModOptionsButton.transform.localPosition =
                    pos != null ? pos.Value - new Vector3(1.3f, 0f, 0f) : new Vector3(-1.3f, -2.4f, 1f);
                ModOptionsButton.name = "FinalSuspectFeatures Options";
                ModOptionsButton.Text.text = GetString("FinalSuspectFeatures");
                ModOptionsButton.Background.color = ColorHelper.ClientFeatureColor;
                var modOptionsPassiveButton = ModOptionsButton.GetComponent<PassiveButton>();
                modOptionsPassiveButton.OnClick = new Button.ButtonClickedEvent();
                modOptionsPassiveButton.OnClick.AddListener(new Action(() =>
                {
                    CustomBackground.gameObject.SetActive(true);
                }));

                if (leaveButton) leaveButton.transform.localPosition = new Vector3(-1.35f, -2.411f, -1f);

                if (returnButton) returnButton.transform.localPosition = new Vector3(1.35f, -2.411f, -1f);
            }

            // ボタン生成
            ToggleButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            ToggleButton.transform.localPosition = new Vector3(
                // 現在のオプション数を基に位置を計算
                numItems % 2 == 0 ? -1.3f : 1.3f,
                // ReSharper disable once PossibleLossOfFraction
                2.2f - 0.5f * (numItems / 2),
                -6f);
            ToggleButton.name = name;
            ToggleButton.Text.text = GetString("ClientFeature." + name);
            ToggleButton.Background.color = ColorHelper.ClientFeatureColor_ClickType;
            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((Action)OnClick);
        }
        finally
        {
            numItems++;
        }
    }

    public ToggleButtonBehaviour ToggleButton { get; set; }
    public Action OnClickAction { get; protected set; }

    public static SpriteRenderer CustomBackground { get; set; }
    public static ToggleButtonBehaviour ModOptionsButton { get; set; }

    /// <summary>
    ///     Modオプション画面に何かアクションを起こすボタンを追加します
    /// </summary>
    /// <param name="name">ボタンラベルの翻訳キーとボタンのオブジェクト名</param>
    /// <param name="onClickAction">クリック時に発火するアクション</param>
    /// <param name="optionsMenuBehaviour">OptionsMenuBehaviourのインスタンス</param>
    /// <returns>作成したアイテム</returns>
    public static ClientFeatureItem Create(
        string name,
        Action onClickAction,
        OptionsMenuBehaviour optionsMenuBehaviour)
    {
        return new ClientFeatureItem(name, optionsMenuBehaviour)
        {
            OnClickAction = onClickAction
        };
    }

    public void OnClick()
    {
        try
        {
            OnClickAction?.Invoke();
        }
        catch (Exception ex)
        {
            Exception(ex, "MoreActions");
        }
    }
}