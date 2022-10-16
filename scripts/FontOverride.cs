
namespace TMProGS;

public class FontBase : FontCache
{
    public TMP_FontAsset? FontAsset { get; private set; } = null;

    public FontBase(FontMaker maker) : base(maker)
    {
    }

    public override void Apply()
    {
        Update();
        if(FontAsset is null)
        {
            FontAsset = Build(string.IsNullOrEmpty(TemplateFontName) ? "chinese_body" : TemplateFontName!);
        }
        else
        {
            Replace(FontAsset);
        }
        FontAsset.name = Name;
        FontManager.AddOverrideFont(this, FontAsset);
        //if(!FontManager.OverrideParent.fallbackFontAssets.Contains(FontAsset)) FontManager.OverrideParent.fallbackFontAssets.Add(FontAsset);
    }
    public override void Undo()
    {
        if(FontAsset is null) return;
        FontManager.RemoveOverrideFont(this);
    }
}
