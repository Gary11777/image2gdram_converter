using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;
using static Image2Gdram.Core.Tests.Fonts.Polygons;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Заливка многоугольников и суперсэмплинг (решения D-15, N-47).</summary>
public class PolygonRasterizerTests
{
    [Fact]
    public void Square_covers_exactly_its_pixels()
    {
        MonoBitmap bitmap = Aliased(Outline(Rect(1, 1, 3, 3)), 4, 4);
        Assert.Equal(new[] { "....", ".##.", ".##.", "...." }, Rows(bitmap));
        int[] coverage = PolygonRasterizer.ComputeCoverage(Outline(Rect(1, 1, 3, 3)), 4, 4, 0, 0, 4);
        Assert.Equal(16, coverage[1 * 4 + 1]);
        Assert.Equal(0, coverage[0]);
        Assert.Equal(4 * 16, coverage.Sum());
    }

    [Fact]
    public void Pixel_center_on_left_and_top_edges_is_inside_on_right_and_bottom_is_outside()
    {
        // Рёбра проходят ровно через центры пикселей: (0.5, 0.5) лежит на левом и верхнем ребре, (1.5, 1.5) — на правом и нижнем.
        MonoBitmap bitmap = Aliased(Outline(Rect(0.5, 0.5, 1.5, 1.5)), 3, 3);
        Assert.Equal(new[] { "#..", "...", "..." }, Rows(bitmap));
    }

    [Fact]
    public void Orientation_does_not_change_result()
    {
        OutlinePoint[] contour = Rect(0.5, 0.5, 2.5, 3.5);
        Assert.Equal(Rows(Aliased(Outline(contour), 4, 4)), Rows(Aliased(Outline(Reversed(contour)), 4, 4)));
        Assert.Equal(
            PolygonRasterizer.ComputeCoverage(Outline(contour), 4, 4, 0, 0, 4),
            PolygonRasterizer.ComputeCoverage(Outline(Reversed(contour)), 4, 4, 0, 0, 4));
    }

    [Theory]
    [InlineData(1.5)]
    [InlineData(1.375)]
    [InlineData(1.25)]
    [InlineData(1.0)]
    [InlineData(1.7)]
    public void Shapes_sharing_an_edge_neither_overlap_nor_leave_a_gap(double split)
    {
        GlyphOutline left = Outline(Rect(0, 0, split, 2));
        GlyphOutline right = Outline(Rect(split, 0, 3, 2));
        GlyphOutline whole = Outline(Rect(0, 0, 3, 2));
        foreach (int n in new[] { 1, 4 })
        {
            int[] a = PolygonRasterizer.ComputeCoverage(left, 3, 2, 0, 0, n);
            int[] b = PolygonRasterizer.ComputeCoverage(right, 3, 2, 0, 0, n);
            int[] all = PolygonRasterizer.ComputeCoverage(whole, 3, 2, 0, 0, n);
            Assert.Equal(all, a.Zip(b, (x, y) => x + y).ToArray());
        }
    }

    [Fact]
    public void Horizontal_edges_on_sample_rows_follow_the_top_left_rule()
    {
        // Верхнее ребро на y = 0.5 включается, нижнее на y = 2.5 — нет: заняты строки 0 и 1.
        Assert.Equal(new[] { "###", "###", "...", "..." }, Rows(Aliased(Outline(Rect(0, 0.5, 3, 2.5)), 3, 4)));

        // Вырожденный по высоте прямоугольник (только горизонтальные рёбра) ничего не закрашивает.
        Assert.All(Rows(Aliased(Outline(Rect(0, 0.5, 3, 0.5)), 3, 2)), row => Assert.Equal("...", row));
    }

    [Theory]
    [InlineData(FillRule.NonZero)]
    [InlineData(FillRule.EvenOdd)]
    public void Letter_o_with_opposite_inner_contour_has_a_hole_under_both_rules(FillRule rule)
    {
        GlyphOutline o = Outline(Rect(0, 0, 6, 6), Reversed(Rect(2, 2, 4, 4)));
        Assert.Equal(
            new[] { "######", "######", "##..##", "##..##", "######", "######" },
            Rows(Aliased(o, 6, 6, rule: rule)));
    }

    [Fact]
    public void Same_direction_inner_contour_is_filled_by_nonzero_and_hollow_by_evenodd()
    {
        GlyphOutline nested = Outline(Rect(0, 0, 6, 6), Rect(2, 2, 4, 4));
        Assert.All(Rows(Aliased(nested, 6, 6, rule: FillRule.NonZero)), row => Assert.Equal("######", row));
        Assert.Equal("##..##", Rows(Aliased(nested, 6, 6, rule: FillRule.EvenOdd))[2]);
    }

    [Theory]
    [InlineData(FillRule.NonZero)]
    [InlineData(FillRule.EvenOdd)]
    public void Digit_eight_has_two_holes(FillRule rule)
    {
        GlyphOutline eight = Outline(Rect(0, 0, 6, 12), Reversed(Rect(2, 2, 4, 5)), Reversed(Rect(2, 7, 4, 10)));
        string[] rows = Rows(Aliased(eight, 6, 12, rule: rule));
        Assert.Equal("######", rows[1]);
        Assert.Equal("##..##", rows[3]);
        Assert.Equal("######", rows[6]);
        Assert.Equal("##..##", rows[8]);
        Assert.Equal("######", rows[11]);
    }

    [Fact]
    public void Self_intersecting_pentagram_center_depends_on_fill_rule()
    {
        GlyphOutline star = Outline(Pentagram(10, 10, 10));
        Assert.Equal(2, Math.Abs(WindingNumber(star, 10.0, 10.0)));
        Assert.True(Aliased(star, 20, 20, 0.5, 0.5, FillRule.NonZero)[10, 10]);
        Assert.False(Aliased(star, 20, 20, 0.5, 0.5, FillRule.EvenOdd)[10, 10]);
    }

    [Theory]
    [InlineData(FillRule.NonZero)]
    [InlineData(FillRule.EvenOdd)]
    public void Bow_tie_fills_both_lobes(FillRule rule)
    {
        // Самопересечение «бабочка»: левый и правый треугольники с числом обхода +1 и -1.
        var bowTie = Outline(new[] { new OutlinePoint(0, 0), new OutlinePoint(8, 8), new OutlinePoint(8, 0), new OutlinePoint(0, 8) });
        MonoBitmap bitmap = Aliased(bowTie, 8, 8, rule: rule);
        Assert.True(bitmap[0, 4]);
        Assert.True(bitmap[7, 4]);
        Assert.False(bitmap[4, 0]);
        Assert.False(bitmap[4, 7]);
    }

    public static IEnumerable<object[]> DegenerateOutlines() => new[]
    {
        new object[] { "empty", GlyphOutline.Empty },
        new object[] { "empty contour", Outline(Array.Empty<OutlinePoint>()) },
        new object[] { "one point", Outline(new[] { new OutlinePoint(1, 1) }) },
        new object[] { "two points", Outline(new[] { new OutlinePoint(0, 0), new OutlinePoint(3, 3) }) },
        new object[] { "collinear", Outline(new[] { new OutlinePoint(0, 0), new OutlinePoint(1, 1), new OutlinePoint(3, 3) }) },
        new object[] { "zero width", Outline(Rect(1.5, 0, 1.5, 4)) },
        new object[] { "repeated point", Outline(new[] { new OutlinePoint(2, 2), new OutlinePoint(2, 2), new OutlinePoint(2, 2) }) },
        new object[] { "cancelling pair", Outline(Rect(0, 0, 4, 4), Reversed(Rect(0, 0, 4, 4))) },
    };

    [Theory]
    [MemberData(nameof(DegenerateOutlines))]
    public void Degenerate_and_empty_contours_cover_nothing(string name, GlyphOutline outline)
    {
        Assert.NotNull(name);
        foreach (int n in new[] { 1, 4, 16 })
        {
            Assert.All(PolygonRasterizer.ComputeCoverage(outline, 4, 4, 0, 0, n), c => Assert.Equal(0, c));
        }
    }

    [Theory]
    [InlineData(5, 0, 9, 4)]
    [InlineData(-9, 0, -5, 4)]
    [InlineData(0, -9, 4, -5)]
    [InlineData(0, 5, 4, 9)]
    [InlineData(-9, -9, -4, -4)]
    [InlineData(4, 4, 9, 9)]
    public void Contour_entirely_outside_the_cell_is_clipped_away(double x0, double y0, double x1, double y1)
    {
        foreach (int n in new[] { 1, 4 })
        {
            Assert.All(PolygonRasterizer.ComputeCoverage(Outline(Rect(x0, y0, x1, y1)), 4, 4, 0, 0, n), c => Assert.Equal(0, c));
        }
    }

    [Fact]
    public void Contour_larger_than_the_cell_fills_it_completely()
    {
        int[] coverage = PolygonRasterizer.ComputeCoverage(Outline(Rect(-100, -100, 100, 100)), 5, 3, 0, 0, 4);
        Assert.All(coverage, c => Assert.Equal(16, c));
    }

    [Fact]
    public void Partially_outside_contour_is_clipped_at_cell_borders()
    {
        Assert.Equal(new[] { "##..", "##..", "....", "...." }, Rows(Aliased(Outline(Rect(-2, -2, 2, 2)), 4, 4)));
        Assert.Equal(new[] { "....", "....", "..##", "..##" }, Rows(Aliased(Outline(Rect(2, 2, 7, 7)), 4, 4)));
    }

    [Fact]
    public void Negative_and_positive_offsets_move_the_contour()
    {
        GlyphOutline square = Outline(Rect(3, 3, 5, 5));
        Assert.Equal(new[] { "##..", "##..", "....", "...." }, Rows(Aliased(square, 4, 4, -3, -3)));
        Assert.Equal(new[] { "#...", "....", "....", "...." }, Rows(Aliased(square, 4, 4, -4, -4)));
        Assert.All(Rows(Aliased(square, 4, 4, -5, -5)), row => Assert.Equal("....", row));
        Assert.Equal(Rows(Aliased(Outline(Rect(0, 0, 2, 2)), 4, 4, 1, 2)), Rows(Aliased(square, 4, 4, -2, -1)));
    }

    [Fact]
    public void Supersampling_counts_partial_coverage()
    {
        // Отсчёты 4x4 стоят в 0.125, 0.375, 0.625, 0.875.
        Assert.Equal(8, PolygonRasterizer.ComputeCoverage(Outline(Rect(0, 0, 0.5, 1)), 1, 1, 0, 0, 4)[0]);
        Assert.Equal(1, PolygonRasterizer.ComputeCoverage(Outline(Rect(0, 0, 0.25, 0.25)), 1, 1, 0, 0, 4)[0]);
        Assert.Equal(0, PolygonRasterizer.ComputeCoverage(Outline(Rect(0, 0, 0.125, 0.125)), 1, 1, 0, 0, 4)[0]);
        Assert.Equal(1, PolygonRasterizer.ComputeCoverage(Outline(Rect(0.125, 0.125, 0.25, 0.25)), 1, 1, 0, 0, 4)[0]);
        Assert.Equal(12, PolygonRasterizer.ComputeCoverage(Outline(Rect(0.25, 0, 1, 1)), 1, 1, 0, 0, 4)[0]);
    }

    [Theory]
    [InlineData(0, 255)]
    [InlineData(1, 239)]
    [InlineData(4, 191)]
    [InlineData(8, 128)]
    [InlineData(12, 64)]
    [InlineData(15, 16)]
    [InlineData(16, 0)]
    public void Coverage_maps_to_luma_with_rounding(int covered, int luma)
    {
        Assert.Equal(luma, PolygonRasterizer.CoverageToLuma(covered, 16));
    }

    [Theory]
    [InlineData(8, 128, false)]
    [InlineData(8, 129, true)]
    [InlineData(16, 0, false)]
    [InlineData(16, 1, true)]
    [InlineData(0, 255, false)]
    [InlineData(1, 255, true)]
    [InlineData(1, 239, false)]
    [InlineData(1, 240, true)]
    public void Antialiased_pixel_is_active_when_luma_is_below_threshold(int coveredSamples, int threshold, bool active)
    {
        // Покрыто ровно coveredSamples отсчётов из 16: столбцы по 4 отсчёта плюс остаток в следующем столбце.
        var contours = new List<OutlinePoint[]>();
        int fullColumns = coveredSamples / 4;
        if (fullColumns > 0)
        {
            contours.Add(Rect(0, 0, fullColumns * 0.25, 1));
        }

        if (coveredSamples % 4 > 0)
        {
            contours.Add(Rect(fullColumns * 0.25, 0, (fullColumns + 1) * 0.25, coveredSamples % 4 * 0.25));
        }

        GlyphOutline outline = Outline(contours.ToArray());
        Assert.Equal(coveredSamples, PolygonRasterizer.ComputeCoverage(outline, 1, 1, 0, 0, 4)[0]);
        Assert.Equal(active, PolygonRasterizer.Rasterize(outline, 1, 1, 0, 0, GlyphRenderMode.Antialiased, threshold)[0, 0]);
    }

    [Fact]
    public void Thin_stroke_between_centers_is_lost_without_antialiasing_but_kept_with_it()
    {
        GlyphOutline stroke = Outline(Rect(0.6, 0, 1.4, 3));
        Assert.All(Rows(Aliased(stroke, 3, 3)), row => Assert.Equal("...", row));
        MonoBitmap smooth = PolygonRasterizer.Rasterize(stroke, 3, 3, 0, 0, GlyphRenderMode.Antialiased, 200);
        Assert.Equal(new[] { "##.", "##.", "##." }, Rows(smooth));
    }

    [Fact]
    public void Aliased_mode_ignores_threshold()
    {
        GlyphOutline shape = Outline(Rect(0.2, 0.2, 2.7, 1.9));
        string[] expected = Rows(Aliased(shape, 3, 3));
        foreach (int threshold in new[] { 0, 1, 128, 255 })
        {
            Assert.Equal(expected, Rows(PolygonRasterizer.Rasterize(shape, 3, 3, 0, 0, GlyphRenderMode.Aliased, threshold)));
        }
    }

    [Fact]
    public void Coverage_matches_independent_winding_number_on_random_polygons()
    {
        var random = new Random(20261006);
        for (int iteration = 0; iteration < 200; iteration++)
        {
            int contourCount = random.Next(1, 4);
            var contours = new OutlinePoint[contourCount][];
            for (int c = 0; c < contourCount; c++)
            {
                contours[c] = Enumerable.Range(0, random.Next(3, 9))
                    .Select(_ => new OutlinePoint(random.NextDouble() * 14 - 2, random.NextDouble() * 14 - 2))
                    .ToArray();
            }

            GlyphOutline outline = Outline(contours);
            int[] coverage = PolygonRasterizer.ComputeCoverage(outline, 10, 10, 0, 0, 4, FillRule.NonZero);
            int[] evenOdd = PolygonRasterizer.ComputeCoverage(outline, 10, 10, 0, 0, 4, FillRule.EvenOdd);
            for (int py = 0; py < 10; py++)
            {
                for (int px = 0; px < 10; px++)
                {
                    int expectedNonZero = 0, expectedEvenOdd = 0;
                    for (int j = 0; j < 4; j++)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            int w = WindingNumber(outline, px + (i + 0.5) / 4, py + (j + 0.5) / 4);
                            expectedNonZero += w != 0 ? 1 : 0;
                            expectedEvenOdd += (w & 1) != 0 ? 1 : 0;
                        }
                    }

                    Assert.Equal(expectedNonZero, coverage[py * 10 + px]);
                    Assert.Equal(expectedEvenOdd, evenOdd[py * 10 + px]);
                }
            }
        }
    }

    [Fact]
    public void Rasterization_is_deterministic()
    {
        var random = new Random(42);
        GlyphOutline outline = Outline(Enumerable.Range(0, 3)
            .Select(_ => Enumerable.Range(0, 12).Select(_ => new OutlinePoint(random.NextDouble() * 16 - 2, random.NextDouble() * 20 - 2)).ToArray())
            .ToArray());
        int[] first = PolygonRasterizer.ComputeCoverage(outline, 12, 16, 0.3, -0.7, 4);
        for (int run = 0; run < 3; run++)
        {
            Assert.Equal(first, PolygonRasterizer.ComputeCoverage(outline, 12, 16, 0.3, -0.7, 4));
        }

        MonoBitmap a = PolygonRasterizer.Rasterize(outline, 12, 16, 0.3, -0.7, GlyphRenderMode.Antialiased, 128);
        MonoBitmap b = PolygonRasterizer.Rasterize(outline, 12, 16, 0.3, -0.7, GlyphRenderMode.Antialiased, 128);
        Assert.True(a.ContentEquals(b));
    }

    [Fact]
    public void Invalid_arguments_are_rejected()
    {
        GlyphOutline square = Outline(Rect(0, 0, 1, 1));
        Assert.Throws<ArgumentException>(() => new GlyphOutline(new[] { new[] { new OutlinePoint(double.NaN, 0) } }));
        Assert.Throws<ArgumentException>(() => new GlyphOutline(new[] { new[] { new OutlinePoint(0, double.PositiveInfinity) } }));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.ComputeCoverage(square, 0, 1, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.ComputeCoverage(square, 1, 0, 0, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.ComputeCoverage(square, 1, 1, 0, 0, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.ComputeCoverage(square, 1, 1, 0, 0, 17));
        Assert.Throws<ArgumentException>(() => PolygonRasterizer.ComputeCoverage(square, 1, 1, double.NaN, 0, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.ComputeCoverage(square, 1, 1, 0, 0, 1, (FillRule)7));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.Rasterize(square, 1, 1, 0, 0, GlyphRenderMode.Antialiased, 256));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.Rasterize(square, 1, 1, 0, 0, GlyphRenderMode.Antialiased, -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.Rasterize(square, 1, 1, 0, 0, (GlyphRenderMode)5, 128));
        Assert.Throws<ArgumentOutOfRangeException>(() => PolygonRasterizer.CoverageToLuma(17, 16));
    }
}
