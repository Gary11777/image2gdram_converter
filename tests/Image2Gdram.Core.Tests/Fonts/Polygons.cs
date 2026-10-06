using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Fonts;

internal static class Polygons
{
    /// <summary>Прямоугольник по часовой стрелке на экране (y вниз): верх-лево → верх-право → низ-право → низ-лево.</summary>
    public static OutlinePoint[] Rect(double x0, double y0, double x1, double y1) =>
        new[] { new OutlinePoint(x0, y0), new OutlinePoint(x1, y0), new OutlinePoint(x1, y1), new OutlinePoint(x0, y1) };

    public static OutlinePoint[] Reversed(OutlinePoint[] contour) => contour.Reverse().ToArray();

    public static GlyphOutline Outline(params OutlinePoint[][] contours) => new(contours);

    /// <summary>Пятиконечная звезда одним самопересекающимся контуром (центр имеет число обхода 2).</summary>
    public static OutlinePoint[] Pentagram(double cx, double cy, double radius)
    {
        var points = new OutlinePoint[5];
        for (int k = 0; k < 5; k++)
        {
            double angle = (-90 + 144 * k) * Math.PI / 180;
            points[k] = new OutlinePoint(cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
        }

        return points;
    }

    public static string[] Rows(MonoBitmap bitmap)
    {
        var rows = new string[bitmap.Height];
        for (int y = 0; y < bitmap.Height; y++)
        {
            var chars = new char[bitmap.Width];
            for (int x = 0; x < bitmap.Width; x++)
            {
                chars[x] = bitmap[x, y] ? '#' : '.';
            }

            rows[y] = new string(chars);
        }

        return rows;
    }

    public static MonoBitmap Aliased(GlyphOutline outline, int width, int height, double dx = 0, double dy = 0, FillRule rule = FillRule.NonZero) =>
        PolygonRasterizer.Rasterize(outline, width, height, dx, dy, GlyphRenderMode.Aliased, 128, rule);

    /// <summary>
    /// Независимая проверка точки: число обхода по алгоритму Сандея (луч вправо). Используется только
    /// для точек, заведомо не лежащих на рёбрах.
    /// </summary>
    public static int WindingNumber(GlyphOutline outline, double px, double py)
    {
        int winding = 0;
        foreach (IReadOnlyList<OutlinePoint> contour in outline.Contours)
        {
            for (int i = 0; i < contour.Count; i++)
            {
                OutlinePoint a = contour[i];
                OutlinePoint b = contour[(i + 1) % contour.Count];
                double isLeft = (b.X - a.X) * (py - a.Y) - (px - a.X) * (b.Y - a.Y);
                if (a.Y <= py)
                {
                    if (b.Y > py && isLeft > 0)
                    {
                        winding++;
                    }
                }
                else if (b.Y <= py && isLeft < 0)
                {
                    winding--;
                }
            }
        }

        return winding;
    }
}
