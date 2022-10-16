
namespace TMProGS;

public static class FontManager
{
    public static readonly string[] RootFontAssetName = new[]{
        "TrajanPro-Bold SDF",
        "perpetua_tmpro",
        "Perpetua-SDF",
        "trajan_bold_tmpro"
    };
    public static readonly Dictionary<LanguageCode, string[]> LanguageSpecialFontAssetName = new()
    {
        [LanguageCode.ZH] = new[] { "chinese_body", "chinese_body_bold" },
        [LanguageCode.RU] = new[] { "russian_body", "russian_body_fallback", "russian_title", "russian_title_fallback" },
        [LanguageCode.KO] = new[] { "korean_body" },
        [LanguageCode.JA] = new[] { "japanese_body", "japanese_title", "japanese_body_bold" },
        //[LanguageCode.TR] = new[] { "trajan_bold_tmpro" }
    };
    static FontManager()
    {
        On.TMPro.TextMeshPro.Awake += (orig, self) =>
        {
            orig(self);
            int requireCount = fontAssets.Count + 2;
            ref var oarr = ref self.private_m_subTextObjects();
            if (oarr.Length < requireCount)
            {
                var arr = new TMP_SubMesh[requireCount + 16];
                Array.Copy(oarr, arr, oarr.Length);
                oarr = arr;
            }
        };
        On.TMPro.TextMeshProUGUI.Awake += (orig, self) =>
        {
            orig(self);
            int requireCount = fontAssets.Count + 2;
            ref var oarr = ref self.private_m_subTextObjects();
            if (oarr.Length < requireCount)
            {
                var arr = new TMP_SubMeshUI[requireCount + 16];
                Array.Copy(oarr, arr, oarr.Length);
                oarr = arr;
            }
        };
    }
    internal static void ApplyFontConfig(FontCache font, TMProGlyphSupplementation.GS.FontConfig config)
    {
        font.Priority = config.priority;
        font.Mode = config.mode;
        if(config.enabled)
        {
            font.Apply();
        }
        else
        {
            font.Undo();
        }
    }
    private static List<TMP_FontAsset> rootFonts = null!;
    private static Dictionary<string, TMP_FontAsset> fontCache = new();
    public static TMP_FontAsset? GetFontAsset(string name)
    {
        if (fontCache.TryGetValue(name, out var asset)) return asset;
        asset = Resources.FindObjectsOfTypeAll<TMP_FontAsset>().First(x => x.name.Equals("chinese_body", StringComparison.OrdinalIgnoreCase));
        if (asset is not null) fontCache.Add(name, asset);
        return asset;
    }
    public static IReadOnlyList<TMP_FontAsset> RootFonts
    {
        get
        {
            if (rootFonts is null)
            {
                rootFonts = new();
                FindRootFont();
            }
            return rootFonts.AsReadOnly();
        }
    }
    private static void FindRootFont()
    {
        foreach (var v in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
        {
            if (RootFontAssetName.Contains(v.name))
            {
                rootFonts.Add(v);
            }
        }
    }
    private static bool _fake_root = false;
    private static void CreateFakeRoot()
    {
        if(_fake_root) return;
        
        foreach(var v in RootFonts)
        {
            var of = v.fallbackFontAssets;
            v.fallbackFontAssets = new();

            var fake = UnityEngine.Object.Instantiate(v);

            v.fallbackFontAssets = of;
            v.fallbackFontAssets.Insert(0, fake);
            v.private_m_glyphInfoList() = new();
            v.ReadFontDefinition();
        }
        _fake_root = true;
    }
    public static void RefreshFonts()
    {
        CreateFakeRoot();
        foreach (var f in fontAssets)
        {
            foreach (var v in RootFonts)
            {
                v.fallbackFontAssets.Remove(f);
            }
        }
        fontAssets.Clear();
        List<(TMP_FontAsset, int)> overrideFonts = new();
        List<(TMP_FontAsset, int)> fallbacksFonts = new();
        foreach ((var fc, var asset) in fonts)
        {
            if (fc.Mode == FontCache.FontMode.Override)
            {
                overrideFonts.Add((asset, fc.Priority));
            }
            else if (fc.Mode == FontCache.FontMode.AsFallback)
            {
                fallbacksFonts.Add((asset, fc.Priority));
            }
        }
        var rf = RootFonts;
        foreach (var v in overrideFonts.OrderBy(x => x.Item2))
        {
            foreach (var r in rf)
            {
                fontAssets.Add(v.Item1);
                r.fallbackFontAssets.Insert(0, v.Item1);
            }
        }
        foreach (var v in overrideFonts.OrderByDescending(x => x.Item2))
        {
            foreach (var r in rf)
            {
                fontAssets.Add(v.Item1);
                r.fallbackFontAssets.Append(v.Item1);
            }
        }
        int requireCount = fontAssets.Count + 2;
        foreach (var v in UnityEngine.Object.FindObjectsOfType<TMP_Text>())
        {
            if (v is TextMeshPro tmp)
            {
                ref var oarr = ref tmp.private_m_subTextObjects();
                if (oarr.Length < requireCount)
                {
                    var arr = new TMP_SubMesh[requireCount + 16];
                    Array.Copy(oarr, arr, oarr.Length);
                    oarr = arr;
                }
            }
            if (v is TextMeshProUGUI tmpUI)
            {
                ref var oarr = ref tmpUI.private_m_subTextObjects();
                if (oarr.Length < requireCount)
                {
                    var arr = new TMP_SubMeshUI[requireCount + 16];
                    Array.Copy(oarr, arr, oarr.Length);
                    oarr = arr;
                }
            }
        }
    }
    private static Dictionary<FontCache, TMP_FontAsset> fonts = new();
    private static List<TMP_FontAsset> fontAssets = new();
    internal static bool autoRefresh = true;
    public static void AddOverrideFont(FontCache font, TMP_FontAsset fontAsset)
    {
        fonts[font] = fontAsset;
        if (autoRefresh) RefreshFonts();
    }
    public static void RemoveOverrideFont(FontCache font)
    {
        if (fonts.TryGetValue(font, out var f))
        {
            foreach (var v in RootFonts)
            {
                v.fallbackFontAssets.Remove(f);
            }
        }
        fonts.Remove(font);
    }
}
