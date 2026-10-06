using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Fonts;

/// <summary>Откуда взято текущее изображение символа.</summary>
public enum GlyphOrigin
{
    /// <summary>Ни источник, ни пользователь символ не заполняли — он пустой (фон).</summary>
    None,

    /// <summary>Из активного источника: TTF, лист символов или импортированный массив (решение N-27).</summary>
    Source,

    /// <summary>Нарисован или изменён вручную; перегенерация источника его не трогает (решение D-14).</summary>
    Manual,
}

/// <summary>
/// Таблица шрифта: 256 символов CP1251 одного размера ячейки (п. 4.2.1 ТЗ). Каждый символ — базовое
/// изображение от источника и необязательное ручное переопределение поверх него (решения D-14, N-27).
/// Смена размера ячейки и импорт массива создают новую таблицу. Класс не потокобезопасен:
/// для фонового пересчёта передавайте <see cref="Clone"/>.
/// </summary>
public sealed class FontTable
{
    public const int CharCount = 256;

    private readonly MonoBitmap?[] _source = new MonoBitmap?[CharCount];
    private readonly MonoBitmap?[] _manual = new MonoBitmap?[CharCount];
    private readonly MonoBitmap _blank;

    public FontTable(FontCellSize cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        Cell = cell;
        _blank = new MonoBitmap(cell.Width, cell.Height);
    }

    public FontCellSize Cell { get; }

    public bool HasManualEdits => _manual.Any(m => m is not null);

    /// <summary>Коды символов с ручными правками по возрастанию.</summary>
    public IReadOnlyList<int> ManualCodes => Enumerable.Range(0, CharCount).Where(c => _manual[c] is not null).ToArray();

    public GlyphOrigin GetOrigin(int code)
    {
        CheckCode(code);
        return _manual[code] is not null ? GlyphOrigin.Manual
            : _source[code] is not null ? GlyphOrigin.Source
            : GlyphOrigin.None;
    }

    public bool IsManual(int code) => _manual[CheckCode(code)] is not null;

    /// <summary>В итоговом изображении символа нет ни одного активного пикселя (упаковывается как фон, F-04).</summary>
    public bool IsEmpty(int code)
    {
        MonoBitmap glyph = Effective(CheckCode(code));
        for (int y = 0; y < glyph.Height; y++)
        {
            if (glyph.GetRow(y).Contains(true))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>Копия итогового изображения: ручное, иначе от источника, иначе пустое.</summary>
    public MonoBitmap GetGlyph(int code) => Effective(CheckCode(code)).Clone();

    /// <summary>Копия изображения от источника или <see langword="null"/>.</summary>
    public MonoBitmap? GetSourceGlyph(int code) => _source[CheckCode(code)]?.Clone();

    /// <summary>Копия ручного переопределения или <see langword="null"/>.</summary>
    public MonoBitmap? GetManualGlyph(int code) => _manual[CheckCode(code)]?.Clone();

    /// <summary>
    /// Заменяет базовое содержимое результатом источника: коды из набора получают его растры,
    /// остальные становятся пустыми. Ручные правки сохраняются (решение D-14).
    /// </summary>
    public void ReplaceSource(GlyphSet glyphs)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        if (glyphs.Cell != Cell)
        {
            throw new ArgumentException($"Glyph set cell {glyphs.Cell} does not match table cell {Cell}.", nameof(glyphs));
        }

        for (int code = 0; code < CharCount; code++)
        {
            _source[code] = glyphs.GetShared(code)?.Clone();
        }
    }

    /// <summary>Записывает ручное переопределение символа (копию растра размером в ячейку).</summary>
    public void SetManual(int code, MonoBitmap bitmap)
    {
        CheckCode(code);
        CheckSize(Cell, bitmap);
        _manual[code] = bitmap.Clone();
    }

    /// <summary>«Вернуть символ к источнику»: снимает ручное переопределение.</summary>
    public void RevertToSource(int code) => _manual[CheckCode(code)] = null;

    /// <summary>«Сбросить все ручные правки».</summary>
    public void ResetAllManual() => Array.Clear(_manual);

    public FontTable Clone()
    {
        var copy = new FontTable(Cell);
        for (int code = 0; code < CharCount; code++)
        {
            copy._source[code] = _source[code]?.Clone();
            copy._manual[code] = _manual[code]?.Clone();
        }

        return copy;
    }

    public static int GetBytesPerChar(FontCellSize cell, IPacker packer, PackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(cell);
        ArgumentNullException.ThrowIfNull(packer);
        return packer.GetSize(cell.Width, cell.Height, options);
    }

    /// <summary>
    /// Упаковывает все 256 символов подряд: символ c занимает байты c·N … c·N + N − 1 (решение F-04).
    /// Пустой символ упаковывается как фон: 0x00, с инверсией 0xFF.
    /// </summary>
    public byte[] Pack(IPacker packer, PackingOptions options)
    {
        int perChar = GetBytesPerChar(Cell, packer, options);
        var data = new byte[checked(perChar * CharCount)];
        for (int code = 0; code < CharCount; code++)
        {
            packer.Pack(Effective(code), options, data.AsSpan(code * perChar, perChar));
        }

        return data;
    }

    /// <summary>Данные для генераторов вывода этапа 3.</summary>
    public FontOutputData ToOutputData(IPacker packer, PackingOptions options, FontSourceInfo source, PresetInfo preset) =>
        new(Cell.Width, Cell.Height, Pack(packer, options), options, source, preset);

    internal static int CheckCode(int code) =>
        (uint)code < CharCount ? code : throw new ArgumentOutOfRangeException(nameof(code), code, "Character code must be in 0..255.");

    internal static void CheckSize(FontCellSize cell, MonoBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        if (bitmap.Width != cell.Width || bitmap.Height != cell.Height)
        {
            throw new ArgumentException($"Glyph bitmap {bitmap.Width}x{bitmap.Height} does not match cell {cell}.", nameof(bitmap));
        }
    }

    private MonoBitmap Effective(int code) => _manual[code] ?? _source[code] ?? _blank;
}
