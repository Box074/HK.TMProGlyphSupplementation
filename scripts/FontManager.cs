
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
    private static void UpdateTMProText(TMP_Text inst)
    {
        int requireCount = fontAssets.Count + 8;

        if (inst is TextMeshPro tmp)
        {
            ref var oarr = ref tmp.private_m_subTextObjects();
            if (oarr.Length < requireCount)
            {
                var arr = new TMP_SubMesh[requireCount + 16];
                Array.Copy(oarr, arr, oarr.Length);
                oarr = arr;
            }
        }
        if (inst is TextMeshProUGUI tmpUI)
        {
            ref var oarr = ref tmpUI.private_m_subTextObjects();
            if (oarr.Length < requireCount)
            {
                var arr = new TMP_SubMeshUI[requireCount + 16];
                Array.Copy(oarr, arr, oarr.Length);
                oarr = arr;
            }
        }
        ref var mr = ref inst.private_m_materialReferences();
        if (mr.Length < requireCount)
        {
            var arr = new MaterialReference[requireCount + 32];
            Array.Copy(mr, arr, mr.Length);
            mr = arr;
        }
    }
    static FontManager()
    {
        On.TMPro.TextMeshPro.Awake += (orig, self) =>
        {
            orig(self);
            UpdateTMProText(self);
        };
        On.TMPro.TextMeshProUGUI.Awake += (orig, self) =>
        {
            orig(self);
            UpdateTMProText(self);
        };
        On.TMPro.TMP_SubMesh.OnEnable += (orig, self) =>
        {
            if (self.GetComponent<TMP_SubMeshCheck>() is null) self.gameObject.AddComponent<TMP_SubMeshCheck>().submesh = self;
            orig(self);
        };
        On.TMPro.TMP_SubMesh.AddSubTextObject += (orig, self, mat) =>
        {
            if(mat.material == null)
            {
                mat.material = mat.fontAsset.material;
            }
            return orig(self, mat);
        };
    }
    class TMP_SubMeshCheck : MonoBehaviour
    {
        public TMP_SubMesh submesh = null!;
        private void Update()
        {
            if (submesh.fontAsset != null && submesh.sharedMaterial == null)
            {
                submesh.sharedMaterial = submesh.fontAsset.material;
            }
        }
    }
    internal static void ApplyFontConfig(FontCache font, TextMeshProGlyphSupplementation.GS.FontConfig config)
    {
        font.Priority = config.priority;
        font.Mode = config.mode;
        if (config.enabled)
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
        if (_fake_root) return;

        foreach (var v in RootFonts)
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
            if (TMP_Settings.fallbackFontAssets is not null)
            {
                TMP_Settings.fallbackFontAssets.Remove(f);
            }
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
            fontAssets.Add(v.Item1);
            foreach (var r in rf)
            {
                r.fallbackFontAssets.Insert(0, v.Item1);
            }
        }
        foreach (var v in fallbacksFonts.OrderByDescending(x => x.Item2))
        {
            fontAssets.Add(v.Item1);
            if (TMP_Settings.fallbackFontAssets == null)
            {
                TMP_Settings.instance.private_m_fallbackFontAssets() = new();
            }
            TMP_Settings.fallbackFontAssets!.Add(v.Item1);
        }

        foreach (var v in UnityEngine.Object.FindObjectsOfType<TMP_Text>(true))
        {
            UpdateTMProText(v);
            v.havePropertiesChanged = true;
            v.SetVerticesDirty();
            v.SetLayoutDirty();
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
            foreach (var v in RootFonts)
            {
                v.fallbackFontAssets.Remove(f);
            }
        }
        fonts.Remove(font);
    }
}
