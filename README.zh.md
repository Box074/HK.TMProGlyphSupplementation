
# TMPro Glyph Supplementation

字体资源一般存放于`mod安装目录\fonts`目录下，可以通过全局配置文件中的`fontsDir`更改。以下提到的`fonts`目录未特殊说明均指该目录。

目录结构：

```text
+-- font
|   |
|   +-- packed
|   |   |
|   |   +-- <font name>
|   |   |   |
|   |   |   + <atlas id>.json
|   |   |   |
|   |   |   + <atlas id>.png
|   |   |   |
|   |   |   + ...
|   |   |   |
|   +-- dispersive
|   |   |
|   |   +-- <unicode>.png
|   |   |
|   |   +-- ...
|   |   |  |
|   |   |  + <unicode>.png
```

分散（未打包）的字形图像以`<unicode>.png`作为文件名，储存在`fonts\dispersive`目录下，在游戏启动时它们会被合并为一个字体资源。你可以使用`font-maker.py`批量生成分散（未打包）的字形图像。命令行格式为`py font_maker.py <ttf字体文件路径> <输出文件夹> <字符串>`。

已打包的字体图集一般位于`fonts\packed`目录下，以`<atlas id>.png`和`<atlas id>.json`为一组，通常一个字体会包含多个字体图集。你可以使用`gen-font-pack.py`生成完整的字体图集。命令行格式为`py gen-font-pack.py <ttf字体文件路径> <输出文件夹> <图集宽度> <图集高度>`。`图集高度`和`图集宽度`都不能小于1024。
`<atlas id>.json`内容：

```json
{
    "fontpath": <字体文件名称>,
    "data": {
        "<Unicode码>": [<x>, <y>, <width>, <height>],
        ......
    }
}
```
