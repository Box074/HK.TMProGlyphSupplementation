
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
    public (string, string, List<FontCache>) font = ("HK.TMPro.Menu.UnpackedFont".Localize(), TMProGlyphSupplementation.UnpackedInnerName
        , new(){TMProGlyphSupplementation.unpackedFont});
    protected override void Build(ContentArea contentArea)
    {
        AddButton("HK.TMPro.Menu.PackedFonts".Localize(), "", ()=>
        {
            listMenu.Rebuild();
            GoToMenu(listMenu);
        }, FontPerpetua);
        AddButton("HK.TMPro.Menu.UnpackedFont".Localize(), "", () =>
        {
            ctrl.fonts = font;
            GoToMenu(ctrl);
        }, FontPerpetua);
    }
}
