
namespace TMProGS.Menu;

class FontListMenu : CustomMenu
{
    public FontListMenu(MenuScreen rs) : base(rs, "HK.TMPro.Menu.PackedFonts".Localize())
    {
        ctrl = new(this);
    }
    public FontControl ctrl;
    public override Font titleFont => FontPerpetua;
    protected override void Build(ContentArea contentArea)
    {
        Dictionary<string, List<FontCache>> fonts = new();
        foreach(var v in TMProGlyphSupplementation.caches)
        {
            if(v is not FontPacked font) continue;
            if(!fonts.TryGetValue(font.Name, out var f))
            {
                f = new();
                fonts[font.Name] = f;
            }
            f.Add(font);
        }
        foreach(var v in fonts)
        {
            AddButton(v.Key, "",
            () =>
            {
                ctrl.fonts = (v.Key, v.Key, v.Value);
                GoToMenu(ctrl);
            }, FontPerpetua);
        }
    }
}
