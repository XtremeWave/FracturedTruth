using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FinalSuspect.Modules.Features;
using FinalSuspect.Modules.Resources;
using FinalSuspect.Modules.SoundInterface;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace FinalSuspect.Modules.Panels;

public static class SoundManagementPanel
{
    public static SpriteRenderer CustomBackground { get; set; }
    public static GameObject Slider { get; private set; }
    public static Dictionary<string, GameObject> Items { get; private set; }

    private static int numItems;
    
    public static void Hide()
    {
        if (CustomBackground != null)
            CustomBackground?.gameObject.SetActive(false);
    }
    
    public static void Init(OptionsMenuBehaviour optionsMenuBehaviour)
    {
        var mouseMoveToggle = optionsMenuBehaviour.DisableMouseMovement;
        if (!IsNotJoined) return;

        if (CustomBackground == null)
        {
            numItems = 0;
            CustomBackground = Object.Instantiate(optionsMenuBehaviour.Background, optionsMenuBehaviour.transform);
            CustomBackground.name = "Audio Management Panel Background";
            CustomBackground.transform.localScale = new Vector3(0.9f, 0.9f, 1f);
            CustomBackground.transform.localPosition += Vector3.back * 18;
            CustomBackground.gameObject.SetActive(false);

            var closeButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            closeButton.transform.localPosition = new Vector3(1.3f, -2.43f, -16f);
            closeButton.name = "Close";
            closeButton.Text.text = GetString("Close");
            closeButton.Background.color = Color.red;
            var closePassiveButton = closeButton.GetComponent<PassiveButton>();
            closePassiveButton.OnClick = new Button.ButtonClickedEvent();
            closePassiveButton.OnClick.AddListener(new Action(() =>
            {
                CustomBackground.gameObject.SetActive(false);
            }));

            var newButton = Object.Instantiate(mouseMoveToggle, CustomBackground.transform);
            newButton.transform.localPosition = new Vector3(1.3f, -1.88f, -16f);
            newButton.name = "New Audio";
            newButton.Text.text = GetString("NewSound");
            newButton.Background.color = Palette.White;
            var newPassiveButton = newButton.GetComponent<PassiveButton>();
            newPassiveButton.OnClick = new Button.ButtonClickedEvent();
            newPassiveButton.OnClick.AddListener(new Action(SoundManagementNewWindow.Open));

            var helpText = Object.Instantiate(CustomPopup.InfoTMP?.gameObject, CustomBackground.transform);
            helpText.name = "Help Text";
            helpText.transform.localPosition = new Vector3(-1.25f, -2.15f, -15f);
            helpText.transform.localScale = new Vector3(1f, 1f, 1f);
            var helpTextTMP = helpText.GetComponent<TextMeshPro>();
            helpTextTMP.text = GetString("CustomAudioManagementHelp");
            helpText.gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(2.45f, 1f);

            var sliderTemplate = AccountManager.Instance.transform.FindChild("MainSignInWindow/SignIn/AccountsMenu/Accounts/Slider").gameObject;
            if (sliderTemplate != null && Slider == null)
            {
                Slider = Object.Instantiate(sliderTemplate, CustomBackground.transform);
                Slider.name = "Audio Management Slider";
                Slider.transform.localPosition = new Vector3(0f, 0.5f, -11f);
                Slider.transform.localScale = new Vector3(1f, 1f, 1f);
                Slider.GetComponent<SpriteRenderer>().size = new Vector2(5f, 4f);
                var scroller = Slider.GetComponent<Scroller>();
                scroller.ScrollWheelSpeed = 0.3f;
                var mask = Slider.transform.FindChild("Mask");
                mask.transform.localScale = new Vector3(4.9f, 3.92f, 1f);
            }
        }
        RefreshTagList();
    }
  
    public static void RefreshTagList()
    {
        if (!IsNotJoined) return;
        numItems = 0;
        var scroller = Slider.GetComponent<Scroller>();
        scroller.Inner.gameObject.ForEachChild((Action<GameObject>)DestroyObj);

        var numberSetter = AccountManager.Instance.transform.FindChild("DOBEnterScreen/EnterAgePage/MonthMenu/Months").GetComponent<NumberSetter>();
        var buttonPrefab = numberSetter.ButtonPrefab.gameObject;

        Items?.Values.Do(Object.Destroy);
        Items = new Dictionary<string, GameObject>();
        foreach (var audio in XtremeMusic.musics)
        {
            numItems++;
            var filename = audio.FileName;

            var button = Object.Instantiate(buttonPrefab, scroller.Inner);
            button.transform.localPosition = new Vector3(-1f, 1.6f - 0.6f * numItems, -10.5f);
            button.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            button.name = "Btn-" + filename;

            var renderer = button.GetComponent<SpriteRenderer>();
            var rollover = button.GetComponent<ButtonRolloverHandler>();

            var previewText = Object.Instantiate(button.transform.GetChild(0).GetComponent<TextMeshPro>(), button.transform);
            previewText.transform.SetLocalX(1.9f);
            previewText.fontSize = 1f;
            previewText.name = "PreText-" + filename;

            Object.Destroy(button.GetComponent<UIScrollbarHelper>());
            Object.Destroy(button.GetComponent<NumberButton>());

            string buttontext;
            Color buttonColor;
            var enable = true;

            var audioExist = audio.CurrectAudioStates is not AudiosStates.NotExist || SoundInterface.SoundManager.CustomAudios.Contains(filename);
            var unpublished = audio.unpublished;
            
            switch (audio.CurrectAudioStates)
            {
                case AudiosStates.IsDownLoading:
                    buttontext = GetString("DownloadingAudios");
                    buttonColor = Color.yellow;
                    enable = false;
                    break;
                case AudiosStates.DownLoadSucceedNotice or AudiosStates.DownLoadFailureNotice:
                {
                    var succeed = audio.CurrectAudioStates is AudiosStates.DownLoadSucceedNotice;
                    buttontext = GetString($"{audio.CurrectAudioStates}");
                    buttonColor = succeed ? Color.cyan : Palette.Brown;
                    enable = false;
                    break;
                }
                case AudiosStates.IsPlaying:
                    buttontext = GetString("Playing");
                    buttonColor = Color.red;
                    enable = false;
                    break;
                default:
                {
                    if (audioExist)
                    {
                        buttontext = GetString("delete");
                        buttonColor = !audio.UnOfficial ? Color.red : Palette.Purple;
                    }
                    else
                    {
                        buttontext = !audio.UnOfficial ? GetString("download") : GetString("NoFound");
                        buttonColor = !audio.UnOfficial ? Color.green : Color.black;
                    }
                    break;
                }
            }
            
            if (unpublished)
            {
                buttonColor = Palette.DisabledGrey;
                enable = false;
            }

            var preview = audio.Name;

            var passiveButton = button.GetComponent<PassiveButton>();
            passiveButton.OnClick = new Button.ButtonClickedEvent();
            passiveButton.OnClick.AddListener(new Action(() =>
            {
                if (audioExist)
                {
                    Delete(audio);
                }
                else
                {
                    audio.CurrectAudioStates = audio.LastAudioStates = AudiosStates.IsDownLoading;
                    RefreshTagList();
                    var task = ResourcesDownloader.StartDownload(FileType.Sounds, filename + ".wav");
                    task.ContinueWith(t => 
                    {
                        _ = new MainThreadTask(() =>
                        {
                            audio.CurrectAudioStates = audio.LastAudioStates = t.Result ? AudiosStates.DownLoadSucceedNotice : AudiosStates.DownLoadFailureNotice;
                            RefreshTagList();

                            _ = new LateTask(() =>
                            {
                                XtremeMusic.CreateMusic(music: audio.CurrectAudio);
                                RefreshTagList();
                                MyMusicPanel.RefreshTagList();
                            },3f, "Refresh Tag List");
                        }, "Download Notice");
                    });
                }
            }));

            button.transform.GetChild(0).GetComponent<TextMeshPro>().text = buttontext;
            rollover.OutColor = renderer.color = buttonColor;
            button.GetComponent<PassiveButton>().enabled = enable;
            previewText.text = preview;
            Items.Add(filename, button);
        }
        
        scroller.SetYBoundsMin(0f);
        scroller.SetYBoundsMax(0.6f * numItems);
        return;

        static void DestroyObj(GameObject obj)
        {
            if (obj.name.StartsWith("AccountButton")) Object.Destroy(obj);
        }
    }

    private static void Delete(XtremeMusic audio)
    {
        var sound = audio.FileName;
        if (audio.UnOfficial)
            DeleteSoundInName(sound);
        DeleteSoundInFile(sound);
        if (!audio.UnOfficial) XtremeMusic.CreateMusic(music: audio.CurrectAudio);
        RefreshTagList();
        MyMusicPanel.RefreshTagList();
    }

    private static void DeleteSoundInName(string name)
    {
        using StreamReader sr = new(SoundInterface.SoundManager.TAGS_PATH);

        List<string> update = [];
        while (sr.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            if (line != name)
            {
                update.Add(line);
            }
        }
        
        sr.Dispose();

        File.Delete(SoundInterface.SoundManager.TAGS_PATH);
        File.Create(SoundInterface.SoundManager.TAGS_PATH).Close();

        var attributes = File.GetAttributes(SoundInterface.SoundManager.TAGS_PATH);
        File.SetAttributes(SoundInterface.SoundManager.TAGS_PATH, attributes | FileAttributes.Hidden);

        using StreamWriter sw = new(SoundInterface.SoundManager.TAGS_PATH, true);

        foreach (var updateline in update)
        {
            sw.WriteLine(updateline);
        }
        
        var item = XtremeMusic.musics.FirstOrDefault(x => x.Name == name);
        XtremeMusic.musics.Remove(item);
    }

    private static void DeleteSoundInFile(string sound)
    {
        var path = GetResourceFilesPath(FileType.Sounds, sound + ".wav");
        File.Delete(path);
    }
}