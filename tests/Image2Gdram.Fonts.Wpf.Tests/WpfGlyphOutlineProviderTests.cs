using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Fonts.Wpf.Tests;

/// <summary>
/// Контуры и растеризация реальными шрифтами Windows (решения D-15, N-47, N-48). Проверяются свойства,
/// а не байты: эталоны шрифтов строятся по растровым листам (раздел 11 agents.md).
/// Arial, Consolas и Courier New входят в состав Windows 10/11.
/// </summary>
public class WpfGlyphOutlineProviderTests
{
    private static readonly WpfGlyphOutlineProvider Provider = new();

    public static IEnumerable<object[]> SystemFonts() => new[] { new object[] { "Arial" }, new object[] { "Consolas" }, new object[] { "Courier New" } };

    [Theory]
    [MemberData(nameof(SystemFonts))]
    public void Installed_font_has_positive_metrics(string family)
    {
        Assert.True(Provider.TryGetMetrics(new FontFaceSpec(family, 16), out FontMetrics metrics));
        Assert.InRange(metrics.Ascent, 8, 16);
        Assert.InRange(metrics.Descent, 1, 8);
        Assert.Contains(family, Provider.GetInstalledFamilies());
    }

    [Theory]
    [InlineData("No Such Font 9F3A")]
    [InlineData("Arial, Consolas")]
    [InlineData("#Arial")]
    public void Unknown_or_fallback_family_is_not_installed(string family)
    {
        var face = new FontFaceSpec(family, 16);
        Assert.False(Provider.TryGetMetrics(face, out _));
        Assert.Null(Provider.GetOutline(face, 'A'));
    }

    [Fact]
    public void Installed_families_are_sorted_and_unique()
    {
        IReadOnlyList<string> families = Provider.GetInstalledFamilies();
        Assert.NotEmpty(families);
        Assert.Equal(families.Count, families.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Equal(families.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ThenBy(f => f, StringComparer.Ordinal), families);
    }

    [Theory]
    [MemberData(nameof(SystemFonts))]
    public void Letter_outline_sits_on_the_baseline_at_the_left_edge(string family)
    {
        GlyphOutline outline = Provider.GetOutline(new FontFaceSpec(family, 16), 'H')!;
        Assert.NotEmpty(outline.Contours);
        OutlinePoint[] points = outline.Contours.SelectMany(c => c).ToArray();
        Assert.InRange(points.Max(p => p.Y), -0.05, 0.05);
        Assert.InRange(points.Min(p => p.Y), -16, -8);
        Assert.InRange(points.Min(p => p.X), 0, 4);
        Assert.InRange(points.Max(p => p.X), 4, 16);

        OutlinePoint[] descender = Provider.GetOutline(new FontFaceSpec(family, 16), 'p')!.Contours.SelectMany(c => c).ToArray();
        Assert.True(descender.Max(p => p.Y) > 1);
    }

    [Fact]
    public void Space_has_an_empty_outline_and_missing_characters_have_none()
    {
        var face = new FontFaceSpec("Arial", 16);
        Assert.Empty(Provider.GetOutline(face, ' ')!.Contours);
        Assert.Empty(Provider.GetOutline(face, '\u00A0')!.Contours);
        Assert.Null(Provider.GetOutline(face, '\u0378'));
        Assert.Null(Provider.GetOutline(face, '\u0001'));
        Assert.Null(Provider.GetOutline(face, '\uD800'));
    }

    [Theory]
    [MemberData(nameof(SystemFonts))]
    public void Default_ranges_render_to_non_empty_glyphs_inside_the_cell(string family)
    {
        foreach (FontCellSize cell in FontCellSize.All)
        {
            // Courier New at 8 px has strokes thinner than half a pixel: at threshold 128 they vanish, which is expected.
            var options = new TrueTypeOptions(new FontFaceSpec(family, cell.Height)) { Threshold = 224 };
            var source = new TrueTypeGlyphSource(new GlyphOutlineCache(Provider), options);
            GlyphSourceResult result = source.Render(cell, CharRangeSet.Default);

            foreach (int code in result.MissingCodes)
            {
                Assert.Null(Provider.GetOutline(source.Options.Face, TrueTypeGlyphSource.GetRenderableCharacter(code)!.Value));
            }

            Assert.Equal(161, result.Glyphs.Count + result.MissingCodes.Count);
            foreach (char c in "AZaz09@АЯаяЁё")
            {
                int code = Image2Gdram.Core.Text.Cp1251.GetBytes(c.ToString())[0];
                MonoBitmap glyph = result.Glyphs.Get(code)!;
                Assert.Equal(cell.Width, glyph.Width);
                Assert.Equal(cell.Height, glyph.Height);
                Assert.True(CountActive(glyph) > 0, $"{family} {cell} '{c}' is empty");
            }

            Assert.Equal(0, CountActive(result.Glyphs.Get(0x20)!));
        }
    }

    [Fact]
    public async Task Rendering_is_repeatable_across_instances_and_threads()
    {
        var options = new TrueTypeOptions(new FontFaceSpec("Consolas", 16)) { OffsetY = -1 };
        byte[] first = Pack(new TrueTypeGlyphSource(new GlyphOutlineCache(new WpfGlyphOutlineProvider()), options));
        byte[] second = Pack(new TrueTypeGlyphSource(new GlyphOutlineCache(new WpfGlyphOutlineProvider()), options));
        byte[] onWorker = await Task.Run(() => Pack(new TrueTypeGlyphSource(new GlyphOutlineCache(new WpfGlyphOutlineProvider()), options)));

        byte[] onSta = Array.Empty<byte>();
        var sta = new Thread(() => onSta = Pack(new TrueTypeGlyphSource(new GlyphOutlineCache(new WpfGlyphOutlineProvider()), options)));
        sta.SetApartmentState(ApartmentState.STA);
        sta.Start();
        sta.Join();

        Assert.Equal(first, second);
        Assert.Equal(first, onWorker);
        Assert.Equal(first, onSta);
        Assert.Contains(first, b => b != 0);
    }

    [Fact]
    public void Synthesized_and_real_styles_change_the_glyph()
    {
        MonoBitmap regular = Render(new FontFaceSpec("Arial", 16), 'l');
        MonoBitmap bold = Render(new FontFaceSpec("Arial", 16, bold: true), 'l');
        MonoBitmap italic = Render(new FontFaceSpec("Arial", 16, italic: true), 'l');
        Assert.True(CountActive(bold) > CountActive(regular));
        Assert.False(italic.ContentEquals(regular));
    }

    [Fact]
    public void Antialiased_threshold_controls_stroke_weight()
    {
        var face = new FontFaceSpec("Arial", 16);
        int light = CountActive(Render(face, 'O', GlyphRenderMode.Antialiased, threshold: 40));
        int normal = CountActive(Render(face, 'O', GlyphRenderMode.Antialiased, threshold: 128));
        int heavy = CountActive(Render(face, 'O', GlyphRenderMode.Antialiased, threshold: 250));
        Assert.True(light < normal && normal < heavy);
        Assert.True(CountActive(Render(face, 'O', GlyphRenderMode.Aliased)) > 0);
    }

    private static MonoBitmap Render(FontFaceSpec face, char c, GlyphRenderMode mode = GlyphRenderMode.Antialiased, int threshold = 128)
    {
        var source = new TrueTypeGlyphSource(new GlyphOutlineCache(Provider), new TrueTypeOptions(face) { RenderMode = mode, Threshold = threshold });
        int code = Image2Gdram.Core.Text.Cp1251.GetBytes(c.ToString())[0];
        return source.Render(FontCellSize.Cell12x16, new CharRangeSet(CharRangePreset.None, new[] { code })).Glyphs.Get(code)!;
    }

    private static byte[] Pack(TrueTypeGlyphSource source)
    {
        var table = new FontTable(FontCellSize.Cell12x16);
        table.ReplaceSource(source.Render(FontCellSize.Cell12x16, CharRangeSet.Default).Glyphs);
        return table.Pack(new Mono1bppPacker(), PackingOptions.Default);
    }

    private static int CountActive(MonoBitmap bitmap)
    {
        int count = 0;
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                count += bitmap[x, y] ? 1 : 0;
            }
        }

        return count;
    }
}
