using System;
using System.IO;
using AmongUs.Data;
using FinalSuspect.Helpers;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using static FinalSuspect.Modules.ClientActions.FeatureItems.NameTag.NameTagManager;
using Component = FinalSuspect.Modules.ClientActions.FeatureItems.NameTag.NameTagManager.Component;
using Object = UnityEngine.Object;

namespace FinalSuspect.Modules.ClientActions.FeatureItems.NameTag;

public static class NameTagEditMenu
{
    public static GameObject Menu { get; private set; }

    public static GameObject EditTitleButton { get; private set; }
    public static GameObject EditPrefixButton { get; private set; }
    public static GameObject EditSuffixButton { get; private set; }
    public static GameObject EditNameButton { get; private set; }
    public static GameObject EditDisplayNameButton { get; private set; }
    public static GameObject EditLastTagButton { get; private set; }

    public static GameObject Preview { get; private set; }

    public static GameObject Text_Info { get; private set; }
    public static GameObject Text_Enter { get; private set; }

    public static GameObject Size_Info { get; private set; }
    public static GameObject Size_Enter { get; private set; }

    public static GameObject Color_Info { get; private set; }
    public static GameObject Color1_Enter { get; private set; }
    public static GameObject Color2_Enter { get; private set; }
    public static GameObject Color3_Enter { get; private set; }

    public static GameObject PreviewButton { get; private set; }
    public static GameObject SaveAndExitButton { get; private set; }
    public static GameObject DeleteButton { get; private set; }

    private static string FriendCode;
    private static NameTagManager.NameTag CacheTag;
    private static ComponentType CurrentComponent;

    private enum ComponentType
    {
        Title,
        Prefix,
        Suffix,
        Name,
        DisplayName,
        LastTag
    }

    public static void Hide()
    {
        if (Menu != null)
            Menu?.SetActive(false);
    }
#nullable enable
    public static void Toggle(string? friendCode, bool? on)
    {
        if (Menu == null) on ??= true;
        else on ??= !Menu.activeSelf;
        if (!IsNotJoined || !on.Value)
        {
            if (Menu != null) Menu?.SetActive(false);
            return;
        }

        if (Menu == null) Init();
        if (Menu == null) return;
        Menu.SetActive(on.Value);
        FriendCode = friendCode;
        CacheTag = (friendCode != null && AllExternalNameTags.TryGetValue(friendCode, out var tag))
            ? DeepClone(tag)
            : new NameTagManager.NameTag();
        if (!Menu.activeSelf) return;
        LoadComponent(CacheTag?.DisplayName, ComponentType.DisplayName);
        SetButtonHighlight(EditDisplayNameButton);
        CurrentComponent = ComponentType.DisplayName;
        UpdatePreview();
    }

    private static void SetButtonHighlight(GameObject obj)
    {
        EditTitleButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditPrefixButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditSuffixButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditNameButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditDisplayNameButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        EditLastTagButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = Palette.DisabledGrey;
        obj.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>().color = new Color32(0, 164, 255, 255);
    }

    private static void LoadComponent(Component? com, ComponentType type, bool name = false)
    {
        Text_Enter.GetComponent<TextBoxTMP>().enabled = !name;
        Text_Enter.GetComponent<TextBoxTMP>().SetText(!name ? (com?.Text ?? "") : GetString("CanNotEdit"));
        Size_Enter.GetComponent<TextBoxTMP>().SetText((com?.SizePercentage ?? 100).ToString());
        Color1_Enter.GetComponent<TextBoxTMP>().Clear();
        Color2_Enter.GetComponent<TextBoxTMP>().Clear();
        Color3_Enter.GetComponent<TextBoxTMP>().Clear();
        if (com?.Gradient?.IsValid ?? false)
        {
            int colorNum = 1;
            foreach (var color in com.Gradient.Colors)
            {
                (colorNum switch
                        {
                            1 => Color1_Enter.transform,
                            2 => Color2_Enter.transform,
                            3 => Color3_Enter.transform,
                        }
                    ).GetComponent<TextBoxTMP>().SetText(ColorUtility.ToHtmlStringRGBA(color)[..6]);
                colorNum++;
            }
        }
        else if (com?.TextColor != null)
        {
            Color1_Enter.GetComponent<TextBoxTMP>().SetText(ColorUtility.ToHtmlStringRGBA(com.TextColor.Value)[..6]);
        }
    }
#nullable disable
    private static void UpdatePreview()
    {
        if (!Menu.active || CacheTag == null || Preview == null) return;
        // 只显示DisplayName组件
        var displayName = CacheTag.DisplayName?.Generate() ?? "";
        Preview.GetComponent<TextMeshPro>().text = displayName;
    }

    private static void SaveToCache(ComponentType type)
    {
        var com = new Component();

        string text = Text_Enter.GetComponent<TextBoxTMP>().text.Trim();
        if (text != "" && type != ComponentType.Name) com.Text = text;
        string size = Size_Enter.GetComponent<TextBoxTMP>().text.Trim();
        if (size != "" && float.TryParse(size, out var sizef)) com.SizePercentage = sizef;
        string color1 = Color1_Enter.GetComponent<TextBoxTMP>().text.Trim();
        string color2 = Color2_Enter.GetComponent<TextBoxTMP>().text.Trim();
        string color3 = Color3_Enter.GetComponent<TextBoxTMP>().text.Trim();
        List<Color> colors = new();
        if (color1 != "" && ColorUtility.DoTryParseHtmlColor("#" + color1, out var c1)) colors.Add(c1);
        if (color2 != "" && ColorUtility.DoTryParseHtmlColor("#" + color2, out var c2)) colors.Add(c2);
        if (color3 != "" && ColorUtility.DoTryParseHtmlColor("#" + color3, out var c3)) colors.Add(c3);
        if (colors.Count > 1) com.Gradient = new(colors.ToArray());
        else if (colors.Count == 1) com.TextColor = colors[0];
        com.Spaced = default;

        switch (type)
        {
            case ComponentType.Title:
                CacheTag.Title = com;
                break;
            case ComponentType.Prefix:
                CacheTag.Prefix = com;
                break;
            case ComponentType.Suffix:
                CacheTag.Suffix = com;
                break;
            case ComponentType.Name:
                CacheTag.Name = com;
                break;
            case ComponentType.DisplayName:
                CacheTag.DisplayName = com;
                break;
            case ComponentType.LastTag:
                CacheTag.LastTag = com;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }

        ;
    }
#nullable enable
    private enum ComponentName
    {
        Title,
        Prefix,
        Suffix,
        Name,
        DisplayName,
        LastTag
    }

    private static bool SaveToFile(string friendCode, NameTagManager.NameTag tag)
    {
        if (FriendCode is null or "") return false;

        Il2CppSystem.IO.StringWriter sw = new();
        JsonWriter JsonWriter = new JsonTextWriter(sw);
        JsonWriter.WriteStartObject();

        foreach (ComponentName comName in Enum.GetValues(typeof(ComponentName)))
        {
            var com = comName switch
            {
                ComponentName.Title => tag.Title,
                ComponentName.Prefix => tag.Prefix,
                ComponentName.Suffix => tag.Suffix,
                ComponentName.Name => tag.Name,
                ComponentName.DisplayName => tag.DisplayName,
                ComponentName.LastTag => tag.LastTag,
                _ => null
            };

            if (com == null) continue;

            JsonWriter.WritePropertyName(Enum.GetName(typeof(ComponentName), comName));
            JsonWriter.WriteStartObject();

            if (com.Text != null && comName != ComponentName.Name)
            {
                JsonWriter.WritePropertyName("Text");
                JsonWriter.WriteValue(com.Text);
            }

            if (com.SizePercentage != null)
            {
                JsonWriter.WritePropertyName("SizePercentage");
                JsonWriter.WriteValue(com.SizePercentage.ToString());
            }

            if (com.Gradient != null && com.Gradient.IsValid)
            {
                string colors = "";
                com.Gradient.Colors.Do(c => colors += "#" + ColorUtility.ToHtmlStringRGBA(c)[..6] + ",");
                JsonWriter.WritePropertyName("Gradient");
                JsonWriter.WriteValue(colors.TrimEnd(','));
            }
            else if (com.TextColor != null)
            {
                JsonWriter.WritePropertyName("Color");
                JsonWriter.WriteValue("#" + ColorUtility.ToHtmlStringRGBA(com.TextColor.Value)[..6]);
            }

            if (comName is not ComponentName.Title and not ComponentName.Name)
            {
                JsonWriter.WritePropertyName("Spaced");
                JsonWriter.WriteValue(com.Spaced.ToString());
            }

            JsonWriter.WriteEndObject();
        }

        JsonWriter.WriteEndObject();
        sw.Flush();

        string fileName = TAGS_DIRECTORY_PATH + friendCode.Trim() + ".json";
        if (!File.Exists(fileName)) File.Create(fileName).Close();
        File.WriteAllText(fileName, sw.ToString());
        return true;
    }
#nullable disable
    public static void Init()
    {
        Menu = Object.Instantiate(AccountManager.Instance.transform.FindChild("InfoTextBox").gameObject,
            NameTagPanel.CustomBackground.transform.parent);
        Menu.name = "Name Tag Edit Menu";
        Menu.transform.SetLocalZ(-30f);
        Menu.transform.FindChild("Background").localScale *= 1.4f;

        // 应用屏幕比例调整
        float offset = GetResolutionOffset();
        Menu.transform.localScale = new Vector3(offset, offset, 1f);

        Object.Destroy(Menu.transform.FindChild("Button2").gameObject);

        var closeButton = Object.Instantiate(Menu.transform.parent.FindChild("CloseButton"), Menu.transform);
        closeButton.transform.localPosition = new Vector3(4.9f * offset, 2.5f * offset, -1f);
        closeButton.transform.localScale = new Vector3(1f, 1f, 1f);
        closeButton.GetComponent<PassiveButton>().OnClick = new();
        closeButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() => { Toggle(null, false); }));

        var titlePrefab = Menu.transform.FindChild("TitleText_TMP").gameObject;
        titlePrefab.name = "Title Prefab";
        var infoPrefab = Menu.transform.FindChild("InfoText_TMP").gameObject;
        infoPrefab.name = "Info Prefab";
        var buttonPrefab = Menu.transform.FindChild("Button1").gameObject;
        buttonPrefab.name = "Button Prefab";
        buttonPrefab.GetComponent<PassiveButton>().OnClick = new();
        var enterPrefab =
            Object.Instantiate(
                AccountManager.Instance.transform.FindChild("PremissionRequestWindow/GuardianEmailConfirm").gameObject,
                Menu.transform);
        enterPrefab.name = "Enter Box Prefab";
        enterPrefab.transform.localScale = new Vector3(0.6f * offset, 0.6f * offset, 0.6f * offset);
        enterPrefab.GetComponent<TextBoxTMP>().AllowPaste = true;
        Object.Destroy(enterPrefab.GetComponent<EmailTextBehaviour>());

        int editButtonNum = 0;
        float buttonSpacing = 1.2f * offset;

        // 创建所有组件编辑按钮
        EditTitleButton = CreateComponentButton("Title", buttonPrefab, -3.6f + buttonSpacing * editButtonNum++, offset);
        EditPrefixButton = CreateComponentButton("Prefix", buttonPrefab, -3.6f + buttonSpacing * editButtonNum++, offset);
        EditSuffixButton = CreateComponentButton("Suffix", buttonPrefab, -3.6f + buttonSpacing * editButtonNum++, offset);
        EditNameButton = CreateComponentButton("Name", buttonPrefab, -3.6f + buttonSpacing * editButtonNum++, offset);
        EditDisplayNameButton = CreateComponentButton("DisplayName", buttonPrefab, -3.6f + buttonSpacing * editButtonNum++, offset);
        EditLastTagButton = CreateComponentButton("LastTag", buttonPrefab, -3.6f + buttonSpacing * editButtonNum++, offset);

        PreviewButton = Object.Instantiate(buttonPrefab, Menu.transform);
        PreviewButton.name = "Refresh Preview Button";
        PreviewButton.transform.localPosition = new Vector3(1.2f * offset, -2.5f * offset, 0f);
        PreviewButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            UpdatePreview();
        }));
        var previewButtonTmp = PreviewButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        previewButtonTmp.text = GetString("RefreshPreview");

        SaveAndExitButton = Object.Instantiate(buttonPrefab, Menu.transform);
        SaveAndExitButton.name = "Save And Exit Button";
        SaveAndExitButton.transform.localPosition = new Vector3(3.5f * offset, -2.5f * offset, 0f);
        SaveAndExitButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            SaveToFile(FriendCode, CacheTag);
            ReloadTag(FriendCode);
            NameTagPanel.RefreshTagList();
            Toggle(null, false);
        }));
        var saveButtonTmp = SaveAndExitButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        saveButtonTmp.text = GetString("SaveAndClose");

        DeleteButton = Object.Instantiate(buttonPrefab, Menu.transform);
        DeleteButton.name = "Delete Name Tag Button";
        DeleteButton.transform.localPosition = new Vector3(-3.5f * offset, -2.5f * offset, 0f);
        DeleteButton.GetComponent<PassiveButton>().OnClick.AddListener((Action)(() =>
        {
            string fileName = TAGS_DIRECTORY_PATH + FriendCode.Trim() + ".json";
            if (File.Exists(fileName)) File.Delete(fileName);
            ReloadTag(FriendCode);
            NameTagPanel.RefreshTagList();
            Toggle(null, false);
        }));
        var deleteButtonTmp = DeleteButton.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        deleteButtonTmp.color = Color.red;
        deleteButtonTmp.text = GetString("Delete");

        Preview = Object.Instantiate(titlePrefab, Menu.transform);
        Preview.name = "Preview Text";
        Preview.transform.localPosition = new Vector3(0f * offset, 1.5f * offset, 0f);
        var previewTmp = Preview.GetComponent<TextMeshPro>();
        previewTmp.text = DataManager.player.Customization.Name;
        previewTmp.fontSize = 0.6f;

        Text_Info = Object.Instantiate(infoPrefab, Menu.transform);
        Text_Info.name = "Edit Text Description";
        Text_Info.transform.localPosition = new Vector3(-2.95f * offset, 0f, 0f);
        var textInfoTmp = Text_Info.GetComponent<TextMeshPro>();
        textInfoTmp.text = GetString("TextContent");

        Text_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Text_Enter.name = "Edit Text Enter Box";
        Text_Enter.transform.localPosition = new Vector3(-2.9f * offset, -0.5f * offset, 0f);
        var textEnterTBT = Text_Enter.GetComponent<TextBoxTMP>();
        textEnterTBT.allowAllCharacters =
            textEnterTBT.AllowEmail =
                textEnterTBT.AllowSymbols = true;

        Size_Info = Object.Instantiate(infoPrefab, Menu.transform);
        Size_Info.name = "Edit Size Description";
        Size_Info.transform.localPosition = new Vector3(-2.95f * offset, -1.5f * offset, 0f);
        var sizeInfoTmp = Size_Info.GetComponent<TextMeshPro>();
        sizeInfoTmp.text = GetString("TextContentDescription");

        Size_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Size_Enter.name = "Edit Size Enter Box";
        Size_Enter.transform.localPosition = new Vector3(-2.9f * offset, -2.0f * offset, 0f);
        var sizeEnterTBT = Size_Enter.GetComponent<TextBoxTMP>();
        sizeEnterTBT.allowAllCharacters =
            sizeEnterTBT.AllowEmail =
                sizeEnterTBT.AllowSymbols =
                    sizeEnterTBT.AllowPaste = false;

        Color_Info = Object.Instantiate(infoPrefab, Menu.transform);
        Color_Info.name = "Edit Color Description";
        Color_Info.transform.localPosition = new Vector3(1.95f * offset, 0f, 0f);
        var colorInfoTmp = Color_Info.GetComponent<TextMeshPro>();
        colorInfoTmp.text = GetString("TextColorDescription");

        Color1_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Color1_Enter.name = "Edit Color 1 Enter Box";
        Color1_Enter.transform.localPosition = new Vector3(1.95f * offset, -0.5f * offset, 0f);

        Color2_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Color2_Enter.name = "Edit Color 2 Enter Box";
        Color2_Enter.transform.localPosition = new Vector3(1.95f * offset, -1.0f * offset, 0f);

        Color3_Enter = Object.Instantiate(enterPrefab, Menu.transform);
        Color3_Enter.name = "Edit Color 3 Enter Box";
        Color3_Enter.transform.localPosition = new Vector3(1.95f * offset, -1.5f * offset, 0f);

        titlePrefab.SetActive(false);
        infoPrefab.SetActive(false);
        buttonPrefab.SetActive(false);
        enterPrefab.SetActive(false);
    }

    private static GameObject CreateComponentButton(string componentName, GameObject prefab, float xPos, float offset)
    {
        var button = Object.Instantiate(prefab, Menu.transform);
        button.name = $"Edit {componentName} Button";
        button.transform.localPosition = new Vector3(xPos, 2f * offset, 0f);
        var passiveButton = button.GetComponent<PassiveButton>();
        passiveButton.OnClick = new();
        passiveButton.OnClick.AddListener((Action)(() =>
        {
            SaveToCache(CurrentComponent);
            var type = (ComponentType)Enum.Parse(typeof(ComponentType), componentName);
            LoadComponent(GetComponent(CacheTag, type), type, type == ComponentType.Name);
            SetButtonHighlight(button);
            CurrentComponent = type;
        }));
        var buttonTmp = button.transform.FindChild("Text_TMP").GetComponent<TextMeshPro>();
        buttonTmp.text = GetString(componentName);
        return button;
    }

    private static Component GetComponent(NameTagManager.NameTag tag, ComponentType type)
    {
        return type switch
        {
            ComponentType.Title => tag.Title,
            ComponentType.Prefix => tag.Prefix,
            ComponentType.Suffix => tag.Suffix,
            ComponentType.Name => tag.Name,
            ComponentType.DisplayName => tag.DisplayName,
            ComponentType.LastTag => tag.LastTag,
            _ => null
        };
    }
}