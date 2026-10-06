using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Tests.Packing;
using static Image2Gdram.Core.Tests.Fonts.Polygons;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Источник TrueType без WPF: CP1251 в Unicode, базовая линия, смещения, кэш, отсутствующие глифы (D-15, N-23, N-47).</summary>
public class TrueTypeGlyphSourceTests
{
    private static readonly FontFaceSpec Face = new(FakeOutlineProvider.Family, 8);

    private static TrueTypeGlyphSource Source(FakeOutlineProvider provider, Func<TrueTypeOptions, TrueTypeOptions>? configure = null)
    {
        var options = new TrueTypeOptions(Face);
        return new TrueTypeGlyphSource(new GlyphOutlineCache(provider), configure is null ? options : configure(options));
    }

    [Theory]
    [InlineData(6, 2, 8, 6)]
    [InlineData(6, 2, 16, 10)]
    [InlineData(7.5, 2.5, 8, 7)]
    [InlineData(7.25, 2.25, 8, 7)]
    [InlineData(14.5, 2.5, 16, 14)]
    [InlineData(20, 5, 8, 12)]
    [InlineData(1, 0, 8, 5)]
    public void Baseline_centers_ascent_plus_descent_in_the_cell(double ascent, double descent, int cellHeight, int expected)
    {
        Assert.Equal(expected, TrueTypeGlyphSource.ComputeBaseline(new FontMetrics(ascent, descent), cellHeight));
    }

    [Fact]
    public void Baseline_rounds_halves_away_from_zero()
    {
        // (8 - 9) / 2 + 6.5 = 6.0; (8 - 9) / 2 + 7 = 6.5 -> 7; (1 - 4) / 2 + 1 = -0.5 -> -1.
        Assert.Equal(6, TrueTypeGlyphSource.ComputeBaseline(new FontMetrics(6.5, 2.5), 8));
        Assert.Equal(7, TrueTypeGlyphSource.ComputeBaseline(new FontMetrics(7, 2), 8));
        Assert.Equal(-1, TrueTypeGlyphSource.ComputeBaseline(new FontMetrics(1, 3), 1));
    }

    [Fact]
    public void Glyph_starts_at_left_edge_and_sits_on_the_baseline()
    {
        GlyphSourceResult result = Source(new FakeOutlineProvider()).Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        Assert.Equal(
            new[] { "......", "......", "......", "......", "##....", "##....", "......", "......" },
            Rows(result.Glyphs.Get('A')!));
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(-1, -4)]
    [InlineData(5, 0)]
    [InlineData(4, 3)]
    [InlineData(-2, -6)]
    public void Offsets_move_the_glyph_and_overflow_is_clipped(int offsetX, int offsetY)
    {
        MonoBitmap glyph = Source(new FakeOutlineProvider(), o => o with { OffsetX = offsetX, OffsetY = offsetY })
            .Render(FontCellSize.Cell6x8, CharRangeSet.Default).Glyphs.Get('A')!;

        // Квадрат 2x2 без смещения занимает столбцы 0-1 и строки 4-5 (базовая линия — строка 6).
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 6; x++)
            {
                bool expected = x - offsetX is 0 or 1 && y - offsetY is 4 or 5;
                Assert.Equal(expected, glyph[x, y]);
            }
        }
    }

    [Theory]
    [InlineData(32, 0)]
    [InlineData(-32, 0)]
    [InlineData(0, 32)]
    [InlineData(0, -32)]
    public void Glyph_moved_out_of_the_cell_is_empty(int offsetX, int offsetY)
    {
        GlyphSourceResult result = Source(new FakeOutlineProvider(), o => o with { OffsetX = offsetX, OffsetY = offsetY })
            .Render(FontCellSize.Cell12x16, CharRangeSet.Default);
        Assert.True(result.Glyphs.Contains('A'));
        Assert.All(Rows(result.Glyphs.Get('A')!), row => Assert.DoesNotContain('#', row));
    }

    [Fact]
    public void Only_codes_of_the_selected_ranges_are_filled()
    {
        GlyphSourceResult latin = Source(new FakeOutlineProvider()).Render(FontCellSize.Cell8x8, new CharRangeSet(CharRangePreset.Latin));
        Assert.Equal(Enumerable.Range(0x20, 0x5F), latin.Glyphs.Codes);

        GlyphSourceResult custom = Source(new FakeOutlineProvider()).Render(FontCellSize.Cell8x8, new CharRangeSet(CharRangePreset.None, new[] { 0x41, 0xC0 }));
        Assert.Equal(new[] { 0x41, 0xC0 }, custom.Glyphs.Codes);
    }

    [Fact]
    public void Cyrillic_codes_are_mapped_through_cp1251()
    {
        var provider = new FakeOutlineProvider();
        provider.Overrides['\u0401'] = Outline(Rect(0, -1, 1, 0));
        provider.Overrides['\u044F'] = Outline(Rect(5, -1, 6, 0));
        GlyphSourceResult result = Source(provider).Render(FontCellSize.Cell6x8, new CharRangeSet(CharRangePreset.Cyrillic));
        Assert.True(result.Glyphs.Get(0xA8)![0, 5]);
        Assert.False(result.Glyphs.Get(0xA8)![5, 5]);
        Assert.True(result.Glyphs.Get(0xFF)![5, 5]);
        Assert.Equal(66, result.Glyphs.Count);
    }

    [Fact]
    public void Space_has_an_empty_glyph_that_is_not_missing()
    {
        GlyphSourceResult result = Source(new FakeOutlineProvider()).Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        Assert.True(result.Glyphs.Contains(0x20));
        Assert.All(Rows(result.Glyphs.Get(0x20)!), row => Assert.Equal("......", row));
        Assert.DoesNotContain(0x20, result.MissingCodes);
    }

    [Fact]
    public void Missing_glyphs_stay_empty_and_are_listed_in_ascending_order()
    {
        var provider = new FakeOutlineProvider();
        provider.Missing.UnionWith(new[] { '\u044F', '\u0401', '~', '\u00A0' });
        var ranges = new CharRangeSet(CharRangePreset.Latin | CharRangePreset.Cyrillic | CharRangePreset.OtherCp1251);
        GlyphSourceResult result = Source(provider).Render(FontCellSize.Cell6x8, ranges);

        Assert.Equal(new[] { 0x7E, 0xA0, 0xA8, 0xFF }, result.MissingCodes);
        Assert.All(result.MissingCodes, code => Assert.False(result.Glyphs.Contains(code)));
        Diagnostic warning = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.GlyphsMissingInFont, warning.Code);
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
        Assert.Equal(new[] { "0x7E", "0xA0", "0xA8", "0xFF" }, warning.Arguments);
    }

    [Fact]
    public void Control_codes_and_0x98_are_never_rendered_nor_listed_as_missing()
    {
        var provider = new FakeOutlineProvider();
        var ranges = new CharRangeSet(CharRangePreset.None, Enumerable.Range(0x00, 0x20).Append(0x7F).Append(0x98));
        GlyphSourceResult result = Source(provider).Render(FontCellSize.Cell6x8, ranges);
        Assert.Equal(0, result.Glyphs.Count);
        Assert.Empty(result.MissingCodes);
        Assert.Empty(result.Diagnostics);
        Assert.Equal(0, provider.OutlineCalls);
    }

    [Theory]
    [InlineData(0x00, null)]
    [InlineData(0x0A, null)]
    [InlineData(0x1F, null)]
    [InlineData(0x7F, null)]
    [InlineData(0x98, null)]
    [InlineData(0x20, ' ')]
    [InlineData(0x41, 'A')]
    [InlineData(0x7E, '~')]
    [InlineData(0xA0, '\u00A0')]
    [InlineData(0xAD, '\u00AD')]
    [InlineData(0xB9, '\u2116')]
    [InlineData(0xC0, '\u0410')]
    [InlineData(0xFF, '\u044F')]
    public void Renderable_character_follows_cp1251(int code, char? expected)
    {
        Assert.Equal(expected, TrueTypeGlyphSource.GetRenderableCharacter(code));
    }

    [Fact]
    public void Font_that_is_not_installed_gives_an_error_and_no_glyphs()
    {
        var provider = new FakeOutlineProvider();
        var source = new TrueTypeGlyphSource(new GlyphOutlineCache(provider), new TrueTypeOptions(new FontFaceSpec("No Such Font", 8)));
        GlyphSourceResult result = source.Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        Assert.Equal(0, result.Glyphs.Count);
        Diagnostic error = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.FontNotFound, error.Code);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Equal(new[] { "No Such Font" }, error.Arguments);
        Assert.Equal(0, provider.OutlineCalls);
    }

    [Fact]
    public void Changing_offsets_mode_threshold_and_ranges_reuses_cached_outlines()
    {
        var provider = new FakeOutlineProvider();
        var cache = new GlyphOutlineCache(provider);
        var options = new TrueTypeOptions(Face);
        new TrueTypeGlyphSource(cache, options).Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        int outlineCalls = provider.OutlineCalls;
        int metricsCalls = provider.MetricsCalls;
        Assert.Equal(161, outlineCalls);

        new TrueTypeGlyphSource(cache, options with { OffsetX = 3, OffsetY = -2 }).Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        new TrueTypeGlyphSource(cache, options with { RenderMode = GlyphRenderMode.Aliased }).Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        new TrueTypeGlyphSource(cache, options with { Threshold = 10 }).Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        new TrueTypeGlyphSource(cache, options).Render(FontCellSize.Cell12x16, new CharRangeSet(CharRangePreset.Latin));
        Assert.Equal(outlineCalls, provider.OutlineCalls);
        Assert.Equal(metricsCalls, provider.MetricsCalls);

        new TrueTypeGlyphSource(cache, options with { Face = new FontFaceSpec(FakeOutlineProvider.Family, 9) }).Render(FontCellSize.Cell6x8, CharRangeSet.Default);
        Assert.Equal(2 * outlineCalls, provider.OutlineCalls);
    }

    [Fact]
    public void Repacking_the_table_does_not_touch_the_font()
    {
        var provider = new FakeOutlineProvider();
        var table = new FontTable(FontCellSize.Cell6x8);
        table.ReplaceSource(Source(provider).Render(FontCellSize.Cell6x8, CharRangeSet.Default).Glyphs);
        int calls = provider.OutlineCalls + provider.MetricsCalls;
        var packer = new Mono1bppPacker();
        foreach (int combination in TestBitmaps.CombinationIndexes)
        {
            table.Pack(packer, TestBitmaps.ToCore(Image2Gdram.Reference.RefOptions.AllCombinations[combination]));
        }

        Assert.Equal(calls, provider.OutlineCalls + provider.MetricsCalls);
    }

    [Fact]
    public void Cache_keeps_at_most_eight_faces_and_drops_the_least_recently_used()
    {
        var provider = new FakeOutlineProvider();
        var cache = new GlyphOutlineCache(provider);
        for (int size = 1; size <= GlyphOutlineCache.MaxFaces; size++)
        {
            cache.GetOutline(new FontFaceSpec(FakeOutlineProvider.Family, size), 'A');
        }

        cache.GetOutline(new FontFaceSpec(FakeOutlineProvider.Family, 1), 'A');
        int calls = provider.OutlineCalls;
        cache.GetOutline(new FontFaceSpec(FakeOutlineProvider.Family, 9), 'A');
        cache.GetOutline(new FontFaceSpec(FakeOutlineProvider.Family, 1), 'A');
        Assert.Equal(calls + 1, provider.OutlineCalls);
        cache.GetOutline(new FontFaceSpec(FakeOutlineProvider.Family, 2), 'A');
        Assert.Equal(calls + 2, provider.OutlineCalls);
    }

    [Fact]
    public void Rendering_is_deterministic()
    {
        var provider = new FakeOutlineProvider();
        provider.Overrides['A'] = Outline(Pentagram(3, -3, 3.3), Rect(0.2, -7.7, 5.9, -6.1));
        FontTable first = Table(Source(provider, o => o with { OffsetY = 1 }).Render(FontCellSize.Cell6x8, CharRangeSet.Default));
        FontTable second = Table(Source(provider, o => o with { OffsetY = 1 }).Render(FontCellSize.Cell6x8, CharRangeSet.Default));
        var packer = new Mono1bppPacker();
        Assert.Equal(first.Pack(packer, PackingOptions.Default), second.Pack(packer, PackingOptions.Default));
        Assert.False(first.IsEmpty('A'));
    }

    [Fact]
    public void Cancellation_is_observed()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        Assert.Throws<OperationCanceledException>(() => Source(new FakeOutlineProvider()).Render(FontCellSize.Cell6x8, CharRangeSet.Default, cts.Token));
    }

    [Fact]
    public void Source_info_describes_the_face()
    {
        TrueTypeGlyphSource source = Source(new FakeOutlineProvider(), o => o with { Face = new FontFaceSpec("Consolas", 16, bold: true, italic: true) });
        Assert.Equal(FontSourceInfo.TrueType("Consolas", 16, bold: true, italic: true), source.Info);
    }

    [Fact]
    public void Invalid_options_are_rejected()
    {
        var cache = new GlyphOutlineCache(new FakeOutlineProvider());
        var options = new TrueTypeOptions(Face);
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrueTypeGlyphSource(cache, options with { OffsetX = 33 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrueTypeGlyphSource(cache, options with { OffsetY = -33 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrueTypeGlyphSource(cache, options with { Threshold = 256 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new TrueTypeGlyphSource(cache, options with { RenderMode = (GlyphRenderMode)9 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FontFaceSpec("Arial", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FontFaceSpec("Arial", 65));
        Assert.Throws<ArgumentException>(() => new FontFaceSpec(" ", 8));
    }

    private static FontTable Table(GlyphSourceResult result)
    {
        var table = new FontTable(result.Glyphs.Cell);
        table.ReplaceSource(result.Glyphs);
        return table;
    }
}
