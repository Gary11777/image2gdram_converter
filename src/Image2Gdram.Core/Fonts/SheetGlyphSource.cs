using System.Globalization;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Fonts;

/// <summary>
/// Параметры растрового листа символов (п. 4.2.2 ТЗ, источник 2; решения N-12, N-33, N-51).
/// Ячейка листа задаётся отдельно от ячейки шрифта.
/// </summary>
public sealed record SheetOptions
{
    /// <summary>Ширина ячейки листа, пикселей (1…64). По умолчанию — ячейка шрифта, её ставит интерфейс.</summary>
    public int CellWidth { get; init; } = 6;

    /// <summary>Высота ячейки листа, пикселей (1…64).</summary>
    public int CellHeight { get; init; } = 8;

    /// <summary>Отступ от левого края листа, пикселей (0…1024).</summary>
    public int MarginX { get; init; }

    /// <summary>Отступ от верхнего края листа, пикселей (0…1024).</summary>
    public int MarginY { get; init; }

    /// <summary>Интервал между ячейками по X, пикселей (0…256).</summary>
    public int SpacingX { get; init; }

    /// <summary>Интервал между ячейками по Y, пикселей (0…256).</summary>
    public int SpacingY { get; init; }

    /// <summary>Символов в строке листа (1…256).</summary>
    public int CharsPerRow { get; init; } = 16;

    /// <summary>Код символа в первой ячейке (0x00…0xFF).</summary>
    public int FirstCode { get; init; }

    /// <summary>Порог яркости листа (0…255). Активен пиксель с <c>Y &lt; Threshold</c>.</summary>
    public int Threshold { get; init; } = 128;
}

/// <summary>
/// Источник символов из растрового листа BMP/PNG (п. 4.2.2 ТЗ, источник 2).
/// Файл открывает вызывающий код через <see cref="Imaging.IImageDecoder"/>; здесь только уже собранный кадр.
/// Лист не изменяется.
/// </summary>
public sealed class SheetGlyphSource : IGlyphSource
{
    private readonly RgbaImage _sheet;
    private readonly SheetOptions _options;

    public SheetGlyphSource(RgbaImage sheet, string fileName, SheetOptions options)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(options);
        Validate(options);
        _sheet = sheet;
        _options = options;
        Info = FontSourceInfo.Sheet(fileName);
    }

    public FontSourceInfo Info { get; }

    public GlyphSourceResult Render(FontCellSize cell, CharRangeSet ranges, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(ranges);
        byte white = BackgroundCompositor.BackgroundValue(BackgroundColor.White);
        var glyphs = new GlyphSet(cell);
        int fitted = 0;
        int? firstMissing = null;
        int stepX = _options.CellWidth + _options.SpacingX;
        int stepY = _options.CellHeight + _options.SpacingY;

        for (int code = _options.FirstCode; code <= 0xFF; code++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int k = code - _options.FirstCode;
            int col = k % _options.CharsPerRow;
            int row = k / _options.CharsPerRow;
            int originX = _options.MarginX + col * stepX;
            int originY = _options.MarginY + row * stepY;
            bool inside = originX < _sheet.Width && originY < _sheet.Height;
            if (inside)
            {
                fitted++;
                if (ranges.Contains(code))
                {
                    glyphs.Set(code, ReadCell(cell, originX, originY, white));
                }
            }
            else if (firstMissing is null && ranges.Contains(code))
            {
                firstMissing = code;
            }
        }

        if (firstMissing is not int missing)
        {
            return new GlyphSourceResult(glyphs);
        }

        string[] arguments =
        {
            fitted.ToString(CultureInfo.InvariantCulture),
            "0x" + missing.ToString("X2", CultureInfo.InvariantCulture),
        };
        var warning = new Diagnostic(DiagnosticCode.SheetTooSmall, DiagnosticSeverity.Warning, arguments);
        return new GlyphSourceResult(glyphs, diagnostics: new[] { warning });
    }

    private MonoBitmap ReadCell(FontCellSize cell, int originX, int originY, byte white)
    {
        var sheetCell = new MonoBitmap(_options.CellWidth, _options.CellHeight);
        for (int y = 0; y < _options.CellHeight; y++)
        {
            int sy = originY + y;
            for (int x = 0; x < _options.CellWidth; x++)
            {
                int sx = originX + x;
                if ((uint)sx >= (uint)_sheet.Width || (uint)sy >= (uint)_sheet.Height)
                {
                    continue;
                }

                _sheet.GetPixel(sx, sy, out byte r, out byte g, out byte b, out byte a);
                byte rr = BackgroundCompositor.Channel(r, a, white);
                byte gg = BackgroundCompositor.Channel(g, a, white);
                byte bb = BackgroundCompositor.Channel(b, a, white);
                sheetCell[x, y] = GrayscaleStep.Luminance(rr, gg, bb) < _options.Threshold;
            }
        }

        return GlyphOps.Fit(sheetCell, cell);
    }

    private static void Validate(SheetOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(options.CellWidth, 1, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.CellWidth, 64, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.CellHeight, 1, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.CellHeight, 64, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MarginX, 0, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MarginX, 1024, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MarginY, 0, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MarginY, 1024, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.SpacingX, 0, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.SpacingX, 256, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.SpacingY, 0, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.SpacingY, 256, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.CharsPerRow, 1, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.CharsPerRow, 256, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.FirstCode, 0, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.FirstCode, 0xFF, nameof(options));
        ArgumentOutOfRangeException.ThrowIfLessThan(options.Threshold, 0, nameof(options));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.Threshold, 255, nameof(options));
    }
}
