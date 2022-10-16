
namespace TMProGS.Menu;

class FontControl : CustomMenu
{
    public (string displayName, string innerName, List<FontCache> fonts) fonts;
    public TMProGlyphSupplementation.GS.FontConfig config = new();
    public override Font titleFont => FontPerpetua;
    public enum FontPriority
    {
        Highest = 64,
        High = 32,
        Normal = 0,
        Low = -32,
        Lowest = -64
    }
    public FontControl(MenuScreen rs) : base(rs, "Font")
    {

    }
    protected override void Build(ContentArea contentArea)
    {
        AddBoolOption("HK.TMPro.Menu.Enabled".Localize(), "",
            val => config.enabled = val, () => config.enabled, FontPerpetua);
        AddOption("HK.TMPro.Menu.Mode".Localize(), "",
            new[] { "HK.TMPro.Mode.Override".Localize(), "HK.TMPro.Mode.Fallback".Localize() }, val => config.mode = val switch
            {
                0 => FontCache.FontMode.Override,
                _ => FontCache.FontMode.AsFallback
            },
            () => config.mode switch
            {
                FontCache.FontMode.Override => 0,
                _ => 1
            }, FontPerpetua);
        AddOption("HK.TMPro.Menu.Priority".Localize(), "",
            new[]{"HK.TMPro.P.Highest".Localize(),
            "HK.TMPro.P.High".Localize(),
            "HK.TMPro.P.Normal".Localize(),
            "HK.TMPro.P.Low".Localize(),
            "HK.TMPro.P.Lowest".Localize()},
            val =>
            {
                config.priority = (int)(val switch
                {
                    0 => FontPriority.Highest,
                    1 => FontPriority.High,
                    3 => FontPriority.Low,
                    4 => FontPriority.Lowest,
                    _ => FontPriority.Normal
                });
            },
            () => (config.priority switch
            {
                > 32 => 0,
                > 0 and <= 32 => 1,
                0 => 2,
                < 0 and >= -32 => 3,
                < -32 => 4
            }),
            FontPerpetua
            );
    }
    protected override void Back()
    {
        TMProGlyphSupplementation.Instance.SaveGlobalSettings();
        FontManager.autoRefresh = false;
        foreach (var v in fonts.fonts)
        {
            FontManager.ApplyFontConfig(v, config);
        }
        FontManager.autoRefresh = true;
        FontManager.RefreshFonts();
        base.Back();
    }
    protected override void OnEnterMenu()
    {
        titleText.text = fonts.displayName;
        config = TMProGlyphSupplementation.Instance.globalSettings.GetConfig(fonts.innerName);

        Refresh();
    }
}
