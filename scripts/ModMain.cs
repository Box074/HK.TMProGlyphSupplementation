
using GlobalEnums;
using UnityEngine.UI;

namespace TMProGS;

class TextMeshProGlyphSupplementation : ModBaseWithSettings<TextMeshProGlyphSupplementation, TextMeshProGlyphSupplementation.GS, object>,
    IGlobalSettings<TextMeshProGlyphSupplementation.GS>, ICustomMenuMod
{
    public const string UnpackedInnerName = "Dispersive Font";
    public override string DisplayName => "TMPro Glyph Supplementation";
    //public override string MenuButtonName => DisplayName;
    protected override void AfterCreateModListButton(MenuButton button)
    {
        base.AfterCreateModListButton(button);
        button.GetLabelText()!.text = DisplayName;
    }
    public class GS
    {
        public class FontConfig
        {
            public bool enabled = true;
            public int priority = 0;
            public FontCache.FontMode mode = FontCache.FontMode.Override;
        }
        public string fontsDir = Path.Combine(Path.GetDirectoryName(typeof(TextMeshProGlyphSupplementation).Assembly.Location), "fonts");
        public Dictionary<string, FontConfig> fonts = new();
        public Dictionary<int, string> dpngsCache = new();
        [JsonIgnore]
        public int AtlasSize
        {
            get
            {
                atlasSize = Mathf.Clamp(atlasSize, 0, 3);
                return atlasSize;
            }
            set
            {
                atlasSize = Mathf.Clamp(atlasSize, 0, 3);
            }
        }
        public int atlasSize = 0;
        public FontConfig GetConfig(string innerName)
        {
            if (!fonts.TryGetValue(innerName, out var config))
            {
                config = new();
                fonts[innerName] = config;
            }
            return config;
        }
    }
    protected override List<(SupportedLanguages, string)>? LanguagesEx => new()
    {
        (SupportedLanguages.ZH, "lang.zh"),
        (SupportedLanguages.EN, "lang.en")
    };
    protected override SupportedLanguages DefaultLanguageCode => SupportedLanguages.EN;
    public static event Action? onTryLoadFonts = null;
    public override Font MenuButtonLabelFont => CustomMenu.FontPerpetua;
    bool ICustomMenuMod.ToggleButtonInsideMenu => true;
    MenuScreen ICustomMenuMod.GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        return new MainMenu(modListMenu);
    }
    public static string FontPath
    {
        get
        {
            var path = TextMeshProGlyphSupplementation.Instance.globalSettings.fontsDir;
            Directory.CreateDirectory(path);
            return path;
        }
    }
    public static string CachePath
    {
        get
        {
            var path = Path.Combine(FontPath, "cache");
            Directory.CreateDirectory(path);
            return path;
        }
    }
    public static string DispersivePath
    {
        get
        {
            var path = Path.Combine(FontPath, "dispersive");
            Directory.CreateDirectory(path);
            return path;
        }
    }
    public static string ProcessedPath
    {
        get
        {
            var path = Path.Combine(FontPath, "packed");
            Directory.CreateDirectory(path);
            return path;
        }
    }
    internal static List<FontCache> caches = new();
    internal static FontCollection unpackedFont = new();
    public override void Initialize()
    {
        ModHooks.FinishedLoadingModsHook += () =>
        {
            LoadFonts();
        };
    }
    private void LoadFonts()
    {
        LoadCache();
        onTryLoadFonts?.Invoke();
    }
    enum ChangeMode
    {
        None, Delete, Change
    }
    private void LoadCache()
    {
        FontManager.autoRefresh = false;
        foreach (var v in Directory.GetFiles(ProcessedPath, "*.json", SearchOption.AllDirectories))
        {
            var atlas = Path.ChangeExtension(v, "png");
            if (!File.Exists(atlas))
            {
                LogWarn($"You seem to have forgotten to put the atlas(\"{atlas}\") corresponding to this json to \"{Path.GetDirectoryName(atlas)}\"");
                continue;
            }
            Log($"Load packed font: {v}");
            var cache = new FontPacked(File.ReadAllBytes(v), File.ReadAllBytes(atlas), v);
            caches.Add(cache);
        }

        unpackedFont.onNewAtlas += (self, old, n) =>
        {
            n.Name = UnpackedInnerName;
            FontManager.ApplyFontConfig(n, globalSettings.GetConfig(UnpackedInnerName));
            caches.Add(n);
        };
        Dictionary<int, (byte[] data, string md5)> pngs = new();
        Stopwatch watch = new();
        watch.Start();
        foreach (var v in Directory.GetFiles(DispersivePath, "*.png", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(v);
            int id = 0;
            if (char.IsDigit(name[0]))
            {
                id = int.Parse(name);
            }
            else
            {
                id = (int)name[0];
            }
            var data = File.ReadAllBytes(v);
            pngs[id] = (data, GetMD5(data));
        }
        watch.Stop();
        Log($"Read glyph files: {pngs.Count}({watch.ElapsedMilliseconds}ms)");
        bool loadCache = true;
        List<(int, ChangeMode)> changed = new(pngs.Count / 10);
        foreach (var v in globalSettings.dpngsCache)
        {
            if (!pngs.TryGetValue(v.Key, out var d))
            {
                changed.Add((v.Key, ChangeMode.Delete));
                break;
            }
            if (d.md5 != v.Value)
            {
                changed.Add((v.Key, ChangeMode.Change));
                break;
            }
        }
        if(changed.Count >= (pngs.Count / 2))
        {
            loadCache = false;
        }
        if (loadCache)
        {
            try
            {
                watch.Start();
                unpackedFont.LoadCache(UnpackedInnerName);
                watch.Stop();
                Log($"Load glyph cache: {watch.ElapsedMilliseconds}ms");
                foreach(var v in changed)
                {
                    if(v.Item2 == ChangeMode.Delete)
                    {
                        unpackedFont.RemoveGlyph(v.Item1);
                    }
                }
            }
            catch (IOException)
            {
                globalSettings.dpngsCache.Clear();
            }
        }
        else
        {
            globalSettings.dpngsCache.Clear();
        }
        watch.Start();
        unpackedFont.AutoApply = false;
        foreach(var v in pngs)
        {
            if(globalSettings.dpngsCache.ContainsKey(v.Key) && unpackedFont.HasGlyph(v.Key)) continue;

            var tex = new Texture2D(1, 1);
            tex.LoadImage(v.Value.data);
            if (tex.width >= 2048 || tex.height >= 2048)
            {
                LogWarn($"You put a packaged font in the \"{DispersivePath}\" folder, which is too bad!");
                UnityEngine.Object.Destroy(tex);
                continue;
            }
            unpackedFont.AddGlyph(v.Key, tex);
            UnityEngine.Object.Destroy(tex);
            globalSettings.dpngsCache[v.Key] = v.Value.md5;
        }
        unpackedFont.AutoApply = true;
        watch.Stop();
        Log($"Apply glyphs: {watch.ElapsedMilliseconds}ms");
        unpackedFont.SaveCache(UnpackedInnerName, true);
        SaveGlobalSettings();
        foreach (var v in unpackedFont) caches.Add(v);

        foreach (var v in caches)
        {
            FontManager.ApplyFontConfig(v, globalSettings.GetConfig(v.Name));
        }

        FontManager.autoRefresh = true;
        FontManager.RefreshFonts();
    }
    private static string GetMD5(byte[] data)
    {
        var arrayByteHashValue = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(data);
        return BitConverter.ToString(arrayByteHashValue).Replace("-", String.Empty).ToLower();
    }
}
