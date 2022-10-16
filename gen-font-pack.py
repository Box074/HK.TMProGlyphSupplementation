from sys import argv
from os import makedirs, path
from typing import Dict, Tuple
from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFont
from json import dumps

if len(argv) < 5:
    print('''
    Usage:\n
    py gen-font-pack.py <Font Path> <Out dir> <Atlas Width> <Atlas Height>
    ''')
    exit()

font = ImageFont.truetype(argv[1], 68)


if not path.exists(argv[2]):
    makedirs(argv[2])

hstr = []#"大家啊哦安排靠卡片卡片将扩大"#[]
fobj = TTFont(argv[1])
dict = fobj.getBestCmap()
for key, _ in dict.items():
    hstr.append(chr(key))

wordcount = len(hstr)
index = 0

imgs: list[Tuple[int, Image.Image]] = []

for c in hstr:
    try:
        index+=1
        size = font.getbbox(c)
        img = Image.new("RGBA", (size[2], size[3]), (0, 0, 0, 0))
        draw = ImageDraw.Draw(img)
        draw.text((0, 0), c, font=font, fill='white')

        width, height = img.size
        minx = 99999
        miny = 99999
        maxx = -10
        maxy = -10
        pix = img.load()
        for x in range(0, width):
            for y in range(0, height):
                if pix[x, y][3] != 0:
                    if minx > x:
                        minx = x
                    if miny > y:
                        miny = y
                    if maxx < x:
                        maxx = x
                    if maxy < y:
                        maxy = y
        imgs.append((ord(c), img))
    except Exception as r:
        print(r)


paddingX = 64
paddingY = 64

atlas = Image.new("RGBA", (int(argv[3]), int(argv[4])), (0, 0, 0, 0))
atlasDraw = ImageDraw.Draw(atlas)
ltopY = paddingY
atlasX = paddingX
atlasY = paddingY

# unicode, x, y, width, height
atlasInfo: Dict[int, Tuple[int, int, int, int]] = {}

def save_atlas(id: int, atlas: Image.Image, info: list[Tuple[int, int, int, int, int]]):
    atlas.save(argv[2] + "\\" + str(id) + ".png")
    f = open(argv[2] + "\\" + str(id) + ".json", "w")
    binfo = {}
    binfo["data"] = info
    binfo["fontpath"] = argv[1]
    f.write(dumps(binfo))
    f.close()

index = 0

for t in imgs:
    img = t[1]
    if atlasX + img.width + paddingX >= atlas.width:
        atlasX = paddingX
        ltopY = atlasY
    if (atlasY - ltopY) < img.height + paddingY:
        atlasY = ltopY + img.height + paddingY
    if ltopY + img.height >= atlas.height:
        save_atlas(index, atlas, atlasInfo)
        index+=1
        atlas = Image.new("RGBA", (int(argv[3]), int(argv[4])), (0, 0, 0, 0))
        atlasDraw = ImageDraw.Draw(atlas)
        ltopY = paddingY
        atlasX = paddingX
        atlasY = paddingY
        atlasInfo = {}
    
    atlasDraw.bitmap((atlasX, ltopY), img)
    atlasInfo[t[0]] = (atlasX, ltopY, img.width, img.height)

    atlasX = atlasX + img.width + paddingX

save_atlas(index, atlas, atlasInfo)
