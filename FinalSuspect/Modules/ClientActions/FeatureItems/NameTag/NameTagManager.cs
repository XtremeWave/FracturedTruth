// NameTagManager.cs
using System;
using System.IO;
using System.Text;
using AmongUs.Data;
using FinalSuspect.Helpers;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace FinalSuspect.Modules.ClientActions.FeatureItems.NameTag;

#nullable enable
public static class NameTagManager
{
    public static readonly string TAGS_DIRECTORY_PATH = @"./TONX_Data/NameTags/";
    private static Dictionary<string, NameTag> NameTags = new();
    public static IReadOnlyDictionary<string, NameTag> AllNameTags => NameTags;

    public static IReadOnlyDictionary<string, NameTag> AllInternalNameTags =>
        AllNameTags.Where(t => t.Value.Isinternal).ToDictionary(x => x.Key, x => x.Value);

    public static IReadOnlyDictionary<string, NameTag> AllExternalNameTags =>
        AllNameTags.Where(t => !t.Value.Isinternal).ToDictionary(x => x.Key, x => x.Value);

    public static NameTag DeepClone(NameTag tag)
    {
        return new NameTag
        {
            Title = CloneCom(tag.Title),
            Prefix = CloneCom(tag.Prefix),
            Suffix = CloneCom(tag.Suffix),
            Name = CloneCom(tag.Name),
            DisplayName = CloneCom(tag.DisplayName),
            LastTag = CloneCom(tag.LastTag)
        };

        static Component? CloneCom(Component? com)
        {
            return com == null ? null : new Component
            {
                Text = com.Text,
                SizePercentage = com.SizePercentage,
                TextColor = com.TextColor,
                Gradient = com.Gradient != null ? new ColorGradient(com.Gradient.Colors.ToArray()) : null,
                Spaced = com.Spaced
            };
        }
    }

    public static (string title, string prefix, string suffix, string name, string displayName, string lastTag) 
        ApplyFor(PlayerControl player)
    {
        return AllNameTags.TryGetValue(player.FriendCode, out var tag) 
            ? tag.Apply(player.GetDataName()) 
            : ("", "", "", "", "", "");
    }

    public static void ReloadTag(string? friendCode)
    {
        if (friendCode == null)
        {
            Init();
            return;
        }

        NameTags.Remove(friendCode);
        string path = $"{TAGS_DIRECTORY_PATH}{friendCode}.json";
        if (File.Exists(path))
        {
            try { ReadTagsFromFile(path); }
            catch (Exception ex)
            {
                Error($"Load Tag From: {path} Failed\n{ex}", "NameTagManager", false);
            }
        }
    }

    public static void Init()
    {
        NameTags = new();

        if (!Directory.Exists(TAGS_DIRECTORY_PATH)) 
            Directory.CreateDirectory(TAGS_DIRECTORY_PATH);
        
        foreach (string file in Directory.EnumerateFiles(TAGS_DIRECTORY_PATH, "*.json", SearchOption.AllDirectories))
        {
            try { ReadTagsFromFile(file); }
            catch (Exception ex)
            {
                Error($"Load Tag From: {file} Failed\n{ex}", "NameTagManager", false);
            }
        }
    }

    public static void ReadTagsFromFile(string path)
    {
        if (path.ToLower().Contains("template")) return;
        var text = File.ReadAllText(path);
        var obj = JObject.Parse(text);
        var tag = GetTagFromJObject(obj);
        string friendCode = Path.GetFileNameWithoutExtension(path);
        
        if (tag != null && friendCode != null)
        {
            NameTags[friendCode] = tag;
            Info($"Name Tag Loaded: {friendCode}", "NameTagManager");
        }
    }

    public static NameTag? GetTagFromJObject(JObject obj)
    {
        var tag = new NameTag();

        if (obj.TryGetValue("Title", out var upper))
            tag.Title = GetComponent(upper);

        if (obj.TryGetValue("Prefix", out var prefix))
            tag.Prefix = GetComponent(prefix);

        if (obj.TryGetValue("Suffix", out var suffix))
            tag.Suffix = GetComponent(suffix);

        if (obj.TryGetValue("Name", out var name))
            tag.Name = GetComponent(name, true);

        if (obj.TryGetValue("DisplayName", out var displayName))
            tag.DisplayName = GetComponent(displayName, true);

        if (obj.TryGetValue("LastTag", out var lastTag))
            tag.LastTag = GetComponent(lastTag, true);

        return tag;

        static Component? GetComponent(JToken token, bool force = false)
        {
            if (token == null) return null;
            var com = new Component
            {
                Text = token["Text"]?.ToString(),
                SizePercentage = ParseSize(token["SizePercentage"]?.ToString()),
                TextColor = ParseColor(token["Color"]?.ToString()),
                Gradient = ParseGradient(token["Gradient"]?.ToString()),
                Spaced = token["Spaced"]?.ToString()?.ToLower() == "true"
            };
            return (com.Text != null || force) ? com : null;
        }

        static float? ParseSize(string? str)
        {
            return float.TryParse(str, out var size) ? size : 90f;
        }

        static Color32? ParseColor(string? str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            if (!str.StartsWith("#")) str = "#" + str;
            return ColorUtility.TryParseHtmlString(str, out var color) ? color : null;
        }

        static ColorGradient? ParseGradient(string? str)
        {
            if (string.IsNullOrEmpty(str)) return null;
            var args = str.Split(',', '，');
            if (args.Length < 2) return null;
            
            var colors = new List<Color>();
            foreach (var arg in args)
            {
                var colorStr = arg.StartsWith("#") ? arg : "#" + arg;
                if (ColorUtility.TryParseHtmlString(colorStr, out var color))
                    colors.Add(color);
            }
            
            return colors.Count >= 2 ? new ColorGradient(colors.ToArray()) : null;
        }
    }

    public class NameTag
    {
        public bool Isinternal { get; set; } = false;
        public Component? DisplayName { get; set; }
        public Component? Title { get; set; }
        public Component? Prefix { get; set; }
        public Component? Suffix { get; set; }
        public Component? Name { get; set; }
        public Component? LastTag { get; set; }

        public (string title, string prefix, string suffix, string name, string displayName, string lastTag) 
            Apply(string name, bool preview = false)
        {
            if (!preview)
                return (
                    Title?.Generate(false) ?? "",
                    Prefix?.Generate(false) ?? "",
                    Suffix?.Generate(false) ?? "",
                    Name?.Generate(false) ?? "",
                    DisplayName?.Generate(false) ?? "",
                    LastTag?.Generate(false) ?? ""
                );
            
            if (Name != null) name = Name.Generate(false);
            name = $"{Prefix?.Generate()}{name}{Suffix?.Generate()}";
            var title = $"({DisplayName?.Generate(false)})";
            return ($"{title}\r\n{name}", "", "", "", "", "");
        }
    }

    public class Component
    {
        public float? SizePercentage { get; set; } = 90f;
        public string? Text { get; set; }
        public Color32? TextColor { get; set; }
        public ColorGradient? Gradient { get; set; }
        public bool Spaced { get; set; } = true;

        public string Generate(bool applySpace = true, bool applySize = true)
        {
            if (string.IsNullOrEmpty(Text)) return "";
            
            var result = Text;
            if (Gradient is { IsValid: true }) 
                result = Gradient.Apply(result);
            else if (TextColor != null) 
                result = StringHelper.ColorString(TextColor.Value, result);
            
            if (Spaced && applySpace) 
                result = $" {result} ";
            if (SizePercentage != null && applySize) 
                result = $"<size={SizePercentage}%>{result}</size>";
            
            return result;
        }
    }

    public class ColorGradient
    {
        public List<Color> Colors { get; }
        private float Spacing { get; }

        public ColorGradient(params Color[] colors)
        {
            Colors = new List<Color>(colors);
            Spacing = Colors.Count > 1 ? 1f / (Colors.Count - 1) : 0f;
        }

        public bool IsValid => Colors.Count >= 2;

        public string Apply(string input)
        {
            switch (input.Length)
            {
                case 0:
                    return input;
                case 1:
                    return StringHelper.ColorString(Colors[0], input);
            }

            var step = 1f / (input.Length - 1);
            var sb = new StringBuilder();
            
            for (int i = 0; i < input.Length; i++)
            {
                var color = Evaluate(step * i);
                sb.Append(StringHelper.ColorString(color, input[i].ToString()));
            }

            return sb.ToString();
        }

        public Color Evaluate(float percent)
        {
            percent = Mathf.Clamp01(percent);
            int indexLow = Mathf.FloorToInt(percent / Spacing);
            
            if (indexLow >= Colors.Count - 1) 
                return Colors[^1];
            
            int indexHigh = indexLow + 1;
            float t = (percent - indexLow * Spacing) / Spacing;
            
            return Color.Lerp(Colors[indexLow], Colors[indexHigh], t);
        }
    }
}
#nullable disable