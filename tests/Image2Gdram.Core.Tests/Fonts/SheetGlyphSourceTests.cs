using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Растровый лист символов (п. 4.2.2 ТЗ, источник 2; решения N-12, N-27, N-51).</summary>
public class SheetGlyphSourceTests
{
    [Theory]
    [InlineData(6, 8)]
    [InlineData(8, 8)]
    [InlineData(12, 16)]
    public void Cells_match_the_sheet_with_distinct_margins_and_spacing(int width, int height)
    {
        FontCellSize cell = FontCellSize.Get(width, height);
        var options = new SheetOptions
        {
            CellWidth = width,
            CellHeight = height,
            MarginX = 3,
            MarginY = 5,
            SpacingX = 2,
            SpacingY = 4,
            CharsPerRow = 16,
            Threshold = 128,
        };
        RgbaImage sheet = Paint(options, 256, code => Pattern(width, height, code));
        GlyphSet glyphs = Render(sheet, "sheet.png", options, cell, AllCodes());

        Assert.Equal(256, glyphs.Count);
        for (int code = 0; code < 256; code++)
        {
            AssertSame(Pattern(width, height, code), glyphs.Get(code));
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(256)]
    public void Chars_per_row_places_the_next_cell(int charsPerRow)
    {
        var options = new SheetOptions { CellWidth = 6, CellHeight = 8, MarginX = 1, MarginY = 1, SpacingX = 2, SpacingY = 3, CharsPerRow = charsPerRow };
        const int count = 20;
        RgbaImage sheet = Paint(options, count, code => Pattern(6, 8, code));
        GlyphSet glyphs = Render(sheet, "row.png", options, FontCellSize.Cell6x8, Codes(0, count - 1));

        for (int code = 0; code < count; code++)
        {
            AssertSame(Pattern(6, 8, code), glyphs.Get(code));
        }

        Assert.False(glyphs.Contains(count));
    }

    [Theory]
    [InlineData(0x20)]
    [InlineData(0xF0)]
    public void First_code_limits_filled_codes(int firstCode)
    {
        var options = new SheetOptions { CellWidth = 6, CellHeight = 8, CharsPerRow = 16, FirstCode = firstCode };
        int count = 0x100 - firstCode;
        RgbaImage sheet = Paint(options, count + 4, code => Pattern(6, 8, firstCode + code));
        GlyphSet glyphs = Render(sheet, "first.png", options, FontCellSize.Cell6x8, AllCodes());

        Assert.Equal(count, glyphs.Count);
        Assert.False(glyphs.Contains(firstCode - 1));
        AssertSame(Pattern(6, 8, firstCode), glyphs.Get(firstCode));
        AssertSame(Pattern(6, 8, 0xFF), glyphs.Get(0xFF));
        Assert.DoesNotContain(glyphs.Codes, code => code > 0xFF);
    }

    [Fact]
    public void Larger_sheet_cell_is_cropped_and_smaller_one_is_padded()
    {
        var large = new SheetOptions { CellWidth = 10, CellHeight = 10, CharsPerRow = 1 };
        MonoBitmap big = new(10, 10);
        big[1, 1] = true;
        big[8, 9] = true;
        GlyphSet cropped = Render(Paint(large, 1, _ => big), "big.png", large, FontCellSize.Cell6x8, Codes(0, 0));
        MonoBitmap croppedExpected = GlyphOps.Fit(big, FontCellSize.Cell6x8);
        Assert.True(big[8, 9]);
        AssertSame(croppedExpected, cropped.Get(0));
        Assert.True(croppedExpected[1, 1]);
        Assert.Equal(6, croppedExpected.Width);

        var small = new SheetOptions { CellWidth = 5, CellHeight = 7, CharsPerRow = 1 };
        MonoBitmap tiny = Pattern(5, 7, 0x2A);
        GlyphSet padded = Render(Paint(small, 1, _ => tiny), "small.png", small, FontCellSize.Cell6x8, Codes(0, 0));
        AssertSame(GlyphOps.Fit(tiny, FontCellSize.Cell6x8), padded.Get(0));
        Assert.False(padded.Get(0)![5, 0]);
        Assert.False(padded.Get(0)![0, 7]);
    }

    [Theory]
    [InlineData(127, 128, true)]
    [InlineData(127, 127, false)]
    [InlineData(76, 77, true)]
    [InlineData(76, 76, false)]
    public void Threshold_is_strictly_less_than(int luma, int threshold, bool active)
    {
        var options = new SheetOptions { CellWidth = 1, CellHeight = 1, CharsPerRow = 1, Threshold = threshold };
        var sheet = new RgbaImage(1, 1);
        if (luma == 76)
        {
            sheet.SetPixel(0, 0, 255, 0, 0, 255);
        }
        else
        {
            sheet.SetPixel(0, 0, (byte)luma, (byte)luma, (byte)luma, 255);
        }

        MonoBitmap glyph = Render(sheet, "y.png", options, FontCellSize.Cell6x8, Codes(0, 0)).Get(0)!;
        Assert.Equal(active, glyph[0, 0]);
        Assert.False(glyph[1, 0]);
    }

    [Fact]
    public void Alpha_is_composited_onto_white()
    {
        var options = new SheetOptions { CellWidth = 3, CellHeight = 1, CharsPerRow = 1, Threshold = 128 };
        var sheet = new RgbaImage(3, 1, 255, 255, 255, 255);
        sheet.SetPixel(0, 0, 0, 0, 0, 0);
        sheet.SetPixel(1, 0, 0, 0, 0, 128);
        sheet.SetPixel(2, 0, 0, 0, 0, 255);
        MonoBitmap glyph = Render(sheet, "alpha.png", options, FontCellSize.Cell6x8, Codes(0, 0)).Get(0)!;

        Assert.False(glyph[0, 0]);
        Assert.True(glyph[1, 0]);
        Assert.True(glyph[2, 0]);
    }

    [Fact]
    public void Only_selected_ranges_are_filled_and_0x98_only_when_explicit()
    {
        var options = new SheetOptions { CellWidth = 6, CellHeight = 8, CharsPerRow = 16 };
        RgbaImage sheet = Paint(options, 256, code => Pattern(6, 8, code));
        GlyphSet defaults = Render(sheet, "ranges.png", options, FontCellSize.Cell6x8, CharRangeSet.Default);

        Assert.True(defaults.Contains(0x41));
        Assert.True(defaults.Contains(0xC0));
        Assert.False(defaults.Contains(0x00));
        Assert.False(defaults.Contains(0x98));
        Assert.False(defaults.Contains(0x7F));

        var explicitCode = new CharRangeSet(CharRangePreset.None, new[] { 0x98, 0x01 });
        GlyphSet chosen = Render(sheet, "ranges.png", options, FontCellSize.Cell6x8, explicitCode);
        AssertSame(Pattern(6, 8, 0x98), chosen.Get(0x98));
        AssertSame(Pattern(6, 8, 0x01), chosen.Get(0x01));
        Assert.Equal(2, chosen.Count);
    }

    [Fact]
    public void Sheet_smaller_than_the_selection_warns_once()
    {
        var options = new SheetOptions { CellWidth = 6, CellHeight = 8, MarginX = 2, MarginY = 1, SpacingX = 3, SpacingY = 0, CharsPerRow = 16 };
        var sheet = new RgbaImage(options.MarginX + options.CellWidth, options.MarginY + options.CellHeight, 255, 255, 255, 255);
        sheet.SetPixel(options.MarginX, options.MarginY, 0, 0, 0, 255);
        SheetGlyphSource source = Source(sheet, "small.png", options);
        GlyphSourceResult result = source.Render(FontCellSize.Cell6x8, AllCodes());

        Assert.True(result.Glyphs.Contains(0x00));
        Assert.True(result.Glyphs.Get(0x00)![0, 0]);
        Assert.False(result.Glyphs.Contains(0x01));
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.SheetTooSmall, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(new[] { "1", "0x01" }, warning.Arguments);
        Assert.Empty(result.MissingCodes);
    }

    [Fact]
    public void A_later_row_still_counts_when_an_earlier_column_does_not_fit()
    {
        var options = new SheetOptions { CellWidth = 4, CellHeight = 4, SpacingX = 1, SpacingY = 1, CharsPerRow = 16 };
        int width = options.CellWidth;
        int height = (options.CellHeight * 2) + options.SpacingY;
        var sheet = new RgbaImage(width, height, 255, 255, 255, 255);
        sheet.SetPixel(0, 0, 0, 0, 0, 255);
        sheet.SetPixel(0, options.CellHeight + options.SpacingY, 0, 0, 0, 255);
        GlyphSourceResult result = Source(sheet, "wrap.png", options).Render(FontCellSize.Cell6x8, AllCodes());

        Assert.True(result.Glyphs.Contains(0x00));
        Assert.True(result.Glyphs.Contains(0x10));
        Assert.False(result.Glyphs.Contains(0x01));
        Assert.Equal(new[] { "2", "0x01" }, Assert.Single(result.Diagnostics).Arguments);
    }

    [Fact]
    public void Pixels_of_a_cell_past_the_image_edge_are_background()
    {
        var options = new SheetOptions { CellWidth = 6, CellHeight = 8, MarginX = 0, MarginY = 0, CharsPerRow = 1 };
        var sheet = new RgbaImage(1, 1, 0, 0, 0, 255);
        MonoBitmap glyph = Render(sheet, "edge.png", options, FontCellSize.Cell6x8, Codes(0, 0)).Get(0)!;

        Assert.True(glyph[0, 0]);
        Assert.False(glyph[1, 0]);
        Assert.False(glyph[0, 1]);
        Assert.Empty(Source(sheet, "edge.png", options).Render(FontCellSize.Cell6x8, Codes(0, 0)).Diagnostics);
    }

    [Fact]
    public void Ink_in_the_margin_or_spacing_is_not_part_of_a_cell()
    {
        var options = new SheetOptions { CellWidth = 6, CellHeight = 8, MarginX = 2, MarginY = 2, SpacingX = 3, SpacingY = 0, CharsPerRow = 2 };
        RgbaImage sheet = Paint(options, 2, code => code == 0 ? Pattern(6, 8, 1) : new MonoBitmap(6, 8));
        sheet.SetPixel(0, 0, 0, 0, 0, 255);
        sheet.SetPixel(options.MarginX + options.CellWidth, options.MarginY, 0, 0, 0, 255);
        GlyphSet glyphs = Render(sheet, "gap.png", options, FontCellSize.Cell6x8, Codes(0, 1));

        AssertSame(Pattern(6, 8, 1), glyphs.Get(0));
        AssertSame(new MonoBitmap(6, 8), glyphs.Get(1));
    }

    [Theory]
    [InlineData(0, 8, 0, 0, 0, 0, 16, 0, 128)]
    [InlineData(65, 8, 0, 0, 0, 0, 16, 0, 128)]
    [InlineData(6, 0, 0, 0, 0, 0, 16, 0, 128)]
    [InlineData(6, 65, 0, 0, 0, 0, 16, 0, 128)]
    [InlineData(6, 8, -1, 0, 0, 0, 16, 0, 128)]
    [InlineData(6, 8, 1025, 0, 0, 0, 16, 0, 128)]
    [InlineData(6, 8, 0, -1, 0, 0, 16, 0, 128)]
    [InlineData(6, 8, 0, 1025, 0, 0, 16, 0, 128)]
    [InlineData(6, 8, 0, 0, -1, 0, 16, 0, 128)]
    [InlineData(6, 8, 0, 0, 257, 0, 16, 0, 128)]
    [InlineData(6, 8, 0, 0, 0, -1, 16, 0, 128)]
    [InlineData(6, 8, 0, 0, 0, 257, 16, 0, 128)]
    [InlineData(6, 8, 0, 0, 0, 0, 0, 0, 128)]
    [InlineData(6, 8, 0, 0, 0, 0, 257, 0, 128)]
    [InlineData(6, 8, 0, 0, 0, 0, 16, -1, 128)]
    [InlineData(6, 8, 0, 0, 0, 0, 16, 256, 128)]
    [InlineData(6, 8, 0, 0, 0, 0, 16, 0, -1)]
    [InlineData(6, 8, 0, 0, 0, 0, 16, 0, 256)]
    public void Invalid_options_throw(int cellWidth, int cellHeight, int marginX, int marginY, int spacingX, int spacingY, int charsPerRow, int firstCode, int threshold)
    {
        var options = new SheetOptions
        {
            CellWidth = cellWidth,
            CellHeight = cellHeight,
            MarginX = marginX,
            MarginY = marginY,
            SpacingX = spacingX,
            SpacingY = spacingY,
            CharsPerRow = charsPerRow,
            FirstCode = firstCode,
            Threshold = threshold,
        };
        Assert.Throws<ArgumentOutOfRangeException>(() => Source(new RgbaImage(1, 1), "a.png", options));
    }

    [Fact]
    public void Null_arguments_and_blank_file_name_throw()
    {
        var image = new RgbaImage(1, 1);
        var options = new SheetOptions();
        Assert.Throws<ArgumentNullException>(() => new SheetGlyphSource(null!, "a.png", options));
        Assert.Throws<ArgumentNullException>(() => new SheetGlyphSource(image, "a.png", null!));
        Assert.Throws<ArgumentException>(() => new SheetGlyphSource(image, "  ", options));
        var source = new SheetGlyphSource(image, "a.png", options);
        Assert.Throws<ArgumentNullException>(() => source.Render(null!, CharRangeSet.Default));
        Assert.Throws<ArgumentNullException>(() => source.Render(FontCellSize.Cell6x8, null!));
    }

    [Fact]
    public void Info_keeps_only_the_file_name()
    {
        SheetGlyphSource source = Source(new RgbaImage(1, 1), @"D:\fonts\sheet.png", new SheetOptions());
        Assert.Equal(FontSourceKind.Sheet, source.Info.Kind);
        Assert.Equal("sheet.png", source.Info.FileName);
        Assert.Null(source.Info.ArrayName);
    }

    [Fact]
    public void Rendering_is_deterministic_and_does_not_change_the_image()
    {
        var options = new SheetOptions { MarginX = 1, SpacingX = 1, SpacingY = 1 };
        RgbaImage sheet = Paint(options, 32, code => Pattern(6, 8, code));
        RgbaImage before = sheet.Clone();
        SheetGlyphSource source = Source(sheet, "same.png", options);
        GlyphSourceResult first = source.Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        GlyphSourceResult second = source.Render(FontCellSize.Cell6x8, CharRangeSet.Default);

        Assert.True(before.ContentEquals(sheet));
        Assert.Equal(first.Glyphs.Codes, second.Glyphs.Codes);
        foreach (int code in first.Glyphs.Codes)
        {
            AssertSame(first.Glyphs.Get(code)!, second.Glyphs.Get(code));
        }
    }

    [Fact]
    public void Replacing_the_source_keeps_manual_edits()
    {
        var options = new SheetOptions();
        FontTable table = new(FontCellSize.Cell6x8);
        table.ReplaceSource(Render(Paint(options, 1, _ => Pattern(6, 8, 1)), "a.png", options, FontCellSize.Cell6x8, Codes(0, 0)));
        MonoBitmap edited = Pattern(6, 8, 9);
        table.SetManual(0x00, edited);
        table.ReplaceSource(Render(Paint(options, 1, _ => Pattern(6, 8, 2)), "b.png", options, FontCellSize.Cell6x8, Codes(0, 0)));

        Assert.True(table.IsManual(0x00));
        AssertSame(edited, table.GetGlyph(0x00));
        AssertSame(Pattern(6, 8, 2), table.GetSourceGlyph(0x00));
    }

    private static SheetGlyphSource Source(RgbaImage sheet, string fileName, SheetOptions options) => new(sheet, fileName, options);

    private static GlyphSet Render(RgbaImage sheet, string fileName, SheetOptions options, FontCellSize cell, CharRangeSet ranges) =>
        Source(sheet, fileName, options).Render(cell, ranges).Glyphs;

    private static CharRangeSet AllCodes() => new(CharRangePreset.None, Enumerable.Range(0, 256));

    private static CharRangeSet Codes(int from, int to) => new(CharRangePreset.None, Enumerable.Range(from, to - from + 1));

    private static MonoBitmap Pattern(int width, int height, int code)
    {
        var bitmap = new MonoBitmap(width, height);
        for (int i = 0; i < 16; i++)
        {
            int x = i % width;
            int y = i / width;
            if (y < height)
            {
                bitmap[x, y] = ((code >> (i % 8)) & 1) != 0;
            }
        }

        bitmap[width - 1, height - 1] = (code & 1) == 0;
        return bitmap;
    }

    private static RgbaImage Paint(SheetOptions options, int count, Func<int, MonoBitmap> glyph)
    {
        int rows = Math.Max(1, (count + options.CharsPerRow - 1) / options.CharsPerRow);
        int width = Math.Max(1, options.MarginX + (options.CharsPerRow * options.CellWidth) + (Math.Max(0, options.CharsPerRow - 1) * options.SpacingX));
        int height = Math.Max(1, options.MarginY + (rows * options.CellHeight) + (Math.Max(0, rows - 1) * options.SpacingY));
        var image = new RgbaImage(width, height, 255, 255, 255, 255);
        for (int k = 0; k < count; k++)
        {
            MonoBitmap cell = glyph(k);
            int col = k % options.CharsPerRow;
            int row = k / options.CharsPerRow;
            int x0 = options.MarginX + (col * (options.CellWidth + options.SpacingX));
            int y0 = options.MarginY + (row * (options.CellHeight + options.SpacingY));
            int copyWidth = Math.Min(cell.Width, options.CellWidth);
            int copyHeight = Math.Min(cell.Height, options.CellHeight);
            for (int y = 0; y < copyHeight; y++)
            {
                for (int x = 0; x < copyWidth; x++)
                {
                    if (cell[x, y])
                    {
                        image.SetPixel(x0 + x, y0 + y, 0, 0, 0, 255);
                    }
                }
            }
        }

        return image;
    }

    private static void AssertSame(MonoBitmap expected, MonoBitmap? actual)
    {
        Assert.NotNull(actual);
        Assert.True(expected.ContentEquals(actual), "Glyph pixels differ.");
    }
}
