using System.Globalization;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using static Image2Gdram.Core.Tests.Output.OutputTestData;

namespace Image2Gdram.Core.Tests.Output;

/// <summary>
/// Золотые тесты C по примерам В.1 и В.4 приложения В с поправками D-02 (название), D-03 (<c>x</c> вместо знака умножения),
/// N-02 (единый заголовок с пресетом) и K-06 (заголовок-комментарий и в <c>.h</c>). Дата отключена.
/// </summary>
public class CGeneratorGoldenTests
{
    private static readonly string[] LogoHeader =
    {
        "/*",
        " * Image2GDRAM Converter 1.0",
        " * Источник: logo.png; пресет: OLED128X64-0.96 (SSD1306)",
        " * Размер: 128x64 (ШxВ); упаковка: вертикальная, LSB first, по страницам, инверсия: нет",
        " * Размер массива: 1024 байта",
        " */",
    };

    private static readonly string[] Font12x16Header =
    {
        "/*",
        " * Image2GDRAM Converter 1.0",
        " * Шрифт: Consolas, 16 px; ячейка 12x16 (ШxВ)",
        " * Упаковка: вертикальная, LSB first, по страницам, инверсия: нет; пресет: OLED128X64-0.96 (SSD1306)",
        " * Размер: 256 символов x 24 байта = 6144 байта",
        " */",
    };

    [Fact]
    public void Stm32_image_128x64_matches_appendix_v4()
    {
        OutputDocument document = Generate(Logo128x64(), OutputFormat.CStm32, "logo_128x64");

        Assert.Equal(new[] { "logo_128x64.c", "logo_128x64.h" }, document.Files.Select(f => f.FileName));
        Assert.Equal(
            Join(LogoHeader.Concat(new[]
            {
                "",
                "#ifndef LOGO_128X64_H",
                "#define LOGO_128X64_H",
                "",
                "#include <stdint.h>",
                "",
                "#define LOGO_128X64_WIDTH   128",
                "#define LOGO_128X64_HEIGHT  64",
                "#define LOGO_128X64_SIZE    1024",
                "",
                "extern const uint8_t logo_128x64[LOGO_128X64_SIZE];",
                "",
                "#endif",
            }).ToArray()),
            document.Files[1].Text);

        var expected = new List<string>(LogoHeader)
        {
            "#include \"logo_128x64.h\"",
            "",
            "const uint8_t logo_128x64[LOGO_128X64_SIZE] = {",
            "    0xFF, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,",
        };
        string zeros = string.Join(", ", Enumerable.Repeat("0x00", 16));
        expected.AddRange(Enumerable.Repeat("    " + zeros + ",", 62));
        expected.Add("    " + zeros);
        expected.Add("};");
        Assert.Equal(Join(expected.ToArray()), document.Files[0].Text);
    }

    [Fact]
    public void Stm32_unsigned_char_variant_has_no_stdint()
    {
        OutputOptions options = Options(OutputFormat.CStm32, "logo_128x64") with { Stm32ElementType = Stm32ElementType.UnsignedChar };

        OutputDocument document = Registry.Generate(Logo128x64(), options);

        Assert.DoesNotContain("stdint", document.Files[1].Text);
        Assert.Contains("extern const unsigned char logo_128x64[LOGO_128X64_SIZE];\r\n", document.Files[1].Text);
        Assert.Contains("\r\nconst unsigned char logo_128x64[LOGO_128X64_SIZE] = {\r\n", document.Files[0].Text);
    }

    [Fact]
    public void C51_font_12x16_matches_appendix_v1()
    {
        var data = new byte[256 * 24];
        FontOutputData font = Font(12, 16, data: data, source: FontSourceInfo.TrueType("Consolas", 16));

        OutputDocument document = Generate(font, OutputFormat.CKeilC51, "font_12x16");

        Assert.Equal(
            Join(Font12x16Header.Concat(new[]
            {
                "",
                "#ifndef FONT_12X16_H",
                "#define FONT_12X16_H",
                "",
                "#define FONT_12X16_CHAR_WIDTH      12",
                "#define FONT_12X16_CHAR_HEIGHT     16",
                "#define FONT_12X16_BYTES_PER_CHAR  24",
                "",
                "extern unsigned char code font_12x16[256][FONT_12X16_BYTES_PER_CHAR];",
                "",
                "#endif",
            }).ToArray()),
            document.Files[1].Text);

        string half = string.Join(", ", Enumerable.Repeat("0x00", 12));
        var expected = new List<string>(Font12x16Header)
        {
            "#include \"font_12x16.h\"",
            "",
            "unsigned char code font_12x16[256][FONT_12X16_BYTES_PER_CHAR] = {",
        };
        for (int code = 0; code < 256; code++)
        {
            expected.Add("    { " + half + ",");
            expected.Add("      " + half + " }" + (code < 255 ? "," : string.Empty) + " " + GlyphComment(code));
        }

        expected.Add("};");
        Assert.Equal(Join(expected.ToArray()), document.Files[0].Text);

        string[] lines = document.Files[0].Text.Split("\r\n");
        Assert.Equal("    { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,", lines[9]);
        Assert.Equal("      0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, /* 0x00 */", lines[10]);
        Assert.EndsWith(" }, /* 0xC0 'А' */", lines[10 + 2 * 0xC0]);
        Assert.EndsWith(" } /* 0xFF 'я' */", lines[10 + 2 * 0xFF]);
    }

    [Fact]
    public void Font_6x8_glyph_fits_one_line()
    {
        var data = new byte[256 * 6];
        new byte[] { 0x7C, 0x12, 0x11, 0x12, 0x7C, 0x00 }.CopyTo(data, 0xC0 * 6);

        string text = Generate(Font(6, 8, data: data), OutputFormat.CKeilC51, "font_6x8").Files[0].Text;

        Assert.Contains("\r\n    { 0x7C, 0x12, 0x11, 0x12, 0x7C, 0x00 }, /* 0xC0 'А' */\r\n", text);
        Assert.Contains("\r\n    { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, /* 0x20 ' ' */\r\n", text);
        Assert.Contains("\r\n    { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 } /* 0xFF 'я' */\r\n};\r\n", text);
    }

    [Fact]
    public void Font_with_32_bytes_per_char_takes_two_lines_of_16()
    {
        FontOutputData font = Font(12, 16, new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst });
        Assert.Equal(32, font.BytesPerChar);

        string[] lines = Generate(font, OutputFormat.CStm32, "font_12x16").Files[0].Text.Split("\r\n");
        string[] glyphLines = lines.Where(l => l.StartsWith("    { ", StringComparison.Ordinal) || l.StartsWith("      0x", StringComparison.Ordinal)).ToArray();

        Assert.Equal(512, glyphLines.Length);
        Assert.All(glyphLines, l => Assert.Equal(16, l.Split("/*")[0].Split("0x").Length - 1));
    }

    [Fact]
    public void Small_c51_image_is_byte_exact()
    {
        var data = new ImageOutputData(
            16,
            8,
            new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0A, 0xAB, 0xCD, 0xEF, 0x10, 0x80, 0xFF },
            new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst },
            new ImageSourceInfo(@"D:\art\icon.bmp"),
            PresetInfo.CustomBasedOn("WG240128A"));

        OutputDocument document = Registry.Generate(data, Options(OutputFormat.CKeilC51, "icon_16x8") with { BytesPerLine = 6 });

        string[] header =
        {
            "/*",
            " * Image2GDRAM Converter 1.0",
            " * Источник: icon.bmp; пресет: Пользовательский (на основе WG240128A)",
            " * Размер: 16x8 (ШxВ); упаковка: горизонтальная, MSB first, 8 бит в байте, инверсия: нет",
            " * Размер массива: 16 байт",
            " */",
        };
        Assert.Equal(
            Join(header.Concat(new[]
            {
                "#include \"icon_16x8.h\"",
                "",
                "unsigned char code icon_16x8[ICON_16X8_SIZE] = {",
                "    0x01, 0x02, 0x03, 0x04, 0x05, 0x06,",
                "    0x07, 0x08, 0x09, 0x0A, 0xAB, 0xCD,",
                "    0xEF, 0x10, 0x80, 0xFF",
                "};",
            }).ToArray()),
            document.Files[0].Text);
        Assert.Equal(
            Join(header.Concat(new[]
            {
                "",
                "#ifndef ICON_16X8_H",
                "#define ICON_16X8_H",
                "",
                "#define ICON_16X8_WIDTH   16",
                "#define ICON_16X8_HEIGHT  8",
                "#define ICON_16X8_SIZE    16",
                "",
                "extern unsigned char code icon_16x8[ICON_16X8_SIZE];",
                "",
                "#endif",
            }).ToArray()),
            document.Files[1].Text);
    }

    [Fact]
    public void Sprite_13x11_keeps_its_actual_size_in_header_and_macros()
    {
        OutputDocument document = Generate(Image(13, 11, new PackingOptions { Direction = PackDirection.Horizontal }), OutputFormat.CStm32, "sprite_13x11");

        Assert.Contains(" * Размер: 13x11 (ШxВ); упаковка: горизонтальная, LSB first, 8 бит в байте, инверсия: нет", document.Files[0].Text);
        Assert.Contains(" * Размер массива: 22 байта", document.Files[0].Text);
        Assert.Contains(
            Join(
                "#define SPRITE_13X11_WIDTH   13",
                "#define SPRITE_13X11_HEIGHT  11",
                "#define SPRITE_13X11_SIZE    22"),
            document.Files[1].Text);
    }

    [Fact]
    public void All_gif_frames_form_a_two_dimensional_array()
    {
        OutputDocument document = Generate(Frames(3, 16, 8), OutputFormat.CStm32, "anim_16x8");
        string h = document.Files[1].Text;
        string c = document.Files[0].Text;

        Assert.Contains(
            Join(
                "#define ANIM_16X8_WIDTH       16",
                "#define ANIM_16X8_HEIGHT      8",
                "#define ANIM_16X8_FRAMES      3",
                "#define ANIM_16X8_FRAME_SIZE  16",
                "#define ANIM_16X8_SIZE        48",
                "",
                "extern const uint8_t anim_16x8[ANIM_16X8_FRAMES][ANIM_16X8_FRAME_SIZE];"),
            h);
        Assert.Contains("const uint8_t anim_16x8[ANIM_16X8_FRAMES][ANIM_16X8_FRAME_SIZE] = {\r\n    { /* кадр 1 */\r\n        0x", c);
        Assert.Contains("\r\n    },\r\n    { /* кадр 2 */\r\n", c);
        Assert.EndsWith("\r\n    }\r\n};\r\n", c);
        Assert.Equal(3, c.Split("/* кадр ").Length - 1);
    }

    [Theory]
    [InlineData(1, 1024)]
    [InlineData(7, 147)]
    [InlineData(16, 64)]
    public void Bytes_per_line_sets_data_line_count(int perLine, int lines)
    {
        OutputDocument document = Registry.Generate(Logo128x64(), Options(OutputFormat.CStm32, "logo") with { BytesPerLine = perLine });

        string[] dataLines = document.Files[0].Text.Split("\r\n").Where(l => l.StartsWith("    0x", StringComparison.Ordinal)).ToArray();

        Assert.Equal(lines, dataLines.Length);
        Assert.All(dataLines[..^1], l => Assert.EndsWith(",", l));
        Assert.False(dataLines[^1].EndsWith(','));
    }

    [Theory]
    [InlineData(OutputFormat.CKeilC51)]
    [InlineData(OutputFormat.CStm32)]
    public void C_output_uses_only_block_comments_hex_literals_and_no_static(OutputFormat format)
    {
        foreach (OutputData data in new OutputData[] { Logo128x64(), Font(6, 8), Frames(2) })
        {
            foreach (OutputFile file in Generate(data, format, "picture").Files)
            {
                Assert.DoesNotContain("//", file.Text);
                Assert.DoesNotContain("0b", file.Text);
                Assert.DoesNotContain("static", file.Text);
            }
        }
    }

    [Fact]
    public void C_source_includes_its_own_header()
    {
        foreach (OutputFormat format in new[] { OutputFormat.CKeilC51, OutputFormat.CStm32 })
        {
            OutputDocument document = Generate(Font(8, 8), format, "my_font");
            Assert.Contains("#include \"my_font.h\"\r\n", document.Files[0].Text);
            Assert.Equal("my_font.h", document.Files[1].FileName);
            Assert.Equal(OutputFileKind.CSource, document.Files[0].Kind);
            Assert.Equal(OutputFileKind.CHeader, document.Files[1].Kind);
        }
    }

    private static string GlyphComment(int code)
    {
        bool printable = code is >= 0x20 and <= 0x7E || (code >= 0x80 && code != 0x98 && code != 0xA0 && code != 0xAD);
        string hex = "0x" + code.ToString("X2", CultureInfo.InvariantCulture);
        if (!printable)
        {
            return "/* " + hex + " */";
        }

        Image2Gdram.Core.Text.Cp1251.RegisterEncodingProvider();
        char c = System.Text.Encoding.GetEncoding(1251).GetString(new[] { (byte)code })[0];
        return "/* " + hex + " '" + c + "' */";
    }
}
