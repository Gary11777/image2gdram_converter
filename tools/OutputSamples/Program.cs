using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.TestAssets;

// Образцы вывода всех генераторов для дымовой компиляции (tools/compile-check.ps1) и для просмотра.
// Использование: OutputSamples <папка>. Дата в заголовке отключена, повторный запуск даёт те же файлы.

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: OutputSamples <output directory>");
    return 2;
}

string root = Path.GetFullPath(args[0]);
var registry = OutputGeneratorRegistry.CreateDefault();
var packer = new Mono1bppPacker();

var ssd1306 = PresetInfo.Named("OLED128X64-0.96", "SSD1306");
var t6963c = PresetInfo.Named("WG240128A", "RA6963 / T6963C");
var vertical = PackingOptions.Default;
var horizontal = new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst };

var images = new List<(string Name, OutputData Data)>
{
    ("pattern_128x64", Image(TestPattern.CreateScreen(128, 64), vertical, "test_pattern_128x64.png", ssd1306)),
    ("pattern_240x128", Image(TestPattern.CreateScreen(240, 128), horizontal, "test_pattern_240x128.png", t6963c)),
    ("sprite_13x11", Image(TestPattern.CreateSprite(), vertical with { Invert = true }, "test_sprite_13x11.png", PresetInfo.Custom)),
    ("anim_16x16", Frames(3, 16, 16, vertical)),
};
var fonts = new List<(string Name, OutputData Data)>
{
    ("font_6x8", Font(6, 8, vertical)),
    ("font_8x8", Font(8, 8, horizontal with { BitsPerByte = 6 })),
    ("font_12x16", Font(12, 16, vertical with { PageTraversal = PageTraversal.ByColumns })),
};

var targets = new (string Folder, OutputOptions Options)[]
{
    ("stm32", new OutputOptions { Format = OutputFormat.CStm32 }),
    ("stm32_uchar", new OutputOptions { Format = OutputFormat.CStm32, Stm32ElementType = Stm32ElementType.UnsignedChar }),
    ("stm32_utf8", new OutputOptions { Format = OutputFormat.CStm32, Encoding = OutputEncoding.Utf8NoBom }),
    ("c51", new OutputOptions { Format = OutputFormat.CKeilC51 }),
    ("a51_module", new OutputOptions { Format = OutputFormat.A51Module }),
    ("a51_include", new OutputOptions { Format = OutputFormat.A51Include, AsmNumberFormat = AsmNumberFormat.Binary }),
    ("bin", new OutputOptions { Format = OutputFormat.Bin }),
};

foreach ((string folder, OutputOptions options) in targets)
{
    string directory = Path.Combine(root, folder);
    Directory.CreateDirectory(directory);
    foreach ((string name, OutputData data) in images.Concat(fonts))
    {
        OutputDocument document = registry.Generate(data, options with { ArrayName = name, IncludeDate = false, BytesPerLine = 16 });
        OutputWriter.Save(document, directory, _ => true);
    }

    Console.WriteLine(directory);
}

return 0;

OutputData Image(PatternBitmap pattern, PackingOptions packing, string fileName, PresetInfo preset)
{
    var bitmap = new MonoBitmap(pattern.Width, pattern.Height);
    for (int y = 0; y < pattern.Height; y++)
    {
        for (int x = 0; x < pattern.Width; x++)
        {
            bitmap[x, y] = pattern.IsActive(x, y);
        }
    }

    return new ImageOutputData(bitmap.Width, bitmap.Height, packer.Pack(bitmap, packing), packing, new ImageSourceInfo(fileName), preset);
}

OutputData Frames(int count, int width, int height, PackingOptions packing)
{
    var frames = new List<byte[]>();
    for (int f = 0; f < count; f++)
    {
        var bitmap = new MonoBitmap(width, height);
        for (int i = 0; i < Math.Min(width, height); i++)
        {
            bitmap[(i + f) % width, i] = true;
        }

        frames.Add(packer.Pack(bitmap, packing));
    }

    return new ImageOutputData(width, height, frames, allFrames: true, packing, new ImageSourceInfo("anim.gif", count), ssd1306);
}

OutputData Font(int width, int height, PackingOptions packing)
{
    int bytesPerChar = packer.GetSize(width, height, packing);
    var data = new byte[FontOutputData.CharCount * bytesPerChar];
    for (int i = 0; i < data.Length; i++)
    {
        data[i] = (byte)(i * 37 + i / bytesPerChar);
    }

    return new FontOutputData(width, height, data, packing, FontSourceInfo.Sheet($"font_{width}x{height}.png"), ssd1306);
}
