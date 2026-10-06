using System.Globalization;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Fonts;

namespace Image2Gdram.Core.Fonts.Import;

/// <summary>
/// Раскладка импортированных байтов по 256 символам (п. 4.2.2 ТЗ, источник 4; решения D-14, D-17, N-27).
/// Диапазоны не применяются: заполняется каждый код, для которого есть хотя бы один байт.
/// </summary>
public sealed class ImportGlyphSource : IGlyphSource
{
    private readonly byte[] _values;
    private readonly IPacker _packer;
    private readonly PackingOptions _packing;

    public ImportGlyphSource(ImportedArray array, string fileName, IPacker packer, PackingOptions packing)
    {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentNullException.ThrowIfNull(array.Values);
        ArgumentNullException.ThrowIfNull(packer);
        ArgumentNullException.ThrowIfNull(packing);
        if (array.Error is not null)
        {
            throw new ArgumentException(
                $"Array '{array.Name ?? "(unnamed)"}' at line {array.Line} cannot be imported: {array.Error.Kind}.",
                nameof(array));
        }

        _values = array.Values.ToArray();
        _packer = packer;
        _packing = packing;
        Info = FontSourceInfo.Import(fileName, array.Name);
    }

    public FontSourceInfo Info { get; }

    public GlyphSourceResult Render(FontCellSize cell, CharRangeSet ranges, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(ranges);
        int bytesPerChar = _packer.GetSize(cell.Width, cell.Height, _packing);
        int expected = checked(FontTable.CharCount * bytesPerChar);
        int actual = _values.Length;
        int available = Math.Min(actual, expected);
        int full = available / bytesPerChar;
        int remainder = available % bytesPerChar;
        var glyphs = new GlyphSet(cell);
        byte background = (byte)(_packing.Invert ? 0xFF : 0x00);

        for (int code = 0; code < full; code++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            glyphs.Set(code, Unpack(code * bytesPerChar, bytesPerChar, cell));
        }

        if (remainder > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var padded = new byte[bytesPerChar];
            new Span<byte>(padded).Fill(background);
            _values.AsSpan(full * bytesPerChar, remainder).CopyTo(padded);
            glyphs.Set(full, _packer.Unpack(padded, cell.Width, cell.Height, _packing));
        }

        if (actual == expected)
        {
            return new GlyphSourceResult(glyphs);
        }

        string[] arguments =
        {
            expected.ToString(CultureInfo.InvariantCulture),
            actual.ToString(CultureInfo.InvariantCulture),
        };
        var warning = new Diagnostic(DiagnosticCode.ImportValueCountMismatch, DiagnosticSeverity.Warning, arguments);
        return new GlyphSourceResult(glyphs, diagnostics: new[] { warning });
    }

    private MonoBitmap Unpack(int offset, int count, FontCellSize cell) =>
        _packer.Unpack(_values.AsSpan(offset, count), cell.Width, cell.Height, _packing);
}
