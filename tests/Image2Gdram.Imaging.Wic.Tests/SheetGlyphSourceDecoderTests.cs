using System.IO;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Imaging.Wic;
using Image2Gdram.TestAssets;

namespace Image2Gdram.Imaging.Wic.Tests;

/// <summary>PNG-лист, записанный кодировщиком тестовых изображений, читается WIC тем же результатом, что и <c>RgbaImage</c>.</summary>
public sealed class SheetGlyphSourceDecoderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-sheet-" + Guid.NewGuid().ToString("N"));

    public SheetGlyphSourceDecoderTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void Png_sheet_matches_the_rgba_source()
    {
        var options = new SheetOptions
        {
            CellWidth = 6,
            CellHeight = 8,
            MarginX = 2,
            MarginY = 1,
            SpacingX = 3,
            SpacingY = 2,
            CharsPerRow = 4,
            Threshold = 128,
        };
        const int count = 8;
        int rows = (count + options.CharsPerRow - 1) / options.CharsPerRow;
        int width = options.MarginX + (options.CharsPerRow * options.CellWidth) + ((options.CharsPerRow - 1) * options.SpacingX);
        int height = options.MarginY + (rows * options.CellHeight) + ((rows - 1) * options.SpacingY);
        var active = new bool[width * height];
        for (int code = 0; code < count; code++)
        {
            int col = code % options.CharsPerRow;
            int row = code / options.CharsPerRow;
            int x0 = options.MarginX + (col * (options.CellWidth + options.SpacingX));
            int y0 = options.MarginY + (row * (options.CellHeight + options.SpacingY));
            active[(y0 * width) + x0 + (code % options.CellWidth)] = true;
            active[((y0 + 1) * width) + x0 + 1] = code % 2 == 0;
        }

        var rgba = new RgbaImage(width, height, 255, 255, 255, 255);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (active[(y * width) + x])
                {
                    rgba.SetPixel(x, y, 0, 0, 0, 255);
                }
            }
        }

        string path = Path.Combine(_directory, "sheet.png");
        using (FileStream stream = File.Create(path))
        {
            PngWriter.Write(stream, width, height, (x, y) => active[(y * width) + x]);
        }

        RgbaImage decoded = new WicImageDecoder().Decode(path).Frames[0];
        var ranges = new CharRangeSet(CharRangePreset.None, Enumerable.Range(0, count));
        GlyphSet fromMemory = new SheetGlyphSource(rgba, path, options).Render(FontCellSize.Cell6x8, ranges).Glyphs;
        GlyphSet fromFile = new SheetGlyphSource(decoded, path, options).Render(FontCellSize.Cell6x8, ranges).Glyphs;

        Assert.Equal(fromMemory.Codes, fromFile.Codes);
        foreach (int code in fromMemory.Codes)
        {
            Assert.True(fromMemory.Get(code)!.ContentEquals(fromFile.Get(code)));
        }
    }
}
