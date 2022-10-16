
from sys import argv
from os import makedirs, path
from fontTools.ttLib import TTFont
from PIL import Image, ImageDraw, ImageFont

if len(argv) < 4:
    print('''
    Usage:\n
    py font_maker.py <Font Path> <Out path> <String>
    py font_maker.py <Font Path> <Out path> all
    ''')
    exit()

font = ImageFont.truetype(argv[1], 68)


if not path.exists(argv[2]):
    makedirs(argv[2])

hstr = []
if argv[3] == "all":
    fobj = TTFont(argv[1])
    dict = fobj.getBestCmap()
    for key, _ in dict.items():
        hstr.append(chr(key))
else:
    hstr = argv[3]


for c in hstr:
    try:
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

        img.save(
            argv[2] + "\\" + str(ord(c)) + ".png"
            )
    except Exception as r:
        print(r)

