
namespace TMProGS;

public class FontCollection : IEnumerable<FontCollection.FontAtlasInfo>
{
    public class FontAtlasInfo : FontCache
    {
        internal FontAtlasInfo(FontCollectionState state) : base()
        {
            this.state = state;
        }
        public override void Apply()
        {
            FontManager.AddOverrideFont(this, FontAsset);
        }
        public override void Undo()
        {
            FontManager.RemoveOverrideFont(this);
        }
        public TMP_FontAsset FontAsset
        {
            get
            {
                if (m_fontAsset == null)
                {
                    var fa = FontManager.GetFontAsset("chinese_body");
                    if (fa is null) throw new InvalidOperationException();
                    m_fontAsset = UnityEngine.Object.Instantiate(fa);
                    m_fontAsset.name = Name;
                    m_fontAsset.material = UnityEngine.Object.Instantiate(fa.material);
                    m_fontAsset.fallbackFontAssets = new();
                    m_fontAsset.private_m_glyphInfoList() = glyphs;
                    m_fontAsset.private_m_characterDictionary() = null;
                    m_fontAsset.ReadFontDefinition();
                }
                return m_fontAsset;
            }
        }
        public bool HasChange { get; internal set; } = false;
        internal readonly FontCollectionState state;
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TMPFAI\x0233\x6655");
        internal void Save(string cacheName)
        {
            var p = Path.Combine(TextMeshProGlyphSupplementation.CachePath, GetMD5(cacheName) + ".fai");
            using var writer = new BinaryWriter(File.OpenWrite(p));
            Save(writer);
        }
        private void Save(BinaryWriter writer)
        {
            writer.Write(Magic);
            writer.Write(glyphs.Count);
            byte[] nameByte = Encoding.Unicode.GetBytes(Name);
            writer.Write(nameByte.Length);
            writer.Write(nameByte);
            var texData = atlas!.EncodeToPNG();
            writer.Write(atlas!.width);
            writer.Write(atlas!.height);
            writer.Write(texData.Length);
            writer.Write(texData);
            foreach (var v in glyphs)
            {
                writer.Write(v.id);
                writer.Write(v.x);
                writer.Write(v.y);
                writer.Write(v.width);
                writer.Write(v.height);
            }
        }
        internal FontAtlasInfo(BinaryReader reader, FontCollectionState state) : this(state)
        {
            var magic = reader.ReadBytes(Magic.Length);
            for (int i = 0; i < magic.Length; i++) if (magic[i] != Magic[i]) throw new InvalidOperationException();
            var glc = reader.ReadInt32();
            var nl = reader.ReadInt32();
            Name = Encoding.Unicode.GetString(reader.ReadBytes(nl));
            var tw = reader.ReadInt32();
            var th = reader.ReadInt32();
            var texLen = reader.ReadInt32();
            atlas = new(1, 1, TextureFormat.RGBA32, false);
            var data = reader.ReadBytes(texLen);
            atlas.LoadImage(data);
            for (int i = 0; i < glc; i++)
            {
                var id = reader.ReadInt32();
                var x = reader.ReadInt32();
                var y = reader.ReadInt32();
                var w = reader.ReadInt32();
                var h = reader.ReadInt32();
                var glyph = AddGlyph(id, new(x, y, w, h));
                glyph.y = y;
                state.unicodeTable[id] = this;
            }
        }
        private TMP_FontAsset? m_fontAsset;
        public static FontAtlasInfo FromCache(BinaryReader reader, FontCollectionState state)
        {
            return new(reader, state);
        }
        public static string GetCachePath(string cacheName) => Path.Combine(TextMeshProGlyphSupplementation.CachePath, GetMD5(cacheName) + ".fai");
        public static FontAtlasInfo FromCache(string cacheName, FontCollectionState state)
        {
            var p = GetCachePath(cacheName);
            using var reader = new BinaryReader(File.OpenRead(p));
            return FromCache(reader, state);
        }
        public bool RemoveGlyph(int unicode)
        {
            if (atlas == null || m_fontAsset == null) return false;
            if(!m_fontAsset.characterDictionary.Remove(unicode, out var glyph)) return false;
            HasChange = true;
            glyphs.Remove(glyph);
            return true;
        }
        public TMP_Glyph AddGlyph(int unicode, RectInt rect)
        {
            if (atlas == null) throw new InvalidOperationException();
            TMP_Glyph glyph = new();
            glyph.x = rect.xMin;
            glyph.y = atlas.height - rect.yMax;
            glyph.id = unicode;
            glyph.scale = 1;
            glyph.width = rect.width;
            glyph.height = rect.height;
            glyph.xOffset = 3;
            glyph.yOffset = rect.height;
            glyph.xAdvance = rect.width;
            glyphs.Add(glyph);

            FontAsset.atlas = atlas;
            FontAsset.material.mainTexture = atlas;
            FontAsset.fontInfo.AtlasWidth = atlas.width;
            FontAsset.fontInfo.AtlasHeight = atlas.height;
            FontAsset.characterDictionary[unicode] = glyph;
            HasChange = true;
            return glyph;
        }
    }
    public event Action<FontCollection, FontAtlasInfo?, FontAtlasInfo> onNewAtlas = null!;
    public class FontCollectionState
    {
        public int nextX = 0;
        public int nextY = 0;
        public int nextYEx = 0;
        public List<FontAtlasInfo> atlas = new();
        public FontAtlasInfo currentAtlas;
        public byte[] guid { get; private set; }
        public string guidStr => BitConverter.ToString(guid);
        internal readonly Dictionary<int, FontAtlasInfo> unicodeTable = new();
        private static readonly byte[] Magic = Encoding.ASCII.GetBytes("TMPFCC\x0233\x6655");
        
        internal FontCollectionState()
        {
            currentAtlas = new(this);
            guid = Guid.NewGuid().ToByteArray();
        }
        public FontAtlasInfo? FindGlyph(int unicode)
        {
            return unicodeTable.TryGetValue(unicode, out var result) ? result : null;
        }
        internal FontCollectionState(BinaryReader reader)
        {
            var magic = reader.ReadBytes(Magic.Length);
            guid = reader.ReadBytes(16);
            for (int i = 0; i < magic.Length; i++) if (magic[i] != Magic[i]) throw new InvalidOperationException();
            nextX = reader.ReadInt32();
            nextY = reader.ReadInt32();
            nextYEx = reader.ReadInt32();
            var ac = reader.ReadInt32();
            var g = guidStr;
            if (reader.ReadByte() == 1)
            {
                currentAtlas = FontAtlasInfo.FromCache(g + "--0", this);
            }
            else
            {
                nextX = 0;
                nextY = 0;
                nextYEx = 0;
                currentAtlas = new(this);
            }
            for (int i = 0; i < ac; i++)
            {
                atlas.Add(FontAtlasInfo.FromCache(g + "-" + i, this));
            }
        }
        public void SaveCache(BinaryWriter writer, bool force = true)
        {
            writer.Write(Magic);
            writer.Write(guid);
            writer.Write(nextX);
            writer.Write(nextY);
            writer.Write(nextYEx);
            writer.Write(atlas.Count);
            writer.Flush();
            var g = guidStr;
            if (currentAtlas.atlas != null && currentAtlas.HasChange)
            {
                writer.Write((byte)1);
                currentAtlas.Save(g + "--0");
            }
            else
            {
                writer.Write((byte)0);
            }
            int id = 0;
            foreach (var v in atlas)
            {
                var sp = g + "-" + id;
                if (force || v.HasChange || !File.Exists(FontAtlasInfo.GetCachePath(sp)))
                {
                    v.Save(sp);
                }
            }
        }
    }
    private FontCollectionState state = new();
    public int padding { get; set; } = 32;
    public bool HasChange => this.Any(x => x.HasChange);
    public virtual Vector2 atlasSize => TextMeshProGlyphSupplementation.Instance.globalSettings.AtlasSize switch
    {
        -2 => new(16384, 16384),
        -1 => new(8192, 8192),
        0 => new(4096, 4096),
        1 => new(2048, 2048),
        2 => new(1024, 1024),
        _ => new(4096, 4096)
    };
    public void LoadCache(BinaryReader reader)
    {
        state = new(reader);
    }
    public void SaveCache(BinaryWriter writer, bool force = true)
    {
        state.SaveCache(writer, force);
    }
    private static string GetMD5(string str)
    {
        var arrayByteHashValue = new System.Security.Cryptography.MD5CryptoServiceProvider().ComputeHash(Encoding.Unicode.GetBytes(str));
        return BitConverter.ToString(arrayByteHashValue).Replace("-", String.Empty).ToLower();
    }
    public void SaveCache(string id, bool force = true)
    {
        var p = Path.Combine(TextMeshProGlyphSupplementation.CachePath, GetMD5(id) + ".fcc");
        if(!File.Exists(p))
        {
            force = true;
        }
        using var writer = new BinaryWriter(File.OpenWrite(p));
        SaveCache(writer, force);
    }
    public bool LoadCache(string id)
    {
        var p = Path.Combine(TextMeshProGlyphSupplementation.CachePath, GetMD5(id) + ".fcc");
        if (!File.Exists(p)) return false;
        using var reader = new BinaryReader(File.OpenRead(p));
        LoadCache(reader);
        return true;
    }
    public bool HasGlyph(int unicode) => state.FindGlyph(unicode) is not null;
    IEnumerator<FontCollection.FontAtlasInfo> IEnumerable<FontCollection.FontAtlasInfo>.GetEnumerator() => state.atlas.Append(state.currentAtlas)
        .GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<FontCollection.FontAtlasInfo>)this).GetEnumerator();
    public bool RemoveGlyph(int unicode)
    {
        var fai = this.FirstOrDefault(x => x.glyphs.Any(x1 => x1.id == unicode));
        if(fai is null) return false;
        return fai.RemoveGlyph(unicode);
    }
    public bool AutoApply { get; set; } = true;
    public void AddGlyph(int unicode, Texture2D tex, bool canreplace = true)
    {
        var fai = state.FindGlyph(unicode);
        if(fai is not null)
        {
            if(canreplace)
            {
                fai.RemoveGlyph(unicode);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }
        _RE_TRY:
        int catlasW = state.currentAtlas.atlas?.width ?? -1;
        int catlasH = state.currentAtlas.atlas?.height ?? -1;


        if (state.nextX + tex.width + padding > catlasW)
        {
            state.nextY = state.nextYEx;
            state.nextX = 0;
        }
        if (state.nextY + tex.height + padding > state.nextYEx)
        {
            state.nextYEx = state.nextY + tex.height + padding;
        }
        if (state.nextYEx >= catlasH)
        {
            FontAtlasInfo? old = null;
            if (state.currentAtlas.atlas != null)
            {
                old = state.currentAtlas;
                state.atlas.Add(state.currentAtlas);
                state.currentAtlas = new(state);
            }
            state.currentAtlas.atlas = new((int)atlasSize.x, (int)atlasSize.y, TextureFormat.Alpha8, false);
            var c = state.currentAtlas.atlas.GetRawTextureData<Color32>();
            var col = new Color32(0, 0, 0, 0);
            for (int i = 0; i < c.Length; i++)
            {
                c[i] = col;
            }
            onNewAtlas?.Invoke(this, old, state.currentAtlas);
            goto _RE_TRY;
        }

        RectInt rect = new(state.nextX, state.nextY, tex.width, tex.height);

        var atlas = state.currentAtlas.atlas!;
        /*var cols = atlas.GetRawTextureData<Color32>();
        var scols = tex.GetPixels32(0);
        for (int y = 0; y < tex.height; y++)
        {
            var xstart = (y + state.nextY) * catlasW + state.nextX;
            var sxstart = tex.width * y;
            for (int x = 0; x < tex.width; x++)
            {
                cols[xstart + x] = scols[sxstart + x];
            }
        }
        if(AutoApply) atlas.Apply();
        else requireApply.Add(atlas);*/
        tex.CopyTo(atlas, rect.min, new(0,0), rect.size);

        state.nextX += tex.width + padding;

        state.currentAtlas.AddGlyph(unicode, rect);
    }
}
