
namespace TMProGS;

public abstract class FontCache
{
    public enum FontMode
    {
        Override,
        AsFallback
    }
    public FontCache(FontMaker maker)
    {
        this.maker = maker;
    }
    public FontCache() : this(null!)
    {}
    protected FontMaker maker;
    public FontMode Mode { get; set; } = FontMode.Override;
    public int Priority { get; set; } = 0;
    public string Name { get; set; } = "";
    public string? TemplateFontName { get; set; } = "";
    public virtual bool Update() => Update(maker);
    public virtual int AtlasPadding => 24;
    protected virtual bool Update(FontMaker maker)
    {
        var atlasTex = new Texture2D(1,1);
        var mglyphs = maker.glyphs;
        var arr = new Texture2D[mglyphs.Count];
        for(int i = 0; i < mglyphs.Count ; i++)
        {
            var g = maker.glyphs[i];
            if(g.tex is null) g.tex = Texture2D.whiteTexture;
            arr[i] = g.tex;
        }
        var rects = atlasTex.PackTextures(arr, AtlasPadding, 8192, false);
        if(rects == null) return false;
        glyphs.Clear();

        for(int i = 0; i < mglyphs.Count; i++)
        {
            var glyph = mglyphs[i];
            var g = new TMP_Glyph();
            var uv = rects[i];
            g.id = glyph.id;

            g.x = uv.xMin * atlasTex.width;
            g.y = (1 - uv.yMax) * atlasTex.height;

            g.width = uv.width * atlasTex.width;
            g.height = uv.height * atlasTex.height;

            g.scale = 1;

            g.xAdvance = uv.width * atlasTex.width;

            g.xOffset = 1;
            g.yOffset = uv.height * atlasTex.height;
            glyphs.Add(g);
        }
        if(this.atlas != null)
        {
            UnityEngine.Object.Destroy(this.atlas);
            this.atlas = null;
        }
        this.atlas = atlasTex;
        return true;
    }
    public abstract void Apply();
    public abstract void Undo();
    protected TMP_FontAsset Build(string templateName)
    {
        var fa = FontManager.GetFontAsset(templateName);
        if(fa is null) throw new InvalidOperationException();
        return Build(fa);
    }

    protected virtual void Replace(TMP_FontAsset asset)
    {
        asset.private_m_characterDictionary() = null;
        asset.AddGlyphInfo(glyphs.ToArray());
        asset.material.mainTexture = Atlas;
        asset.atlas = Atlas;
        asset.ReadFontDefinition();
        asset.fontWeights = new TMP_FontWeights[0];
        //asset.normalStyle = 0;
        //asset.boldStyle = 0;
        var info = asset.private_m_fontInfo();
        info.AtlasWidth = asset.atlas!.width;
        info.AtlasHeight = asset.atlas!.height;
    }

    protected TMP_FontAsset Build(TMP_FontAsset template)
    {
        if(template is null) throw new ArgumentNullException(nameof(template));
        var font = UnityEngine.Object.Instantiate(template);
        font.name = Name;
        font.material = UnityEngine.Object.Instantiate(template.material);
        Replace(font);
        return font;
    }

    public override string ToString()
    {
        return Name;
    }

    [NonSerialized]
    internal protected Texture2D? atlas;
    public Texture2D? Atlas
    {
        get
        {
            return atlas;
        }
    }
    public List<TMP_Glyph> glyphs = new();
}
