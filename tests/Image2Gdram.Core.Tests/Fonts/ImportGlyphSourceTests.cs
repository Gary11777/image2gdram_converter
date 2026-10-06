using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Tests.Packing;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Раскладка импортированных байтов по таблице шрифта (п. 4.2.2, решения D-14, N-27).</summary>
public class ImportGlyphSourceTests
{
    private static readonly Mono1bppPacker Packer = new();

    public static IEnumerable<object[]> CellsAndCombinations() =>
        from cell in FontCellSize.All
        from combination in TestBitmaps.CombinationIndexes
        select new object[] { cell.Width, cell.Height, combination };

    [Theory]
    [MemberData(nameof(CellsAndCombinations))]
    public void Pack_then_import_restores_the_table(int width, int height, int combination)
    {
        FontCellSize cell = FontCellSize.Get(width, height);
        PackingOptions packing = TestBitmaps.ToCore(Image2Gdram.Reference.RefOptions.AllCombinations[combination]);
        FontTable table = RandomTable(cell, 41000 + (width * 100) + combination);
        byte[] packed = table.Pack(Packer, packing);
        GlyphSourceResult result = Import(packed, cell, packing, "font.c", "font_6x8");

        Assert.Empty(result.Diagnostics);
        AssertSame(table, result.Glyphs);
    }

    [Theory]
    [MemberData(nameof(CellsAndCombinations))]
    public void Fewer_values_pad_the_last_glyph_and_warn(int width, int height, int combination)
    {
        FontCellSize cell = FontCellSize.Get(width, height);
        PackingOptions packing = TestBitmaps.ToCore(Image2Gdram.Reference.RefOptions.AllCombinations[combination]);
        FontTable table = RandomTable(cell, 51000 + combination + width);
        byte[] packed = table.Pack(Packer, packing);
        int bytesPerChar = packed.Length / FontTable.CharCount;
        int available = bytesPerChar + 1;
        GlyphSourceResult result = Import(packed[..available], cell, packing, @"D:\io\font.a51", "glyph");

        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ImportValueCountMismatch, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(new[] { packed.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), available.ToString(System.Globalization.CultureInfo.InvariantCulture) }, warning.Arguments);
        AssertPartial(table, packing, packed, available, result.Glyphs);
        var info = new ImportGlyphSource(ArrayOf(packed[..available], "glyph"), @"D:\io\font.a51", Packer, packing).Info;
        Assert.Equal("font.a51", info.FileName);
        Assert.Equal("glyph", info.ArrayName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Fewer_values_that_are_not_a_multiple_of_the_glyph_use_background(bool invert)
    {
        var packing = new PackingOptions { Direction = PackDirection.Vertical, BitOrder = BitOrder.LsbFirst, Invert = invert };
        FontTable table = new(FontCellSize.Cell6x8);
        MonoBitmap glyph = new(6, 8);
        glyph[0, 0] = true;
        glyph[5, 7] = true;
        var set = new GlyphSet(FontCellSize.Cell6x8);
        set.Set(0, glyph);
        set.Set(1, glyph);
        table.ReplaceSource(set);
        byte[] packed = table.Pack(Packer, packing);
        int bytesPerChar = packed.Length / 256;
        int available = bytesPerChar + 3;
        GlyphSet glyphs = Import(packed[..available], FontCellSize.Cell6x8, packing, "font.c", null).Glyphs;

        Assert.True(glyphs.Contains(0));
        Assert.True(glyphs.Contains(1));
        Assert.False(glyphs.Contains(2));
        AssertSameGlyph(table.GetGlyph(0), glyphs.Get(0));
        byte[] partial = new byte[bytesPerChar];
        Array.Fill(partial, (byte)(invert ? 0xFF : 0x00));
        packed.AsSpan(bytesPerChar, 3).CopyTo(partial);
        AssertSameGlyph(Packer.Unpack(partial, 6, 8, packing), glyphs.Get(1));
        Assert.False(glyphs.Get(1)![5, 7]);
    }

    [Fact]
    public void Extra_values_are_dropped_with_one_warning()
    {
        var packing = PackingOptions.Default;
        FontTable table = RandomTable(FontCellSize.Cell8x8, 7);
        byte[] packed = table.Pack(Packer, packing);
        byte[] longer = new byte[packed.Length + 5];
        packed.CopyTo(longer, 0);
        GlyphSourceResult result = Import(longer, FontCellSize.Cell8x8, packing, "font.h", "image");

        AssertSame(table, result.Glyphs);
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(new[] { packed.Length.ToString(System.Globalization.CultureInfo.InvariantCulture), longer.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) }, warning.Arguments);
    }

    [Fact]
    public void Ranges_do_not_limit_an_import()
    {
        var packing = PackingOptions.Default;
        FontTable table = RandomTable(FontCellSize.Cell6x8, 11);
        var source = new ImportGlyphSource(ArrayOf(table.Pack(Packer, packing), "font"), "font.c", Packer, packing);
        GlyphSet glyphs = source.Render(FontCellSize.Cell6x8, new CharRangeSet(CharRangePreset.None, new[] { 0 })).Glyphs;
        Assert.Equal(256, glyphs.Count);
        AssertSame(table, glyphs);
    }

    [Fact]
    public void An_array_with_an_error_cannot_be_imported()
    {
        var broken = new ImportedArray("font", ImportedArrayKind.CInitializer, 4, Array.Empty<byte>(), new ArrayImportIssue(ArrayImportErrorKind.ValueOutOfRange, 4, "300"));
        Assert.Throws<ArgumentException>(() => new ImportGlyphSource(broken, "font.c", Packer, PackingOptions.Default));
    }

    [Fact]
    public void Reset_and_replace_discards_manual_edits()
    {
        var packing = PackingOptions.Default;
        FontTable original = RandomTable(FontCellSize.Cell8x8, 19);
        byte[] packed = original.Pack(Packer, packing);
        FontTable table = new(FontCellSize.Cell8x8);
        table.SetManual(0x41, TestBitmaps.SinglePixel(8, 8, 1, 1));
        Assert.True(table.HasManualEdits);

        GlyphSourceResult imported = Import(packed, FontCellSize.Cell8x8, packing, "in.c", "font_8x8");
        table.ResetAllManual();
        table.ReplaceSource(imported.Glyphs);

        Assert.False(table.HasManualEdits);
        Assert.Empty(table.ManualCodes);
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            Assert.Equal(GlyphOrigin.Source, table.GetOrigin(code));
            AssertSameGlyph(original.GetGlyph(code), table.GetGlyph(code));
        }
    }

    private static GlyphSourceResult Import(byte[] values, FontCellSize cell, PackingOptions packing, string fileName, string? arrayName)
    {
        var source = new ImportGlyphSource(ArrayOf(values, arrayName), fileName, Packer, packing);
        Assert.Equal(FontSourceKind.Import, source.Info.Kind);
        return source.Render(cell, CharRangeSet.Default);
    }

    private static ImportedArray ArrayOf(byte[] values, string? name) =>
        new(name, ImportedArrayKind.CInitializer, 1, values, null);

    private static void AssertPartial(FontTable table, PackingOptions packing, byte[] packed, int available, GlyphSet glyphs)
    {
        int bytesPerChar = packed.Length / FontTable.CharCount;
        byte pad = (byte)(packing.Invert ? 0xFF : 0x00);
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            int offset = code * bytesPerChar;
            if (offset >= available)
            {
                Assert.False(glyphs.Contains(code));
                continue;
            }

            Assert.True(glyphs.Contains(code));
            if (offset + bytesPerChar <= available)
            {
                AssertSameGlyph(table.GetGlyph(code), glyphs.Get(code));
                continue;
            }

            var partial = new byte[bytesPerChar];
            Array.Fill(partial, pad);
            packed.AsSpan(offset, available - offset).CopyTo(partial);
            AssertSameGlyph(Packer.Unpack(partial, table.Cell.Width, table.Cell.Height, packing), glyphs.Get(code));
        }
    }

    private static FontTable RandomTable(FontCellSize cell, int seed)
    {
        var random = new Random(seed);
        var set = new GlyphSet(cell);
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            var bitmap = new MonoBitmap(cell.Width, cell.Height);
            for (int y = 0; y < cell.Height; y++)
            {
                for (int x = 0; x < cell.Width; x++)
                {
                    bitmap[x, y] = random.Next(2) == 0;
                }
            }

            set.Set(code, bitmap);
        }

        var table = new FontTable(cell);
        table.ReplaceSource(set);
        return table;
    }

    private static void AssertSame(FontTable table, GlyphSet glyphs)
    {
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            Assert.True(glyphs.Contains(code));
            AssertSameGlyph(table.GetGlyph(code), glyphs.Get(code));
        }
    }

    private static void AssertSameGlyph(MonoBitmap expected, MonoBitmap? actual)
    {
        Assert.NotNull(actual);
        Assert.True(expected.ContentEquals(actual));
    }
}
