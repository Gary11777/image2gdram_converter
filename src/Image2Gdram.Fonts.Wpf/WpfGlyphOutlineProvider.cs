using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Image2Gdram.Core.Fonts;
using FontMetrics = Image2Gdram.Core.Fonts.FontMetrics;

namespace Image2Gdram.Fonts.Wpf;

/// <summary>
/// Контуры глифов установленных шрифтов через WPF (решения D-15, N-47, N-48):
/// <c>FormattedText</c> с em-размером в пикселях, <c>pixelsPerDip = 1.0</c> и <c>TextFormattingMode.Ideal</c>,
/// затем <c>BuildGeometry</c> (с синтезированными жирным и курсивом), <c>GetFlattenedPathGeometry</c> и многоугольники.
/// Результат не зависит от масштаба экрана и ClearType: используются только контуры, без растра WPF.
/// Наличие глифа проверяется по <c>GlyphTypeface.CharacterToGlyphMap</c>, подстановка из других шрифтов исключена.
/// </summary>
public sealed class WpfGlyphOutlineProvider : IGlyphOutlineProvider
{
    /// <summary>Допуск аппроксимации кривых отрезками, пикселей.</summary>
    public const double FlatteningTolerance = 0.01;

    private readonly object _lock = new();

    public IReadOnlyList<string> GetInstalledFamilies()
    {
        lock (_lock)
        {
            return System.Windows.Media.Fonts.SystemFontFamilies
                .Select(f => f.Source)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(name => name, StringComparer.Ordinal)
                .ToArray();
        }
    }

    public bool TryGetMetrics(FontFaceSpec face, out FontMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(face);
        lock (_lock)
        {
            if (!TryCreateGlyphTypeface(face, out _, out GlyphTypeface? glyphTypeface))
            {
                metrics = default;
                return false;
            }

            double ascent = glyphTypeface.Baseline * face.SizePx;
            double descent = Math.Max(0, (glyphTypeface.Height - glyphTypeface.Baseline) * face.SizePx);
            metrics = new FontMetrics(ascent, descent);
            return true;
        }
    }

    public GlyphOutline? GetOutline(FontFaceSpec face, char character)
    {
        ArgumentNullException.ThrowIfNull(face);
        lock (_lock)
        {
            if (!TryCreateGlyphTypeface(face, out Typeface? typeface, out GlyphTypeface? glyphTypeface)
                || char.IsSurrogate(character)
                || !glyphTypeface.CharacterToGlyphMap.ContainsKey(character))
            {
                return null;
            }

            var text = new FormattedText(
                character.ToString(),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                face.SizePx,
                Brushes.Black,
                numberSubstitution: null,
                TextFormattingMode.Ideal,
                pixelsPerDip: 1.0);

            Geometry geometry = text.BuildGeometry(new Point(0, -text.Baseline));
            PathGeometry flattened = geometry.GetFlattenedPathGeometry(FlatteningTolerance, ToleranceType.Absolute);
            var contours = new List<IReadOnlyList<OutlinePoint>>();
            foreach (PathFigure figure in flattened.Figures)
            {
                if (!figure.IsFilled)
                {
                    continue;
                }

                var points = new List<OutlinePoint> { ToPoint(figure.StartPoint) };
                foreach (PathSegment segment in figure.Segments)
                {
                    switch (segment)
                    {
                        case LineSegment line:
                            points.Add(ToPoint(line.Point));
                            break;
                        case PolyLineSegment poly:
                            points.AddRange(poly.Points.Select(ToPoint));
                            break;
                        default:
                            throw new InvalidOperationException($"Unexpected segment {segment.GetType().Name} after flattening.");
                    }
                }

                contours.Add(points);
            }

            return new GlyphOutline(contours);
        }
    }

    /// <summary>
    /// Запятая в имени задаёт для WPF цепочку подстановки, <c>#</c> — ссылку на файл шрифта;
    /// такие имена считаются неустановленной гарнитурой.
    /// </summary>
    private static bool TryCreateGlyphTypeface(
        FontFaceSpec face,
        [NotNullWhen(true)] out Typeface? typeface,
        [NotNullWhen(true)] out GlyphTypeface? glyphTypeface)
    {
        typeface = null;
        glyphTypeface = null;
        if (face.Family.IndexOfAny(new[] { ',', '#' }) >= 0)
        {
            return false;
        }

        var candidate = new Typeface(
            new FontFamily(face.Family),
            face.Italic ? FontStyles.Italic : FontStyles.Normal,
            face.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStretches.Normal);
        if (!candidate.TryGetGlyphTypeface(out GlyphTypeface found))
        {
            return false;
        }

        typeface = candidate;
        glyphTypeface = found;
        return true;
    }

    private static OutlinePoint ToPoint(Point p) => new(p.X, p.Y);
}
