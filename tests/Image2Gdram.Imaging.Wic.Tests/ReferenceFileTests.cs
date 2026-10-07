using System.Globalization;
using System.IO;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;
using Image2Gdram.Imaging.Wic;

namespace Image2Gdram.Imaging.Wic.Tests;

/// <summary>
/// Побайтное сравнение упаковки ядра с файлами <c>testdata/reference</c> (раздел 11 ТЗ, N-28, N-60).
/// Пиксели читаются из PNG, а не из памяти генератора.
/// </summary>
public class ReferenceFileTests
{
    private static readonly string Root = FindRoot();

    [Fact]
    public void Images_and_font_sheets_match_reference_bins_and_c_files()
    {
        var decoder = new WicImageDecoder();
        var packer = new Mono1bppPacker();
        string referenceRoot = Path.Combine(Root, "testdata", "reference");
        string[] folders = Directory.GetDirectories(referenceRoot)
            .Select(Path.GetFileName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray()!;
        Assert.Equal(
            ["font_12x16", "font_6x8", "font_8x8", "test_pattern_128x64", "test_pattern_240x128", "test_sprite_13x11"],
            folders);

        foreach (string folder in folders)
        {
            bool font = folder.StartsWith("font_", StringComparison.Ordinal);
            MonoBitmap image = font ? null! : LoadMono(decoder, Path.Combine(Root, "testdata", folder + ".png"));
            FontTable? table = font ? LoadFont(decoder, folder) : null;
            int width = font ? table!.Cell.Width : image.Width;
            int height = font ? table!.Cell.Height : image.Height;
            string directory = Path.Combine(referenceRoot, folder);
            string[] names = Directory.GetFiles(directory, "*.bin")
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray()!;
            Assert.Equal(16, names.Length);

            foreach (string name in names)
            {
                PackingOptions options = Parse(name!);
                byte[] expected = File.ReadAllBytes(Path.Combine(directory, name + ".bin"));
                byte[] actual = font ? table!.Pack(packer, options) : packer.Pack(image, options);
                Assert.Equal(expected, actual);
                int symbols = font ? 256 : 1;
                Assert.Equal(packer.GetSize(width, height, options) * symbols, expected.Length);

                IReadOnlyList<ImportedArray> arrays = ArrayImportParser.Parse(File.ReadAllText(Path.Combine(directory, name + ".c")), ImportSyntax.C);
                ImportedArray array = Assert.Single(arrays);
                Assert.Equal(folder + "_" + name, array.Name);
                Assert.Equal(expected, array.Values);
            }
        }

        string index = File.ReadAllText(Path.Combine(referenceRoot, "index.md"));
        foreach (string folder in folders)
        {
            Assert.Contains(folder, index, StringComparison.Ordinal);
        }

        Assert.Contains("Q-08", index, StringComparison.Ordinal);
    }

    private static FontTable LoadFont(WicImageDecoder decoder, string folder)
    {
        (int width, int height) = folder switch
        {
            "font_6x8" => (6, 8),
            "font_8x8" => (8, 8),
            "font_12x16" => (12, 16),
            _ => throw new InvalidOperationException(folder),
        };
        string path = Path.Combine(Root, "testdata", "font_sheet_" + width + "x" + height + ".png");
        RgbaImage frame = decoder.Decode(path).Frames[0];
        Assert.Equal(width * 16, frame.Width);
        Assert.Equal(height * 16, frame.Height);
        Assert.True(CharRangeSet.TryParseCustom("0x00-0xFF", out IReadOnlyList<int> codes, out CharRangeParseError? error));
        Assert.Null(error);

        var source = new SheetGlyphSource(
            frame,
            Path.GetFileName(path),
            new SheetOptions { CellWidth = width, CellHeight = height, CharsPerRow = 16 });
        FontCellSize cell = FontCellSize.Get(width, height);
        GlyphSourceResult rendered = source.Render(cell, new CharRangeSet(CharRangePreset.None, codes));
        Assert.Empty(rendered.Diagnostics);
        Assert.Equal(256, rendered.Glyphs.Count);
        var table = new FontTable(cell);
        table.ReplaceSource(rendered.Glyphs);
        return table;
    }

    private static MonoBitmap LoadMono(WicImageDecoder decoder, string path)
    {
        RgbaImage frame = decoder.Decode(path).Frames[0];
        var bitmap = new MonoBitmap(frame.Width, frame.Height);
        for (int y = 0; y < frame.Height; y++)
        {
            for (int x = 0; x < frame.Width; x++)
            {
                frame.GetPixel(x, y, out byte r, out byte g, out byte b, out byte a);
                byte rr = BackgroundCompositor.Channel(r, a, 255);
                byte gg = BackgroundCompositor.Channel(g, a, 255);
                byte bb = BackgroundCompositor.Channel(b, a, 255);
                bitmap[x, y] = GrayscaleStep.Luminance(rr, gg, bb) < 128;
            }
        }

        return bitmap;
    }

    private static PackingOptions Parse(string name)
    {
        bool invert = name.EndsWith("_inv", StringComparison.Ordinal);
        string body = invert ? name[..^4] : name;
        string[] parts = body.Split('_');
        if (parts.Length != 3)
        {
            throw new InvalidOperationException(name);
        }

        if (parts[0] == "h")
        {
            return new PackingOptions
            {
                Direction = PackDirection.Horizontal,
                BitOrder = parts[1] == "msb" ? BitOrder.MsbFirst : BitOrder.LsbFirst,
                BitsPerByte = int.Parse(parts[2], CultureInfo.InvariantCulture),
                Invert = invert,
            };
        }

        if (parts[0] != "v" || (parts[1] != "lsb" && parts[1] != "msb") || (parts[2] != "pages" && parts[2] != "cols"))
        {
            throw new InvalidOperationException(name);
        }

        return new PackingOptions
        {
            Direction = PackDirection.Vertical,
            BitOrder = parts[1] == "msb" ? BitOrder.MsbFirst : BitOrder.LsbFirst,
            PageTraversal = parts[2] == "cols" ? PageTraversal.ByColumns : PageTraversal.ByPages,
            Invert = invert,
        };
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "specification.md")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Repository root was not found.");
    }
}
