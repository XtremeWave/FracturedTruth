using System;
using BepInEx.Configuration;
using FinalSuspect.Helpers;

namespace FinalSuspect.Modules.ClientOptions;

public sealed class ClientOptionItem<T> : ClientActionItem
{
    public ConfigEntry<T> Config { get; private set; }

    private ClientOptionItem(
        string name,
        ConfigEntry<T> config,
        OptionsMenuBehaviour optionsMenuBehaviour)
        : base(
            name,
            optionsMenuBehaviour)
    {
        Config = config;
        UpdateToggle();
    }

    /// <summary>
    /// Modオプション画面にconfigのトグルを追加します
    /// </summary>
    /// <param name="name">ボタンラベルの翻訳キーとボタンのオブジェクト名</param>
    /// <param name="config">対応するconfig</param>
    /// <param name="optionsMenuBehaviour">OptionsMenuBehaviourのインスタンス</param>
    /// <param name="additionalOnClickAction">クリック時に追加で発火するアクション．configが変更されたあとに呼ばれる</param>
    /// <returns>作成したアイテム</returns>
    public static ClientOptionItem<T> Create(
        string name,
        ConfigEntry<T> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        Action additionalOnClickAction = null)
    {
        var item = new ClientOptionItem<T>(name, config, optionsMenuBehaviour);
        item.OnClickAction = () =>
        {
            switch (config.Value)
            {
                case bool:
                    config.Value = (T)(object)!(bool)(object)config.Value;
                    break;
                case not null when typeof(T).IsEnum:
                    var allValues = (T[])Enum.GetValues(typeof(T));
                    if (allValues.Length == 0) break;
                    var currentIndex = Array.IndexOf(allValues, config.Value);
                    if (currentIndex < 0) 
                        currentIndex = 0;
                    else
                        currentIndex = (currentIndex + 1) % allValues.Length;
                    config.Value = allValues[currentIndex];
                    item.ToggleButton.Text.text += $"\n|{GetString(config.Value.ToString())}|";
                    break;
            }
            
            
            item.UpdateToggle();
            additionalOnClickAction?.Invoke();
        };
        return item;
    }

    public void UpdateToggle()
    {
        if (!ToggleButton) return;

        var color = ColorHelper.ClientOptionColor_Disable;
        switch (Config.Value)
        {
            case bool value:
                color = value ? ColorHelper.ClientOptionColor : ColorHelper.ClientOptionColor_Disable;
                break;
            case not null when typeof(T).IsEnum:
                var allValues = (T[])Enum.GetValues(typeof(T));
                if (allValues.Length == 0) break;
                var currentIndex = Array.IndexOf(allValues, Config.Value);
                color = ColorHelper.ShadeColor(ColorHelper.ClientOptionColor, currentIndex * 0.025f);
                Config.Value = allValues[currentIndex];
                Rename();
                ToggleButton.Text.text += $"\n|{GetString($"Value.{Config.Value.ToString()}")}|";
                break;
        }
        
        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
    }
}

/*public sealed class ClientOptionItem_String : ClientActionItem
{
    public ConfigEntry<string> Config { get; private set; }
    public string Name { get; private set; }

    private ClientOptionItem_String(
        string name,
        string showingName,
        ConfigEntry<string> config,
        string[] selections,
        OptionsMenuBehaviour optionsMenuBehaviour)
        : base(
            showingName,
            optionsMenuBehaviour)
    {
        Name = name;
        Config = config;
        UpdateToggle(selections);
    }

    /// <summary>
    /// Modオプション画面にconfigのトグルを追加します
    /// </summary>
    /// <param name="name">ボタンラベルの翻訳キーとボタンのオブジェクト名</param>
    /// <param name="showingName"></param>
    /// <param name="config">対応するconfig</param>
    /// <param name="optionsMenuBehaviour">OptionsMenuBehaviourのインスタンス</param>
    /// <param name="selections"></param>
    /// <param name="additionalOnClickAction">クリック時に追加で発火するアクション．configが変更されたあとに呼ばれる</param>
    /// <returns>作成したアイテム</returns>
    public static ClientOptionItem_String Create(
        string name,
        string showingName,
        ConfigEntry<string> config,
        OptionsMenuBehaviour optionsMenuBehaviour,
        string[] selections,
        Action additionalOnClickAction = null)
    {
        var item = new ClientOptionItem_String(name, showingName, config, selections, optionsMenuBehaviour);
        item.OnClickAction = () =>
        {
            var currentIndex = Array.IndexOf(selections, config.Value);

            if (currentIndex == -1)
            {
                Error("wrong index", "ClientOptionItem_String");
                return;
            }

            var nextIndex = (currentIndex + 1) % selections.Length;
            showingName =
                config.Value = selections[nextIndex];
            item.UpdateToggle(selections);
            item.UpdateName(showingName);
            additionalOnClickAction?.Invoke();
        };
        return item;
    }

    public void UpdateToggle(string[] selections)
    {
        if (!ToggleButton) return;

        var color = Config.Value == selections[0] ? Palette.Purple : Color.magenta;
        if (Config.Value == "AprilFoolsMode.HorseMode")
            color = Color.gray;
        ToggleButton.Background.color = color;
        ToggleButton.Rollover?.ChangeOutColor(color);
    }

    public void UpdateName(string name = "")
    {

        if (!ToggleButton) return;
        if (name == "")
        {
            ToggleButton.Text.text = GetString(Config.Value);
            return;
        }
        ToggleButton.Text.text = GetString(name);
        if (Config.Value == "AprilFoolsMode.HorseMode")
            ToggleButton.Text.text += $"({GetString("Broken")})";
    }
}*/