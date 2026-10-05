using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Output;

/// <summary>Заголовок-комментарий (п. 4.4.3 ТЗ, решения D-02, D-03, D-04, N-02, N-04).</summary>
public class HeaderCommentTests
{
    [Theory]
    [InlineData(PackDirection.Vertical, BitOrder.LsbFirst, 8, PageTraversal.ByPages, false, 64, "вертикальная, LSB first, по страницам, инверсия: нет")]
    [InlineData(PackDirection.Vertical, BitOrder.MsbFirst, 8, PageTraversal.ByColumns, true, 16, "вертикальная, MSB first, по столбцам, инверсия: да")]
    [InlineData(PackDirection.Vertical, BitOrder.LsbFirst, 8, PageTraversal.ByColumns, false, 8, "вертикальная, LSB first, инверсия: нет")]
    [InlineData(PackDirection.Horizontal, BitOrder.MsbFirst, 8, PageTraversal.ByPages, false, 128, "горизонтальная, MSB first, 8 бит в байте, инверсия: нет")]
    [InlineData(PackDirection.Horizontal, BitOrder.LsbFirst, 6, PageTraversal.ByColumns, true, 8, "горизонтальная, LSB first, 6 бит в байте, инверсия: да")]
    public void Packing_description_shows_only_applicable_parameters(
        PackDirection direction, BitOrder bitOrder, int bits, PageTraversal traversal, bool invert, int height, string expected)
    {
        var packing = new PackingOptions { Direction = direction, BitOrder = bitOrder, BitsPerByte = bits, PageTraversal = traversal, Invert = invert };

        Assert.Equal(expected, HeaderCommentBuilder.DescribePacking(packing, height));
    }

    [Fact]
    public void Image_header_matches_n02()
    {
        var diagnostics = new List<Diagnostic>();

        IReadOnlyList<string> lines = HeaderCommentBuilder.Build(OutputTestData.Logo128x64(), OutputTestData.Options(OutputFormat.CStm32), diagnostics);

        Assert.Equal(
            new[]
            {
                "Image2GDRAM Converter 1.0",
                "Источник: logo.png; пресет: OLED128X64-0.96 (SSD1306)",
                "Размер: 128x64 (ШxВ); упаковка: вертикальная, LSB first, по страницам, инверсия: нет",
                "Размер массива: 1024 байта",
            },
            lines);
        Assert.Empty(diagnostics);
    }

    [Fact]
    public void Date_line_is_second_and_uses_fixed_format()
    {
        OutputOptions options = OutputTestData.Options(OutputFormat.CStm32) with { IncludeDate = true, GeneratedAt = new DateTime(2026, 10, 3, 15, 52, 0) };

        IReadOnlyList<string> lines = HeaderCommentBuilder.Build(OutputTestData.Logo128x64(), options, new List<Diagnostic>());

        Assert.Equal("Дата: 2026-10-03 15:52:00", lines[1]);
        Assert.Equal(5, lines.Count);
    }

    [Fact]
    public void Font_header_matches_n02()
    {
        FontOutputData font = OutputTestData.Font(12, 16, source: FontSourceInfo.TrueType("Consolas", 16));

        IReadOnlyList<string> lines = HeaderCommentBuilder.Build(font, OutputTestData.Options(OutputFormat.CKeilC51), new List<Diagnostic>());

        Assert.Equal(
            new[]
            {
                "Image2GDRAM Converter 1.0",
                "Шрифт: Consolas, 16 px; ячейка 12x16 (ШxВ)",
                "Упаковка: вертикальная, LSB first, по страницам, инверсия: нет; пресет: OLED128X64-0.96 (SSD1306)",
                "Размер: 256 символов x 24 байта = 6144 байта",
            },
            lines);
    }

    [Theory]
    [InlineData(FontSourceKind.TrueType, "Шрифт: Arial, 8 px, жирный, курсив; ячейка 6x8 (ШxВ)")]
    [InlineData(FontSourceKind.Sheet, "Шрифт: лист font.png; ячейка 6x8 (ШxВ)")]
    [InlineData(FontSourceKind.Import, "Шрифт: импорт из font.c (массив font_6x8); ячейка 6x8 (ШxВ)")]
    [InlineData(FontSourceKind.Manual, "Шрифт: ручное рисование; ячейка 6x8 (ШxВ)")]
    public void Font_source_is_described_by_kind(FontSourceKind kind, string expected)
    {
        FontSourceInfo source = kind switch
        {
            FontSourceKind.TrueType => FontSourceInfo.TrueType("Arial", 8, bold: true, italic: true),
            FontSourceKind.Sheet => FontSourceInfo.Sheet(@"C:\fonts\font.png"),
            FontSourceKind.Import => FontSourceInfo.Import(@"C:\src\font.c", "font_6x8"),
            _ => FontSourceInfo.Manual,
        };

        IReadOnlyList<string> lines = HeaderCommentBuilder.Build(OutputTestData.Font(6, 8, source: source), OutputTestData.Options(OutputFormat.CKeilC51), new List<Diagnostic>());

        Assert.Equal(expected, lines[1]);
        Assert.Equal("Размер: 256 символов x 6 байт = 1536 байт", lines[3]);
    }

    [Fact]
    public void Gif_frame_and_all_frames_are_described()
    {
        var single = new ImageOutputData(16, 16, new byte[32], PackingOptions.Default, new ImageSourceInfo("anim.gif", 12, 3), PresetInfo.Custom);
        IReadOnlyList<string> one = HeaderCommentBuilder.Build(single, OutputTestData.Options(OutputFormat.CStm32), new List<Diagnostic>());
        Assert.Equal("Источник: anim.gif, кадр 3; пресет: Пользовательский", one[1]);
        Assert.Equal("Размер массива: 32 байта", one[3]);

        IReadOnlyList<string> all = HeaderCommentBuilder.Build(OutputTestData.Frames(12, 32, 32), OutputTestData.Options(OutputFormat.CStm32), new List<Diagnostic>());
        Assert.Equal("Источник: anim.gif, все кадры (12); пресет: OLED128X64-0.96 (SSD1306)", all[1]);
        Assert.Equal("Размер массива: 12 кадров x 128 байт = 1536 байт", all[3]);
    }

    [Fact]
    public void Preset_variants()
    {
        Assert.EndsWith("пресет: Пользовательский (на основе WG240128A)", Header(PresetInfo.CustomBasedOn("WG240128A"))[1]);
        Assert.EndsWith("пресет: Пользовательский", Header(PresetInfo.Custom)[1]);
        Assert.EndsWith("пресет: Мой дисплей", Header(PresetInfo.Named("Мой дисплей"))[1]);
    }

    [Fact]
    public void Characters_outside_cp1251_are_replaced_with_a_warning()
    {
        var diagnostics = new List<Diagnostic>();
        var data = new ImageOutputData(8, 8, new byte[8], PackingOptions.Default, new ImageSourceInfo("logo\u00D7\u65E5.png"), PresetInfo.Named("a*/b"));

        IReadOnlyList<string> lines = HeaderCommentBuilder.Build(data, OutputTestData.Options(OutputFormat.CStm32), diagnostics);

        Assert.Equal("Источник: logo??.png; пресет: a*?b", lines[1]);
        Assert.Equal(2, diagnostics.Count);
        Assert.All(diagnostics, d => Assert.Equal(DiagnosticCode.NonCp1251CharactersReplaced, d.Code));
        Assert.Equal(new[] { nameof(HeaderField.SourceFile), "logo\u00D7\u65E5.png" }, diagnostics[0].Arguments);
        Assert.Equal(nameof(HeaderField.Preset), diagnostics[1].Arguments[0]);
    }

    [Fact]
    public void Font_family_outside_cp1251_is_replaced()
    {
        var diagnostics = new List<Diagnostic>();
        FontOutputData font = OutputTestData.Font(6, 8, source: FontSourceInfo.TrueType("MS \u30B4\u30B7\u30C3\u30AF", 8));

        IReadOnlyList<string> lines = HeaderCommentBuilder.Build(font, OutputTestData.Options(OutputFormat.CStm32), diagnostics);

        Assert.Equal("Шрифт: MS ????, 8 px; ячейка 6x8 (ШxВ)", lines[1]);
        Assert.Equal(nameof(HeaderField.FontFamily), Assert.Single(diagnostics).Arguments[0]);
    }

    private static IReadOnlyList<string> Header(PresetInfo preset) =>
        HeaderCommentBuilder.Build(
            new ImageOutputData(8, 8, new byte[8], PackingOptions.Default, new ImageSourceInfo("a.png"), preset),
            OutputTestData.Options(OutputFormat.CStm32),
            new List<Diagnostic>());
}
