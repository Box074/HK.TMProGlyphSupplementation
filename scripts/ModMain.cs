
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
    internal static FontBase unpackedFont = null!;
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
            var config = globalSettings.GetConfig(cache.Name);
            FontManager.ApplyFontConfig(cache, config);
            caches.Add(cache);
        }

        FontMaker fm = new();
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
            var tex = new Texture2D(1, 1);
            tex.LoadImage(File.ReadAllBytes(v));
            if(tex.width >= 2048 || tex.height >= 2048)
            {
                LogWarn($"You put a packaged font in the \"{DispersivePath}\" folder, which is too bad!");
                continue;
            }
            fm.AddGlyph(id, tex);
        }
        var fc = new FontBase(fm);
        unpackedFont = fc;
        fc.Name = UnpackedInnerName;
        var c = globalSettings.GetConfig(UnpackedInnerName);
        FontManager.ApplyFontConfig(fc, c);
        FontManager.autoRefresh = true;
        FontManager.RefreshFonts();
        caches.Add(fc);
    }
}
