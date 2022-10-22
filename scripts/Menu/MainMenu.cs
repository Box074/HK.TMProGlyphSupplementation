
namespace TMProGS.Menu;

class MainMenu : CustomMenu
{
    public FontListMenu listMenu;
    public FontControl ctrl;
    public override Font titleFont => FontPerpetua;
    public MainMenu(MenuScreen rs) : base(rs, "Text Mesh Pro Font")
    {
        listMenu = new(this);
        ctrl = new(this);
    }
    public (string, string, List<FontCache>) font = ("HK.TMPro.Menu.UnpackedFont".Localize(), TextMeshProGlyphSupplementation.UnpackedInnerName
        , null!);
    protected override void Back()
    {
        TextMeshProGlyphSupplementation.Instance.SaveGlobalSettings();
        base.Back();
    }
    protected override void Build(ContentArea contentArea)
    {
        AddButton("HK.TMPro.Menu.PackedFonts".Localize(), "", ()=>
        {
            listMenu.Rebuild();
            GoToMenu(listMenu);
        }, FontPerpetua);
        AddButton("HK.TMPro.Menu.UnpackedFont".Localize(), "", () =>
        {
            font.Item3 = new(TextMeshProGlyphSupplementation.unpackedFont);
            ctrl.fonts = font;
            GoToMenu(ctrl);
        }, FontPerpetua);
        AddOption("HK.TMPro.Menu.AtlasSize".Localize(), "HK.TMPro.Menu.AtlasSize.Desc".Localize(), new[]{
            "4096x4096",
            "2048x2048",
            "1024x1024"
        }, val => TextMeshProGlyphSupplementation.Instance.globalSettings.AtlasSize = val,
        () => TextMeshProGlyphSupplementation.Instance.globalSettings.AtlasSize, FontPerpetua);
    }
}
