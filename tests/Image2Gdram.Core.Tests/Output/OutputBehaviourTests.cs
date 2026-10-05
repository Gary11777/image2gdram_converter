using System.Globalization;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.TestAssets;
using static Image2Gdram.Core.Tests.Output.OutputTestData;

namespace Image2Gdram.Core.Tests.Output;

/// <summary>Свойства, общие для всех форматов: BIN, карта байтов, кодировки и CRLF, детерминизм, предупреждения, реестр.</summary>
public class OutputBehaviourTests
{
    public static TheoryData<OutputFormat> Formats()
    {
        var data = new TheoryData<OutputFormat>();
        foreach (OutputFormat format in AllFormats)
        {
            data.Add(format);
        }

        return data;
    }

    private static IEnumerable<OutputData> Samples() => new OutputData[]
    {
        Logo128x64(),
        Image(13, 11, new PackingOptions { Direction = PackDirection.Horizontal, BitsPerByte = 6 }),
        Image(1, 1),
        Frames(3, 16, 16),
        Font(6, 8),
        Font(8, 8, new PackingOptions { Direction = PackDirection.Horizontal, BitsPerByte = 6, Invert = true }),
        Font(12, 16, new PackingOptions { PageTraversal = PageTraversal.ByColumns }),
    };

    [Fact]
    public void Bin_contains_only_the_array_bytes_and_frames_follow_each_other()
    {
        ImageOutputData frames = Frames(3, 16, 16);

        OutputDocument document = Generate(frames, OutputFormat.Bin, "anim");

        OutputFile file = Assert.Single(document.Files);
        Assert.Equal("anim.bin", file.FileName);
        Assert.Equal(OutputFileKind.Binary, file.Kind);
        Assert.Equal(frames.GetFrame(0).ToArray().Concat(frames.GetFrame(1).ToArray()).Concat(frames.GetFrame(2).ToArray()), file.Content);

        FontOutputData font = Font(6, 8);
        Assert.Equal(font.GetAllBytes(), Generate(font, OutputFormat.Bin, "font").Files[0].Content);
    }

    [Fact]
    public void Bin_preview_is_a_hex_dump_of_16_bytes_per_line()
    {
        var data = new ImageOutputData(8, 18, Enumerable.Range(0, 18).Select(i => (byte)(i * 15)).ToArray(), new PackingOptions { Direction = PackDirection.Horizontal }, new ImageSourceInfo("a.png"), Ssd1306);

        string dump = Generate(data, OutputFormat.Bin, "dump").Files[0].Text;

        Assert.Equal(
            Join(
                "00000000  00 0F 1E 2D 3C 4B 5A 69 78 87 96 A5 B4 C3 D2 E1",
                "00000010  F0 FF"),
            dump);
    }

    [Fact]
    public void Packed_test_pattern_reaches_bin_unchanged()
    {
        PatternBitmap pattern = TestPattern.CreateScreen(240, 128);
        var bitmap = new MonoBitmap(pattern.Width, pattern.Height);
        for (int y = 0; y < pattern.Height; y++)
        {
            for (int x = 0; x < pattern.Width; x++)
            {
                bitmap[x, y] = pattern.IsActive(x, y);
            }
        }

        var packing = new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst };
        byte[] packed = new Mono1bppPacker().Pack(bitmap, packing);
        var data = new ImageOutputData(240, 128, packed, packing, new ImageSourceInfo("test_pattern_240x128.png"), PresetInfo.Named("WG240128A", "RA6963 / T6963C"));

        Assert.Equal(3840, data.TotalBytes);
        Assert.Equal(packed, Generate(data, OutputFormat.Bin, "pattern").Files[0].Content);
        Assert.Contains(" * Размер массива: 3840 байт\r\n", Generate(data, OutputFormat.CStm32, "pattern").Files[0].Text);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Byte_map_points_at_each_byte_literal(OutputFormat format)
    {
        foreach (AsmNumberFormat numbers in new[] { AsmNumberFormat.Hex, AsmNumberFormat.Binary })
        {
            foreach (OutputData data in Samples())
            {
                OutputDocument document = Registry.Generate(data, Options(format) with { AsmNumberFormat = numbers, BytesPerLine = 7 });
                byte[] bytes = data.GetAllBytes();

                Assert.Equal(bytes.Length, document.ByteMap.Count);
                string text = document.Files[document.ByteMap.FileIndex].Text;
                int previous = -1;
                for (int i = 0; i < bytes.Length; i++)
                {
                    ByteSpan span = document.ByteMap[i];
                    Assert.True(span.Start > previous);
                    previous = span.Start;
                    Assert.Equal(bytes[i], ParseLiteral(text.Substring(span.Start, span.Length), format));
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Text_uses_crlf_only_and_ends_with_crlf(OutputFormat format)
    {
        foreach (OutputData data in Samples())
        {
            foreach (OutputFile file in Generate(data, format).Files)
            {
                Assert.EndsWith("\r\n", file.Text);
                Assert.DoesNotContain("\r\n\r\n\r\n", file.Text);
                string withoutCrlf = file.Text.Replace("\r\n", string.Empty, StringComparison.Ordinal);
                Assert.DoesNotContain('\n', withoutCrlf);
                Assert.DoesNotContain('\r', withoutCrlf);
            }
        }
    }

    [Theory]
    [InlineData(OutputFormat.CKeilC51)]
    [InlineData(OutputFormat.CStm32)]
    [InlineData(OutputFormat.A51Module)]
    [InlineData(OutputFormat.A51Include)]
    public void Cp1251_and_utf8_without_bom_encode_the_same_text(OutputFormat format)
    {
        FontOutputData font = Font(6, 8);
        OutputDocument cp1251 = Registry.Generate(font, Options(format, "font_6x8") with { Encoding = OutputEncoding.Cp1251 });
        OutputDocument utf8 = Registry.Generate(font, Options(format, "font_6x8") with { Encoding = OutputEncoding.Utf8NoBom });

        for (int i = 0; i < cp1251.Files.Count; i++)
        {
            Assert.Equal(cp1251.Files[i].Text, utf8.Files[i].Text);
            Assert.Equal(cp1251.Files[i].Text, System.Text.Encoding.GetEncoding(1251).GetString(cp1251.Files[i].Content));
            Assert.Equal(utf8.Files[i].Text, new System.Text.UTF8Encoding(false, true).GetString(utf8.Files[i].Content));
            Assert.False(utf8.Files[i].Content.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.False(cp1251.Files[i].Content.AsSpan().StartsWith(new byte[] { 0xEF, 0xBB, 0xBF }));
        }

        byte[] cpData = cp1251.Files[0].Content;
        byte[] utfData = utf8.Files[0].Content;
        Assert.True(Contains(cpData, new byte[] { (byte)'\'', 0xC0, (byte)'\'' }), "CP1251: the glyph 0xC0 is one byte");
        Assert.True(Contains(utfData, new byte[] { (byte)'\'', 0xD0, 0x90, (byte)'\'' }), "UTF-8: the glyph 0xC0 is D0 90");
        Assert.True(Contains(cpData, new byte[] { 0xD8, (byte)'x', 0xC2 }), "CP1251: (ШxВ)");
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Generation_without_date_is_byte_identical(OutputFormat format)
    {
        foreach (OutputData data in Samples())
        {
            OutputDocument first = Registry.Generate(data, Options(format) with { GeneratedAt = new DateTime(2026, 1, 1, 0, 0, 0) });
            OutputDocument second = Registry.Generate(data, Options(format) with { GeneratedAt = new DateTime(2030, 12, 31, 23, 59, 59) });

            Assert.Equal(first.Files.Count, second.Files.Count);
            for (int i = 0; i < first.Files.Count; i++)
            {
                Assert.Equal(first.Files[i].Content, second.Files[i].Content);
            }
        }
    }

    [Fact]
    public void Date_appears_in_every_text_file_when_enabled()
    {
        OutputOptions options = Options(OutputFormat.CStm32) with { IncludeDate = true, GeneratedAt = new DateTime(2026, 10, 3, 15, 52, 0) };

        foreach (OutputFile file in Registry.Generate(Logo128x64(), options).Files)
        {
            Assert.Contains("\r\n * Дата: 2026-10-03 15:52:00\r\n", file.Text);
        }

        string asm = Registry.Generate(Logo128x64(), options with { Format = OutputFormat.A51Include }).Files[0].Text;
        Assert.StartsWith("; Image2GDRAM Converter 1.0\r\n; Дата: 2026-10-03 15:52:00\r\n", asm);
    }

    [Theory]
    [InlineData(OutputFormat.CKeilC51, true)]
    [InlineData(OutputFormat.A51Module, true)]
    [InlineData(OutputFormat.A51Include, true)]
    [InlineData(OutputFormat.CStm32, false)]
    [InlineData(OutputFormat.Bin, false)]
    public void Arrays_above_65535_bytes_warn_for_c51_targets_without_blocking(OutputFormat format, bool warns)
    {
        var exactly = new ImageOutputData(8, 65535, new byte[65535], new PackingOptions { Direction = PackDirection.Horizontal }, new ImageSourceInfo("a.png"), Ssd1306);
        Assert.Empty(Generate(exactly, format).Diagnostics);

        OutputDocument big = Generate(Image(1024, 1024), format);

        Assert.Equal(131072, big.ByteMap.Count);
        if (warns)
        {
            Diagnostic warning = Assert.Single(big.Diagnostics);
            Assert.Equal(DiagnosticCode.ArrayExceeds64KForC51, warning.Code);
            Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
            Assert.Equal("131072", warning.Arguments[0]);
        }
        else
        {
            Assert.Empty(big.Diagnostics);
        }
    }

    [Fact]
    public void Header_warnings_reach_the_document()
    {
        var data = new ImageOutputData(8, 8, new byte[8], PackingOptions.Default, new ImageSourceInfo("\u65E5.png"), Ssd1306);

        OutputDocument document = Generate(data, OutputFormat.CStm32);

        Assert.Equal(DiagnosticCode.NonCp1251CharactersReplaced, Assert.Single(document.Diagnostics).Code);
        Assert.Contains("Источник: ?.png;", document.Files[0].Text);
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public void Invalid_name_or_bytes_per_line_is_rejected(OutputFormat format)
    {
        Assert.Throws<ArgumentException>(() => Generate(Logo128x64(), format, "code"));
        Assert.Throws<ArgumentException>(() => Generate(Logo128x64(), format, "1logo"));
        Assert.Throws<ArgumentOutOfRangeException>(() => Registry.Generate(Logo128x64(), Options(format) with { BytesPerLine = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => Registry.Generate(Logo128x64(), Options(format) with { BytesPerLine = 17 }));
    }

    [Fact]
    public void Name_of_28_characters_is_rejected_only_for_a51_module()
    {
        string name = new('n', 28);

        Assert.Throws<ArgumentException>(() => Generate(Logo128x64(), OutputFormat.A51Module, name));
        Assert.NotNull(Generate(Logo128x64(), OutputFormat.A51Include, name));
        Assert.NotNull(Generate(Logo128x64(), OutputFormat.CKeilC51, name));
    }

    [Fact]
    public void Default_registry_has_all_five_formats()
    {
        Assert.Equal(AllFormats, Registry.Formats);
        foreach (OutputFormat format in AllFormats)
        {
            Assert.Equal(format, Registry.Get(format).Format);
        }
    }

    [Fact]
    public void New_format_is_added_by_registration_only()
    {
        var registry = OutputGeneratorRegistry.CreateDefault();

        registry.Register(new UpperHexGenerator());

        OutputDocument document = registry.Generate(Image(8, 8), new OutputOptions { Format = UpperHexGenerator.CustomFormat, ArrayName = "x" });
        Assert.Equal("x.hex", document.Files[0].FileName);
        Assert.Throws<InvalidOperationException>(() => registry.Register(new UpperHexGenerator()));
        Assert.Throws<NotSupportedException>(() => new OutputGeneratorRegistry().Get(OutputFormat.Bin));
    }

    [Fact]
    public void Output_data_validates_inputs()
    {
        var source = new ImageSourceInfo("a.png");
        Assert.Throws<ArgumentException>(() => new ImageOutputData(8, 8, Array.Empty<byte>(), PackingOptions.Default, source, Ssd1306));
        Assert.Throws<ArgumentException>(() => new ImageOutputData(8, 8, new[] { new byte[8], new byte[8] }, allFrames: false, PackingOptions.Default, source, Ssd1306));
        Assert.Throws<ArgumentException>(() => new ImageOutputData(8, 8, new[] { new byte[8], new byte[7] }, allFrames: true, PackingOptions.Default, source, Ssd1306));
        Assert.Throws<ArgumentException>(() => new FontOutputData(6, 8, new byte[255], PackingOptions.Default, FontSourceInfo.Manual, Ssd1306));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ImageSourceInfo("a.gif", 3, 4));
        Assert.Equal("a.png", new ImageSourceInfo(@"C:\dir\a.png").FileName);
    }

    private static byte ParseLiteral(string literal, OutputFormat format)
    {
        if (literal.StartsWith("0x", StringComparison.Ordinal))
        {
            return byte.Parse(literal[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        if (format == OutputFormat.Bin)
        {
            return byte.Parse(literal, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        return literal[^1] switch
        {
            'h' => byte.Parse(literal[..^1], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
            'b' => Convert.ToByte(literal[..^1], 2),
            _ => throw new Xunit.Sdk.XunitException($"Unexpected literal '{literal}'."),
        };
    }

    private static bool Contains(byte[] haystack, byte[] needle) =>
        haystack.AsSpan().IndexOf(needle) >= 0;

    private sealed class UpperHexGenerator : IOutputGenerator
    {
        public const OutputFormat CustomFormat = (OutputFormat)100;

        public OutputFormat Format => CustomFormat;

        public OutputDocument Generate(OutputData data, OutputOptions options)
        {
            string text = Convert.ToHexString(data.GetAllBytes()) + "\r\n";
            int[] starts = Enumerable.Range(0, data.TotalBytes).Select(i => i * 2).ToArray();
            var file = new OutputFile(options.ArrayName + ".hex", OutputFileKind.Binary, text, System.Text.Encoding.ASCII.GetBytes(text));
            return new OutputDocument(new[] { file }, new ByteSpanMap(0, starts, 2), Array.Empty<Diagnostic>());
        }
    }
}
