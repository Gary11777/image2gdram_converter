using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>
/// Лист и импорт проходят через таблицу первой части в генераторы этапа 3 (решения D-02, D-03, D-14).
/// </summary>
public class FontSourceIntegrationTests
{
    private static readonly Mono1bppPacker Packer = new();
    private static readonly OutputGeneratorRegistry Generators = OutputGeneratorRegistry.CreateDefault();
    private static readonly PresetInfo Preset = PresetInfo.Named("OLED128X64-0.96", "SSD1306");

    [Fact]
    public void Sheet_of_6x8_matches_appendix_v_formatting()
    {
        var packing = PackingOptions.Default;
        byte[] glyphBytes = { 0x7C, 0x12, 0x11, 0x12, 0x7C, 0x00 };
        MonoBitmap glyph = Packer.Unpack(glyphBytes, 6, 8, packing);
        var options = new SheetOptions { CellWidth = 6, CellHeight = 8, MarginX = 2, MarginY = 3, SpacingX = 1, SpacingY = 4, CharsPerRow = 16, FirstCode = 0xC0 };
        var sheet = new RgbaImage(options.MarginX + 6, options.MarginY + 8, 255, 255, 255, 255);
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                if (glyph[x, y])
                {
                    sheet.SetPixel(options.MarginX + x, options.MarginY + y, 0, 0, 0, 255);
                }
            }
        }

        var table = new FontTable(FontCellSize.Cell6x8);
        GlyphSourceResult rendered = new SheetGlyphSource(sheet, @"C:\fonts\font.png", options)
            .Render(FontCellSize.Cell6x8, new CharRangeSet(CharRangePreset.None, new[] { 0xC0 }));
        Assert.Empty(rendered.Diagnostics);
        table.ReplaceSource(rendered.Glyphs);
        FontOutputData data = table.ToOutputData(Packer, packing, rendered.Glyphs.Count == 1 ? new SheetGlyphSource(sheet, "font.png", options).Info : throw new InvalidOperationException(), Preset);

        string c = Generate(data, OutputFormat.CKeilC51, "font_6x8").Files[0].Text;
        string module = Generate(data, OutputFormat.A51Module, "font_6x8").Files[0].Text;
        string include = Generate(data, OutputFormat.A51Include, "font_6x8").Files[0].Text;

        foreach (string text in new[] { c, module, include })
        {
            Assert.Contains("Image2GDRAM Converter 1.0", text, StringComparison.Ordinal);
            Assert.Contains("6x8", text, StringComparison.Ordinal);
            Assert.Contains("\u043B\u0438\u0441\u0442 font.png", text, StringComparison.Ordinal);
            Assert.DoesNotContain("Converter_image_IU", text, StringComparison.Ordinal);
            Assert.DoesNotContain("\u00D7", text, StringComparison.Ordinal);
        }

        Assert.Contains("    { 0x7C, 0x12, 0x11, 0x12, 0x7C, 0x00 }, /* 0xC0 '\u0410' */", c, StringComparison.Ordinal);
        Assert.Contains("unsigned char code font_6x8[256][FONT_6X8_BYTES_PER_CHAR] = {", c, StringComparison.Ordinal);
        const string db = "                DB      07Ch, 012h, 011h, 012h, 07Ch, 000h  ; 0C0h '\u0410'";
        Assert.Contains("FONT_6X8:", module, StringComparison.Ordinal);
        Assert.Contains("PUBLIC", module, StringComparison.Ordinal);
        Assert.Contains("SEGMENT", module, StringComparison.Ordinal);
        Assert.Contains(db, module, StringComparison.Ordinal);
        Assert.Contains("END", module, StringComparison.Ordinal);
        Assert.Contains("FONT_6X8:", include, StringComparison.Ordinal);
        Assert.Contains(db, include, StringComparison.Ordinal);
        Assert.DoesNotContain("PUBLIC", include, StringComparison.Ordinal);
        Assert.DoesNotContain("SEGMENT", include, StringComparison.Ordinal);
        Assert.DoesNotContain("END", include, StringComparison.Ordinal);

        ImportedArray parsed = Assert.Single(ArrayImportParser.Parse(c, ImportSyntax.C));
        Assert.Equal("font_6x8", parsed.Name);
        Assert.Equal(table.Pack(Packer, packing), parsed.Values);
    }

    [Fact]
    public void Generated_font_text_imports_back_to_the_same_table()
    {
        foreach (FontCellSize cell in FontCellSize.All)
        {
            PackingOptions packing = Packing(cell);
            FontTable table = RandomTable(cell, 62000 + cell.Width);
            byte[] packed = table.Pack(Packer, packing);
            string name = "font_" + cell;
            FontOutputData data = table.ToOutputData(Packer, packing, FontSourceInfo.Manual, Preset);
            foreach (OutputFormat format in new[] { OutputFormat.CKeilC51, OutputFormat.CStm32, OutputFormat.A51Module, OutputFormat.A51Include })
            {
                foreach (OutputEncoding encoding in new[] { OutputEncoding.Cp1251, OutputEncoding.Utf8NoBom })
                {
                    foreach (bool includeDate in new[] { false, true })
                    {
                        AsmNumberFormat[] numbers = format is OutputFormat.A51Module or OutputFormat.A51Include
                            ? new[] { AsmNumberFormat.Hex, AsmNumberFormat.Binary }
                            : new[] { AsmNumberFormat.Hex };
                        foreach (AsmNumberFormat numberFormat in numbers)
                        {
                            OutputDocument document = Generators.Generate(data, new OutputOptions
                            {
                                Format = format,
                                ArrayName = name,
                                Encoding = encoding,
                                IncludeDate = includeDate,
                                GeneratedAt = new DateTime(2026, 10, 6, 21, 0, 0),
                                AsmNumberFormat = numberFormat,
                            });
                            OutputFile file = document.Files[0];
                            DecodedText decoded = TextFileReader.Decode(file.Content);
                            ImportSyntax syntax = format is OutputFormat.A51Module or OutputFormat.A51Include ? ImportSyntax.Asm : ImportSyntax.C;
                            ImportedArray array = Assert.Single(ArrayImportParser.Parse(decoded.Text, syntax));
                            Assert.Null(array.Error);
                            Assert.Equal(syntax == ImportSyntax.Asm ? name.ToUpperInvariant() : name, array.Name);
                            Assert.Equal<byte>(packed, array.Values);

                            var imported = new ImportGlyphSource(array, file.FileName, Packer, packing);
                            GlyphSet glyphs = imported.Render(cell, new CharRangeSet(CharRangePreset.None)).Glyphs;
                            for (int code = 0; code < FontTable.CharCount; code++)
                            {
                                Assert.True(table.GetGlyph(code).ContentEquals(glyphs.Get(code)), $"{format} {encoding} {numberFormat} date={includeDate} code {code}");
                            }
                        }
                    }
                }
            }
        }
    }

    private static OutputDocument Generate(FontOutputData data, OutputFormat format, string name) =>
        Generators.Generate(data, new OutputOptions
        {
            Format = format,
            ArrayName = name,
            IncludeDate = false,
            Encoding = OutputEncoding.Utf8NoBom,
        });

    private static PackingOptions Packing(FontCellSize cell) => cell.Width switch
    {
        6 => PackingOptions.Default,
        8 => new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst, BitsPerByte = 8, Invert = true },
        _ => new PackingOptions { Direction = PackDirection.Vertical, BitOrder = BitOrder.MsbFirst, PageTraversal = PageTraversal.ByColumns, Invert = true },
    };

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
}
