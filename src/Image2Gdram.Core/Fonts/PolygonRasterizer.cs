using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Fonts;

/// <summary>Правило заливки контура. Для глифов TrueType — <see cref="NonZero"/> (решение D-15).</summary>
public enum FillRule
{
    NonZero,
    EvenOdd,
}

/// <summary>Режим растеризации символа (п. 4.2.2 ТЗ, решение D-15).</summary>
public enum GlyphRenderMode
{
    /// <summary>Без сглаживания: пиксель активен, если его центр внутри контура.</summary>
    Aliased,

    /// <summary>Со сглаживанием: суперсэмплинг 4x4 -> покрытие -> яркость -> порог.</summary>
    Antialiased,
}

/// <summary>
/// Собственная заливка многоугольников по строкам развёртки (решения D-15, N-47). Отсчёт пикселя
/// в точке <c>(px + (i + 0.5) / n, py + (j + 0.5) / n)</c>. Отсчёт на левом или верхнем ребре внутри,
/// на правом или нижнем — снаружи, поэтому смежные фигуры с общим ребром не перекрываются и не дают
/// щели. Горизонтальные рёбра пропускаются, контуры замыкаются неявно; всё за пределами растра
/// отсекается.
/// </summary>
public static class PolygonRasterizer
{
    /// <summary>Число отсчётов на сторону пикселя в режиме сглаживания (4x4 = 16).</summary>
    public const int SupersamplesPerAxis = 4;

    public const int MaxSamplesPerAxis = 16;

    /// <summary>
    /// Для каждого пикселя растра width x height — число отсчётов (0..n^2) внутри контура, сдвинутого на
    /// (<paramref name="offsetX"/>, <paramref name="offsetY"/>). Порядок — по строкам сверху вниз.
    /// </summary>
    public static int[] ComputeCoverage(
        GlyphOutline outline,
        int width,
        int height,
        double offsetX,
        double offsetY,
        int samplesPerAxis,
        FillRule fillRule = FillRule.NonZero)
    {
        ArgumentNullException.ThrowIfNull(outline);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(samplesPerAxis, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(samplesPerAxis, MaxSamplesPerAxis);
        if (!double.IsFinite(offsetX) || !double.IsFinite(offsetY))
        {
            throw new ArgumentException("Offsets must be finite.");
        }

        if (fillRule is not (FillRule.NonZero or FillRule.EvenOdd))
        {
            throw new ArgumentOutOfRangeException(nameof(fillRule), fillRule, null);
        }

        var coverage = new int[checked(width * height)];
        Edge[] edges = BuildEdges(outline, offsetX, offsetY);
        if (edges.Length == 0)
        {
            return coverage;
        }

        int n = samplesPerAxis;
        var crossX = new double[edges.Length];
        var crossDir = new int[edges.Length];
        for (int py = 0; py < height; py++)
        {
            for (int j = 0; j < n; j++)
            {
                double sy = (py * n + j + 0.5) / n;
                int count = 0;
                foreach (Edge e in edges)
                {
                    if (e.Y0 <= sy && sy < e.Y1)
                    {
                        crossX[count] = e.X0 + (sy - e.Y0) * (e.X1 - e.X0) / (e.Y1 - e.Y0);
                        crossDir[count] = e.Direction;
                        count++;
                    }
                }

                if (count == 0)
                {
                    continue;
                }

                Array.Sort(crossX, crossDir, 0, count);
                int k = 0;
                int winding = 0;
                int rowStart = py * width;
                for (int px = 0; px < width; px++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        double sx = (px * n + i + 0.5) / n;
                        while (k < count && crossX[k] <= sx)
                        {
                            winding += crossDir[k];
                            k++;
                        }

                        bool inside = fillRule == FillRule.NonZero ? winding != 0 : (winding & 1) != 0;
                        if (inside)
                        {
                            coverage[rowStart + px]++;
                        }
                    }
                }
            }
        }

        return coverage;
    }

    /// <summary>
    /// Растеризует контур в монохромный растр. <see cref="GlyphRenderMode.Aliased"/> — один отсчёт в центре пикселя;
    /// <see cref="GlyphRenderMode.Antialiased"/> — 16 отсчётов, яркость по <see cref="CoverageToLuma"/>,
    /// пиксель активен при <c>Y &lt; threshold</c> (решение F-09). Порог в режиме без сглаживания не используется.
    /// </summary>
    public static MonoBitmap Rasterize(
        GlyphOutline outline,
        int width,
        int height,
        double offsetX,
        double offsetY,
        GlyphRenderMode mode,
        int threshold,
        FillRule fillRule = FillRule.NonZero)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(threshold, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(threshold, 255);
        int n = mode switch
        {
            GlyphRenderMode.Aliased => 1,
            GlyphRenderMode.Antialiased => SupersamplesPerAxis,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null),
        };

        int[] coverage = ComputeCoverage(outline, width, height, offsetX, offsetY, n, fillRule);
        int total = n * n;
        var bitmap = new MonoBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int covered = coverage[y * width + x];
                bitmap[x, y] = mode == GlyphRenderMode.Aliased ? covered != 0 : CoverageToLuma(covered, total) < threshold;
            }
        }

        return bitmap;
    }

    /// <summary>
    /// Яркость пикселя по покрытию: <c>Y = 255 - покрытие * 255</c>, округление к ближайшему целому
    /// в целых числах: <c>(255 * (total - covered) + total / 2) / total</c>.
    /// </summary>
    public static int CoverageToLuma(int covered, int total)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(total, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(covered, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(covered, total);
        return (255 * (total - covered) + total / 2) / total;
    }

    private static Edge[] BuildEdges(GlyphOutline outline, double offsetX, double offsetY)
    {
        var edges = new List<Edge>();
        foreach (IReadOnlyList<OutlinePoint> contour in outline.Contours)
        {
            int count = contour.Count;
            if (count < 2)
            {
                continue;
            }

            for (int i = 0; i < count; i++)
            {
                OutlinePoint a = contour[i];
                OutlinePoint b = contour[(i + 1) % count];
                double ax = a.X + offsetX, ay = a.Y + offsetY;
                double bx = b.X + offsetX, by = b.Y + offsetY;
                if (ay == by)
                {
                    continue;
                }

                edges.Add(ay < by ? new Edge(ax, ay, bx, by, 1) : new Edge(bx, by, ax, ay, -1));
            }
        }

        return edges.ToArray();
    }

    private readonly record struct Edge(double X0, double Y0, double X1, double Y1, int Direction);
}
