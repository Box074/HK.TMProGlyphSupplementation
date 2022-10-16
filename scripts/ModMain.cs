
namespace TMProGS;

class TMProGlyphSupplementation : ModBaseWithSettings<TMProGlyphSupplementation, TMProGlyphSupplementation.GS, object>, IGlobalSettings<TMProGlyphSupplementation.GS>
{
    public override string DisplayName => "TMPro Glyph Supplementation";
    public override string MenuButtonName => DisplayName;
    public class GS
    {
        public string fontsDir = Path.Combine(Path.GetDirectoryName(typeof(TMProGlyphSupplementation).Assembly.Location), "fonts");
    }
    public static event Action? onTryLoadFonts = null;
    public static string FontPath
    {
        get
        {
            var path = TMProGlyphSupplementation.Instance.globalSettings.fontsDir;
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
        foreach(var v in Directory.GetFiles(ProcessedPath, "*.json", SearchOption.AllDirectories))
        {
            var atlas = Path.ChangeExtension(v, "png");
            if(!File.Exists(atlas)) continue;
            Log($"Load packed font: {v}");
            var cache = new FontPacked(File.ReadAllBytes(v), File.ReadAllBytes(atlas));
            cache.Apply();
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
            fm.AddGlyph(id, tex);
        }
        var fc = new FontBase(fm);
        fc.Name = "Dispersive Font";
        fc.Apply();
        FontManager.autoRefresh = true;
        FontManager.RefreshFonts();
        caches.Add(fc);
    }
}
