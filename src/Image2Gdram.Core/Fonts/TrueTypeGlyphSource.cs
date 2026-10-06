using System.Globalization;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Fonts;

/// <summary>Параметры источника TrueType / OpenType (п. 4.2.2 ТЗ, источник 1; решения D-15, N-11, N-33, N-47).</summary>
public sealed record TrueTypeOptions
{
    public const int MinOffset = -32;
    public const int MaxOffset = 32;

    public TrueTypeOptions(FontFaceSpec face)
    {
        ArgumentNullException.ThrowIfNull(face);
        Face = face;
    }

    public FontFaceSpec Face { get; init; }

    /// <summary>Смещение глифа в ячейке по X, пикселей (-32..32; положительное — вправо).</summary>
    public int OffsetX { get; init; }

    /// <summary>Смещение глифа в ячейке по Y, пикселей (-32..32; положительное — вниз).</summary>
    public int OffsetY { get; init; }

    public GlyphRenderMode RenderMode { get; init; } = GlyphRenderMode.Antialiased;

    /// <summary>Порог яркости в режиме сглаживания (0..255, по умолчанию 128).</summary>
    public int Threshold { get; init; } = 128;
}

/// <summary>
/// Источник TrueType: CP1251 -> Unicode, контур из кэша, растеризация в ячейку (решения D-15, D-16, N-23, N-47).
/// Код 0x98 и управляющие коды (0x00..0x1F, 0x7F) не растеризуются и не считаются отсутствующими.
/// Контуры глифов кэшируются в <see cref="GlyphOutlineCache"/>: смена упаковки шрифт не перерисовывает.
/// </summary>
public sealed class TrueTypeGlyphSource : IGlyphSource
{
    public TrueTypeGlyphSource(GlyphOutlineCache cache, TrueTypeOptions options)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Face, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.OffsetX, TrueTypeOptions.MinOffset, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.OffsetX, TrueTypeOptions.MaxOffset, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.OffsetY, TrueTypeOptions.MinOffset, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.OffsetY, TrueTypeOptions.MaxOffset, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Threshold, 0, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Threshold, 255, nameof(options));
        if (options.RenderMode is not (GlyphRenderMode.Aliased or GlyphRenderMode.Antialiased))
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.RenderMode, "Unknown render mode.");
        }

        Cache = cache;
        Options = options;
    }

    public GlyphOutlineCache Cache { get; }

    public TrueTypeOptions Options { get; }

    public FontSourceInfo Info => FontSourceInfo.TrueType(Options.Face.Family, Options.Face.SizePx, Options.Face.Bold, Options.Face.Italic);

    /// <summary>
    /// Положение базовой линии в ячейке, пикселей от верха: строка текста (подъём + спуск) центрируется
    /// по высоте ячейки, результат округляется до целого (половины — от нуля) (решение N-47).
    /// </summary>
    public static int ComputeBaseline(FontMetrics metrics, int cellHeight)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellHeight, 1);
        double baseline = (cellHeight - (metrics.Ascent + metrics.Descent)) / 2.0 + metrics.Ascent;
        return (int)Math.Round(baseline, MidpointRounding.AwayFromZero);
    }

    /// <summary>Символ Unicode, который растеризуется для кода CP1251; <see langword="null"/> для 0x98 и управляющих кодов.</summary>
    public static char? GetRenderableCharacter(int code)
    {
        char? c = Cp1251.ToUnicode((byte)FontTable.CheckCode(code));
        return c is char ch && !char.IsControl(ch) ? ch : null;
    }

    public GlyphSourceResult Render(FontCellSize cell, CharRangeSet ranges, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(ranges);
        var glyphs = new GlyphSet(cell);
        FontFaceSpec face = Options.Face;
        if (!Cache.TryGetMetrics(face, out FontMetrics metrics))
        {
            var notFound = new Diagnostic(DiagnosticCode.FontNotFound, DiagnosticSeverity.Error, new[] { face.Family });
            return new GlyphSourceResult(glyphs, diagnostics: new[] { notFound });
        }

        int baseline = ComputeBaseline(metrics, cell.Height);
        var missing = new List<int>();
        foreach (int code in ranges.Codes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (GetRenderableCharacter(code) is not char c)
            {
                continue;
            }

            GlyphOutline? outline = Cache.GetOutline(face, c);
            if (outline is null)
            {
                missing.Add(code);
                continue;
            }

            MonoBitmap bitmap = PolygonRasterizer.Rasterize(
                outline,
                cell.Width,
                cell.Height,
                Options.OffsetX,
                baseline + Options.OffsetY,
                Options.RenderMode,
                Options.Threshold);
            glyphs.Set(code, bitmap);
        }

        var diagnostics = new List<Diagnostic>();
        if (missing.Count > 0)
        {
            string[] args = missing.Select(code => "0x" + code.ToString("X2", CultureInfo.InvariantCulture)).ToArray();
            diagnostics.Add(new Diagnostic(DiagnosticCode.GlyphsMissingInFont, DiagnosticSeverity.Warning, args));
        }

        return new GlyphSourceResult(glyphs, missing, diagnostics);
    }
}
