
namespace TMProGS;

public class FontPacked : FontCache
{
    public class FontPackedInfo
    {
        public string fontpath { get; set; } = null!;
        public Dictionary<int, List<int>> data { get; set; } = null!;
    }
    public TMP_FontAsset? FontAsset { get; private set; } = null;
    private byte[] ba;
    private FontPackedInfo info;
    private string fontPath;
    public FontPacked(byte[] json, byte[] atlas, string p) : base(null!)
    {
        ba = atlas;
        info = JsonConvert.DeserializeObject<FontPackedInfo>(Encoding.UTF8.GetString(json))!;
        if (info is null) throw new InvalidOperationException();
        Name = Path.GetFileNameWithoutExtension(info.fontpath);
        fontPath = p;
        glyphs = null!;

    }
    protected override bool Update(FontMaker maker)
    {
        if (glyphs is null)
        {
            glyphs = new();
            this.atlas = new Texture2D(1, 1);
            this.atlas.LoadImage(ba);
            ba = null!;
            foreach (var v in info.data)
            {
                var gl = new TMP_Glyph();
                gl.id = v.Key;
                gl.x = v.Value[0];
                gl.y = v.Value[1];
                gl.width = v.Value[2];
                gl.height = v.Value[3];
                gl.xAdvance = gl.width;
                gl.yOffset = gl.height;
                gl.xOffset = 1;
                gl.scale = 1;
                glyphs.Add(gl);
            }
        }
        return true;
    }
    protected override void Replace(TMP_FontAsset asset)
    {
        base.Replace(asset);
        asset.name = fontPath;
    }
    public override string ToString()
    {
        return fontPath;
    }
    public override void Apply()
    {
        Update();
        if (FontAsset is null)
        {
            FontAsset = Build(string.IsNullOrEmpty(TemplateFontName) ? "chinese_body" : TemplateFontName!);
        }
        else
        {
            Replace(FontAsset);
        }
        FontManager.AddOverrideFont(this, FontAsset);
    }
    public override void Undo()
    {
        if (FontAsset is null) return;
        FontManager.RemoveOverrideFont(this);
    }
}
