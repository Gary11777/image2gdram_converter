using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Tests.Packing;
using Image2Gdram.Reference;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Таблица 256 символов и её упаковка (п. 4.2.1 ТЗ, решения F-04, D-14, N-27, N-46).</summary>
public class FontTableTests
{
    private static readonly Mono1bppPacker Packer = new();

    private static readonly PackingOptions Vertical = PackingOptions.Default;

    private static readonly PackingOptions Horizontal8 = new() { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst, BitsPerByte = 8 };

    private static readonly PackingOptions Horizontal6 = Horizontal8 with { BitsPerByte = 6 };

    public static IEnumerable<object[]> CellsAndCombinations() =>
        from cell in FontCellSize.All
        from combination in TestBitmaps.CombinationIndexes
        select new object[] { cell.Width, cell.Height, combination };

    [Theory]
    [InlineData(6, 8, 6, 8, 8)]
    [InlineData(8, 8, 8, 8, 16)]
    [InlineData(12, 16, 24, 32, 32)]
    public void Bytes_per_char_and_array_size_follow_table_4_2_1(int width, int height, int vertical, int horizontal8, int horizontal6)
    {
        var table = new FontTable(FontCellSize.Get(width, height));
        Assert.Equal(vertical, FontTable.GetBytesPerChar(table.Cell, Packer, Vertical));
        Assert.Equal(horizontal8, FontTable.GetBytesPerChar(table.Cell, Packer, Horizontal8));
        Assert.Equal(horizontal6, FontTable.GetBytesPerChar(table.Cell, Packer, Horizontal6));
        Assert.Equal(256 * vertical, table.Pack(Packer, Vertical).Length);
        Assert.Equal(256 * horizontal8, table.Pack(Packer, Horizontal8).Length);
        Assert.Equal(256 * horizontal6, table.Pack(Packer, Horizontal6).Length);
    }

    [Fact]
    public void Array_sizes_are_1536_and_6144_bytes_vertically()
    {
        Assert.Equal(1536, new FontTable(FontCellSize.Cell6x8).Pack(Packer, Vertical).Length);
        Assert.Equal(6144, new FontTable(FontCellSize.Cell12x16).Pack(Packer, Vertical).Length);
    }

    [Theory]
    [MemberData(nameof(CellsAndCombinations))]
    public void Empty_table_is_all_background(int width, int height, int combination)
    {
        RefOptions reference = RefOptions.AllCombinations[combination];
        byte[] data = new FontTable(FontCellSize.Get(width, height)).Pack(Packer, TestBitmaps.ToCore(reference));
        byte expected = reference.Invert ? (byte)0xFF : (byte)0x00;
        Assert.All(data, b => Assert.Equal(expected, b));
    }

    [Theory]
    [MemberData(nameof(CellsAndCombinations))]
    public void Table_matches_reference_packer_glyph_by_glyph(int width, int height, int combination)
    {
        FontCellSize cell = FontCellSize.Get(width, height);
        RefOptions reference = RefOptions.AllCombinations[combination];
        FontTable table = RandomTable(cell, seed: width * 1000 + height);
        byte[] data = table.Pack(Packer, TestBitmaps.ToCore(reference));
        int perChar = data.Length / 256;
        for (int code = 0; code < 256; code++)
        {
            byte[] expected = ReferencePacker.Pack(TestBitmaps.ToArray(table.GetGlyph(code)), reference);
            Assert.Equal(expected, data.AsSpan(code * perChar, perChar).ToArray());
        }
    }

    [Theory]
    [MemberData(nameof(CellsAndCombinations))]
    public void Every_glyph_round_trips_through_unpack(int width, int height, int combination)
    {
        FontCellSize cell = FontCellSize.Get(width, height);
        PackingOptions options = TestBitmaps.ToCore(RefOptions.AllCombinations[combination]);
        FontTable table = RandomTable(cell, seed: 7 + combination);
        byte[] data = table.Pack(Packer, options);
        int perChar = data.Length / 256;
        for (int code = 0; code < 256; code++)
        {
            MonoBitmap unpacked = Packer.Unpack(data.AsSpan(code * perChar, perChar), cell.Width, cell.Height, options);
            Assert.True(table.GetGlyph(code).ContentEquals(unpacked), $"code 0x{code:X2}");
        }
    }

    [Fact]
    public void Glyph_with_code_c_occupies_bytes_c_times_n()
    {
        var table = new FontTable(FontCellSize.Cell8x8);
        table.SetManual(0x41, TestBitmaps.SinglePixel(8, 8, 0, 0));
        table.SetManual(0xFF, TestBitmaps.SinglePixel(8, 8, 7, 7));
        byte[] data = table.Pack(Packer, Vertical);
        Assert.Equal(0x01, data[0x41 * 8]);
        Assert.Equal(0x80, data[0xFF * 8 + 7]);
        Assert.Equal(2, data.Count(b => b != 0));
    }

    [Fact]
    public void Font_6x8_in_6_bit_mode_has_padding_bits_set_by_inversion()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        table.SetManual(0x21, TestBitmaps.SinglePixel(6, 8, 0, 0));
        byte[] plain = table.Pack(Packer, Horizontal6);
        byte[] inverted = table.Pack(Packer, Horizontal6 with { Invert = true });

        Assert.Equal(2048, plain.Length);
        Assert.Equal(0x20, plain[0x21 * 8]);
        Assert.Equal(0xDF, inverted[0x21 * 8]);
        Assert.All(plain.Where((_, i) => i != 0x21 * 8), b => Assert.Equal(0x00, b));
        Assert.All(inverted.Where((_, i) => i != 0x21 * 8), b => Assert.Equal(0xFF, b));

        var full = new FontTable(FontCellSize.Cell6x8);
        full.SetManual(0, TestBitmaps.Filled(6, 8, true));
        Assert.All(full.Pack(Packer, Horizontal6).Take(8), b => Assert.Equal(0x3F, b));
        Assert.All(full.Pack(Packer, Horizontal6 with { Invert = true }).Take(8), b => Assert.Equal(0xC0, b));
    }

    [Fact]
    public void Unused_codes_stay_in_place_and_are_background()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        var glyphs = new GlyphSet(FontCellSize.Cell6x8);
        glyphs.Set(0x7E, TestBitmaps.Filled(6, 8, true));
        table.ReplaceSource(glyphs);
        byte[] inverted = table.Pack(Packer, Vertical with { Invert = true });
        Assert.All(inverted.AsSpan(0, 0x7E * 6).ToArray(), b => Assert.Equal(0xFF, b));
        Assert.All(inverted.AsSpan(0x7E * 6, 6).ToArray(), b => Assert.Equal(0x00, b));
        Assert.All(inverted.AsSpan(0x7F * 6).ToArray(), b => Assert.Equal(0xFF, b));
    }

    [Fact]
    public void Origin_and_emptiness_reflect_source_and_manual_edits()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        var glyphs = new GlyphSet(FontCellSize.Cell6x8);
        glyphs.Set(0x20, new MonoBitmap(6, 8));
        glyphs.Set(0x41, TestBitmaps.SinglePixel(6, 8, 1, 1));
        table.ReplaceSource(glyphs);
        table.SetManual(0x01, TestBitmaps.SinglePixel(6, 8, 2, 2));

        Assert.Equal(GlyphOrigin.None, table.GetOrigin(0x00));
        Assert.Equal(GlyphOrigin.Source, table.GetOrigin(0x20));
        Assert.Equal(GlyphOrigin.Source, table.GetOrigin(0x41));
        Assert.Equal(GlyphOrigin.Manual, table.GetOrigin(0x01));
        Assert.True(table.IsEmpty(0x00));
        Assert.True(table.IsEmpty(0x20));
        Assert.False(table.IsEmpty(0x41));
        Assert.False(table.IsEmpty(0x01));
        Assert.True(table.HasManualEdits);
        Assert.Equal(new[] { 0x01 }, table.ManualCodes);
    }

    [Fact]
    public void Regenerating_the_source_keeps_manual_edits()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        table.ReplaceSource(Set(FontCellSize.Cell6x8, (0x41, 1), (0x42, 2), (0x43, 3)));
        MonoBitmap manual = TestBitmaps.Filled(6, 8, true);
        table.SetManual(0x42, manual);
        table.SetManual(0x01, manual);

        table.ReplaceSource(Set(FontCellSize.Cell6x8, (0x41, 4), (0x42, 5)));

        Assert.True(table.GetGlyph(0x41).ContentEquals(Pattern(4)));
        Assert.True(table.GetGlyph(0x42).ContentEquals(manual));
        Assert.True(table.GetSourceGlyph(0x42)!.ContentEquals(Pattern(5)));
        Assert.Equal(GlyphOrigin.None, table.GetOrigin(0x43));
        Assert.True(table.IsEmpty(0x43));
        Assert.Equal(GlyphOrigin.Manual, table.GetOrigin(0x01));
        Assert.Equal(new[] { 0x01, 0x42 }, table.ManualCodes);
    }

    [Fact]
    public void Revert_to_source_and_reset_all_remove_manual_edits()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        table.ReplaceSource(Set(FontCellSize.Cell6x8, (0x41, 1)));
        table.SetManual(0x41, Pattern(9));
        table.SetManual(0x42, Pattern(8));
        table.SetManual(0x43, Pattern(7));

        table.RevertToSource(0x41);
        Assert.True(table.GetGlyph(0x41).ContentEquals(Pattern(1)));
        Assert.Equal(GlyphOrigin.Source, table.GetOrigin(0x41));

        table.ResetAllManual();
        Assert.False(table.HasManualEdits);
        Assert.Equal(GlyphOrigin.None, table.GetOrigin(0x42));
        Assert.True(table.IsEmpty(0x43));
    }

    [Fact]
    public void Returned_glyphs_are_copies()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        MonoBitmap manual = Pattern(3);
        table.SetManual(0x41, manual);
        manual[0, 0] = !manual[0, 0];
        Assert.False(table.GetGlyph(0x41).ContentEquals(manual));

        MonoBitmap copy = table.GetGlyph(0x41);
        copy[1, 1] = !copy[1, 1];
        Assert.True(table.GetGlyph(0x41).ContentEquals(Pattern(3)));

        var glyphs = new GlyphSet(FontCellSize.Cell6x8);
        MonoBitmap source = Pattern(5);
        glyphs.Set(0x30, source);
        source[2, 2] = !source[2, 2];
        Assert.True(glyphs.Get(0x30)!.ContentEquals(Pattern(5)));
    }

    [Fact]
    public void Clone_is_independent()
    {
        FontTable table = RandomTable(FontCellSize.Cell8x8, seed: 3);
        table.SetManual(0x10, Pattern(2, 8, 8));
        FontTable clone = table.Clone();
        Assert.Equal(table.Pack(Packer, Vertical), clone.Pack(Packer, Vertical));
        clone.ResetAllManual();
        clone.ReplaceSource(new GlyphSet(FontCellSize.Cell8x8));
        Assert.True(table.HasManualEdits);
        Assert.Equal(GlyphOrigin.Source, table.GetOrigin(0x42));
    }

    [Fact]
    public void Output_data_feeds_existing_generators()
    {
        FontTable table = RandomTable(FontCellSize.Cell12x16, seed: 11);
        PackingOptions options = Vertical with { Invert = true };
        FontSourceInfo source = FontSourceInfo.TrueType("Consolas", 16);
        PresetInfo preset = PresetInfo.Named("OLED128X64-0.96", "SSD1306");
        FontOutputData data = table.ToOutputData(Packer, options, source, preset);

        Assert.Equal(12, data.CellWidth);
        Assert.Equal(16, data.CellHeight);
        Assert.Equal(24, data.BytesPerChar);
        Assert.Equal(6144, data.TotalBytes);
        Assert.Same(options, data.Packing);
        Assert.Equal(source, data.Source);
        Assert.Equal(table.Pack(Packer, options), data.GetAllBytes());
        Assert.Equal(Packer.Pack(table.GetGlyph(0xC0), options), data.GetGlyph(0xC0).ToArray());

        OutputDocument document = OutputGeneratorRegistry.CreateDefault().Generate(
            data,
            new OutputOptions { Format = OutputFormat.CKeilC51, ArrayName = "font_12x16", IncludeDate = false });
        Assert.Contains("unsigned char code font_12x16[256][FONT_12X16_BYTES_PER_CHAR] = {", document.Files[0].Text);
        Assert.Equal(6144, document.ByteMap.Count);
    }

    [Fact]
    public void Invalid_arguments_are_rejected()
    {
        var table = new FontTable(FontCellSize.Cell6x8);
        Assert.Throws<ArgumentException>(() => table.SetManual(0x41, new MonoBitmap(8, 8)));
        Assert.Throws<ArgumentException>(() => table.ReplaceSource(new GlyphSet(FontCellSize.Cell8x8)));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.GetGlyph(256));
        Assert.Throws<ArgumentOutOfRangeException>(() => table.GetOrigin(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new GlyphSet(FontCellSize.Cell6x8).Set(300, new MonoBitmap(6, 8)));
        Assert.Throws<ArgumentException>(() => new GlyphSet(FontCellSize.Cell6x8).Set(1, new MonoBitmap(6, 7)));
    }

    [Fact]
    public void Only_three_cell_sizes_exist()
    {
        Assert.Equal(new[] { "6x8", "8x8", "12x16" }, FontCellSize.All.Select(c => c.ToString()));
        Assert.Same(FontCellSize.Cell12x16, FontCellSize.Get(12, 16));
        Assert.False(FontCellSize.TryGet(16, 16, out _));
        Assert.Throws<ArgumentOutOfRangeException>(() => FontCellSize.Get(5, 7));
    }

    internal static MonoBitmap Pattern(int seed, int width = 6, int height = 8) => TestBitmaps.Random(width, height, seed);

    private static GlyphSet Set(FontCellSize cell, params (int Code, int Seed)[] glyphs)
    {
        var set = new GlyphSet(cell);
        foreach ((int code, int seed) in glyphs)
        {
            set.Set(code, Pattern(seed, cell.Width, cell.Height));
        }

        return set;
    }

    private static FontTable RandomTable(FontCellSize cell, int seed)
    {
        var table = new FontTable(cell);
        var set = new GlyphSet(cell);
        for (int code = 0; code < 256; code++)
        {
            if (code % 5 != 0)
            {
                set.Set(code, TestBitmaps.Random(cell.Width, cell.Height, seed * 256 + code));
            }
        }

        table.ReplaceSource(set);
        for (int code = 3; code < 256; code += 17)
        {
            table.SetManual(code, TestBitmaps.Random(cell.Width, cell.Height, -seed - code));
        }

        return table;
    }
}
