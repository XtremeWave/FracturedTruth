using System;
using FinalSuspect.Helpers;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FinalSuspect.Modules.ClientOptions;

public class ClientActionItem
{
    public ToggleButtonBehaviour ToggleButton { get; private set; }
    protected Action OnClickAction { get; set; }

    public static SpriteRenderer CustomBackground { get;  set; }
    public static ToggleButtonBehaviour ModOptionsButton { get; set; }
    private static int numItems;

    protected ClientActionItem(string name, OptionsMenuBehaviour optionsMenuBehaviour)
    {
        try
        {
            var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;

            // 在生成第一个按钮时同时生成背景
            if (CustomBackground == null)
            {
                numItems = 0;
                CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
                CustomBackground.name = "Client Options Background";
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
                foreach (var button in selectableButtons)
                {
                    if (button == null)
                    {
                        continue;
                    }

                    switch (button.name)
                    {
                        case "LeaveGameButton":
                            leaveButton = button.GetComponent<PassiveButton>();
                            break;
                        case "ReturnToGameButton":
                            //button.GetComponent<PassiveButton>();
                            break;
                    }
                }
                
                var generalTab = mouseMoveToggle.transform.parent.parent.parent;

                ModOptionsButton = Object.Instantiate(mouseMoveToggle, generalTab);
                var pos = leaveButton?.transform.localPosition;
                ModOptionsButton.transform.localPosition = pos != null ? pos.Value + new Vector3(1.24f, 0f, 0f) : new Vector3(1.24f, -2.4f, 1f);
                ModOptionsButton.name = "FinalSuspect Options";
                ModOptionsButton.Text.text = GetString("FinalSuspectOptions");
                ModOptionsButton.Background.color = ColorHelper.ClientOptionColor;
                var modOptionsPassiveButton = ModOptionsButton.GetComponent<PassiveButton>();
                modOptionsPassiveButton.OnClick = new Button.ButtonClickedEvent();
                modOptionsPassiveButton.OnClick.AddListener(new Action(() =>
                {
                    CustomBackground.gameObject.SetActive(true);
                }));
            }

            // 按钮生成
            ToggleButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            ToggleButton.transform.localPosition = new Vector3(
                // 基于当前选项数量计算位置
                numItems % 2 == 0 ? -1.3f : 1.3f,
                2.2f - 0.5f * (numItems / 2),
                -6f);
            ToggleButton.name = name;
            ToggleButton.Text.text = GetString(name);
            ToggleButton.Background.color = Color.white;
            var passiveButton = ToggleButton.GetComponent<PassiveButton>();
            passiveButton.OnClick = new Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener((Action)OnClick);
        }
        finally
        {
            numItems++;
        }
    }

    /// <summary>
    /// 在 Mod 选项界面中添加一个可执行操作的按钮
    /// </summary>
    /// <param name="name">按钮标签的翻译键和按钮对象名称</param>
    /// <param name="onClickAction">点击时触发的 Action</param>
    /// <param name="optionsMenuBehaviour">OptionsMenuBehaviour 的实例</param>
    /// <returns>创建的项</returns>
    public static ClientActionItem Create(
        string name,
        Action onClickAction,
        OptionsMenuBehaviour optionsMenuBehaviour)
    {
        return new ClientActionItem(name, optionsMenuBehaviour)
        {
            OnClickAction = onClickAction
        };
    }

    private void OnClick()
    {
        OnClickAction?.Invoke();
    }
}