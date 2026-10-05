using System.Globalization;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using static Image2Gdram.Core.Tests.Output.OutputTestData;

namespace Image2Gdram.Core.Tests.Output;

/// <summary>
/// Золотые тесты A51 по примерам В.2 и В.3 с поправками D-02, D-03 и N-02; фрагмент начинается с заголовка (N-03).
/// </summary>
public class A51GeneratorGoldenTests
{
    private const string Db = "                DB      ";

    private static readonly string[] Font6x8Header =
    {
        "; Image2GDRAM Converter 1.0",
        "; Шрифт: ручное рисование; ячейка 6x8 (ШxВ)",
        "; Упаковка: вертикальная, LSB first, инверсия: нет; пресет: OLED128X64-0.96 (SSD1306)",
        "; Размер: 256 символов x 6 байт = 1536 байт",
    };

    [Fact]
    public void Module_font_6x8_matches_appendix_v2()
    {
        string text = Generate(Font6x8WithA(), OutputFormat.A51Module, "font_6x8").Files[0].Text;

        var expected = new List<string>(Font6x8Header)
        {
            "",
            "                PUBLIC  FONT_6X8",
            "",
            "?CO?FONT_6X8    SEGMENT CODE",
            "                RSEG    ?CO?FONT_6X8",
            "",
            "FONT_6X8:",
        };
        expected.AddRange(GlyphLines());
        expected.Add("                END");
        Assert.Equal(Join(expected.ToArray()), text);

        Assert.Contains("\r\n" + Db + "000h, 000h, 000h, 000h, 000h, 000h  ; 00h\r\n", text);
        Assert.Contains("\r\n" + Db + "07Ch, 012h, 011h, 012h, 07Ch, 000h  ; 0C0h 'А'\r\n", text);
    }

    [Fact]
    public void Include_fragment_matches_appendix_v3()
    {
        OutputDocument document = Generate(Font6x8WithA(), OutputFormat.A51Include, "font_6x8");

        var expected = new List<string>(Font6x8Header) { "", "FONT_6X8:" };
        expected.AddRange(GlyphLines());
        Assert.Equal(Join(expected.ToArray()), document.Files[0].Text);

        foreach (string directive in new[] { "PUBLIC", "SEGMENT", "RSEG", "END" })
        {
            Assert.DoesNotContain(directive, document.Files[0].Text);
        }
    }

    [Fact]
    public void Binary_numbers_are_eight_digits_with_b_suffix()
    {
        OutputOptions options = Options(OutputFormat.A51Module, "font_6x8") with { AsmNumberFormat = AsmNumberFormat.Binary };

        OutputDocument document = Registry.Generate(Font6x8WithA(), options);

        Assert.Contains("\r\n" + Db + "01111100b, 00010010b, 00010001b, 00010010b, 01111100b, 00000000b  ; 0C0h 'А'\r\n", document.Files[0].Text);
        Assert.Equal(9, document.ByteMap.Length);
    }

    [Fact]
    public void Image_lines_have_no_comments_and_frames_are_marked()
    {
        var image = new ImageOutputData(8, 16, Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(), PackingOptions.Default, new ImageSourceInfo("x.png"), Ssd1306);
        string text = Registry.Generate(image, Options(OutputFormat.A51Include, "x_8x16") with { BytesPerLine = 10 }).Files[0].Text;

        Assert.EndsWith(
            Join(
                "X_8X16:",
                Db + "000h, 001h, 002h, 003h, 004h, 005h, 006h, 007h, 008h, 009h",
                Db + "00Ah, 00Bh, 00Ch, 00Dh, 00Eh, 00Fh"),
            text);

        string frames = Generate(Frames(2, 8, 8), OutputFormat.A51Module, "anim").Files[0].Text;
        Assert.Contains("\r\nANIM:\r\n                ; кадр 1\r\n" + Db, frames);
        Assert.Contains("\r\n                ; кадр 2\r\n" + Db, frames);
        Assert.EndsWith("\r\n                END\r\n", frames);
    }

    [Fact]
    public void Lines_never_end_with_comma_and_stay_below_256_characters()
    {
        FontOutputData wide = Font(12, 16, new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst });
        foreach (OutputFormat format in new[] { OutputFormat.A51Module, OutputFormat.A51Include })
        {
            foreach (OutputData data in new OutputData[] { wide, Logo128x64(), Frames(2) })
            {
                OutputOptions options = Options(format, "data_array") with { AsmNumberFormat = AsmNumberFormat.Binary };
                foreach (string line in Registry.Generate(data, options).Files[0].Text.Split("\r\n"))
                {
                    Assert.False(line.TrimEnd().EndsWith(','), line);
                    Assert.True(line.Length < 256, line);
                }
            }
        }
    }

    [Fact]
    public void Long_segment_name_keeps_a_space_before_segment()
    {
        string name = new('a', NameValidator.MaxLengthA51Module);

        string text = Generate(Logo128x64(), OutputFormat.A51Module, name).Files[0].Text;

        string label = name.ToUpperInvariant();
        Assert.Contains($"\r\n?CO?{label} SEGMENT CODE\r\n", text);
        Assert.Contains($"\r\n                RSEG    ?CO?{label}\r\n", text);
        Assert.Contains($"\r\n                PUBLIC  {label}\r\n", text);
    }

    [Theory]
    [InlineData(AsmFileExtension.A51, "font_6x8.a51")]
    [InlineData(AsmFileExtension.Asm, "font_6x8.asm")]
    public void Extension_is_chosen_by_the_user(AsmFileExtension extension, string fileName)
    {
        OutputDocument document = Registry.Generate(Font6x8WithA(), Options(OutputFormat.A51Include, "font_6x8") with { AsmFileExtension = extension });

        OutputFile file = Assert.Single(document.Files);
        Assert.Equal(fileName, file.FileName);
        Assert.Equal(OutputFileKind.Assembly, file.Kind);
    }

    [Fact]
    public void Font_with_24_bytes_puts_the_comment_on_the_first_line_only()
    {
        string[] lines = Generate(Font(12, 16), OutputFormat.A51Module, "font_12x16").Files[0].Text.Split("\r\n");
        int first = Array.FindIndex(lines, l => l.EndsWith("; 0C0h 'А'", StringComparison.Ordinal));

        Assert.Equal(12, lines[first].Split(", ").Length);
        Assert.StartsWith(Db, lines[first + 1]);
        Assert.DoesNotContain(";", lines[first + 1]);
        Assert.Equal(12, lines[first + 1].Split(", ").Length);
    }

    private static FontOutputData Font6x8WithA()
    {
        var data = new byte[256 * 6];
        new byte[] { 0x7C, 0x12, 0x11, 0x12, 0x7C, 0x00 }.CopyTo(data, 0xC0 * 6);
        return Font(6, 8, data: data);
    }

    private static IEnumerable<string> GlyphLines()
    {
        Image2Gdram.Core.Text.Cp1251.RegisterEncodingProvider();
        var cp1251 = System.Text.Encoding.GetEncoding(1251);
        for (int code = 0; code < 256; code++)
        {
            string values = code == 0xC0 ? "07Ch, 012h, 011h, 012h, 07Ch, 000h" : "000h, 000h, 000h, 000h, 000h, 000h";
            string hex = code.ToString("X2", CultureInfo.InvariantCulture);
            string number = (hex[0] >= 'A' ? "0" : string.Empty) + hex + "h";
            bool printable = code is >= 0x20 and <= 0x7E || (code >= 0x80 && code != 0x98 && code != 0xA0 && code != 0xAD);
            string glyph = printable ? " '" + cp1251.GetString(new[] { (byte)code }) + "'" : string.Empty;
            yield return Db + values + "  ; " + number + glyph;
        }
    }
}
