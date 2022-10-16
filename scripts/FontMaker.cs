
namespace TMProGS;

public class FontMaker
{
    public class GlyphInfo
    {
        public int id;
        public Texture2D tex = null!;
    }
    internal List<GlyphInfo> glyphs = new();

    public void AddGlyph(int id, Texture2D tex)
    {
        var inst = glyphs.FirstOrDefault(x => x.id == id);
        if(inst is null)
        {
            inst = new();
            inst.id = id;
            glyphs.Add(inst);
        }
        inst.tex = tex;
    }

}
