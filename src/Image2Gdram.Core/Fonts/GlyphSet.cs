using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Fonts;

/// <summary>
/// Результат источника символов: код → растр размером в ячейку. Коды без растра источник не заполнил.
/// Хранит копии растров; обход — по возрастанию кода, без зависимости от порядка словаря.
/// </summary>
public sealed class GlyphSet
{
    private readonly MonoBitmap?[] _glyphs = new MonoBitmap?[FontTable.CharCount];

    public GlyphSet(FontCellSize cell)
    {
        ArgumentNullException.ThrowIfNull(cell);
        Cell = cell;
    }

    public FontCellSize Cell { get; }

    public int Count { get; private set; }

    /// <summary>Заполненные коды по возрастанию.</summary>
    public IEnumerable<int> Codes
    {
        get
        {
            for (int code = 0; code < FontTable.CharCount; code++)
            {
                if (_glyphs[code] is not null)
                {
                    yield return code;
                }
            }
        }
    }

    public bool Contains(int code) => _glyphs[FontTable.CheckCode(code)] is not null;

    /// <summary>Копия растра символа или <see langword="null"/>, если код не заполнен.</summary>
    public MonoBitmap? Get(int code) => _glyphs[FontTable.CheckCode(code)]?.Clone();

    /// <summary>Записывает копию растра; размер растра должен совпадать с ячейкой.</summary>
    public void Set(int code, MonoBitmap bitmap)
    {
        FontTable.CheckCode(code);
        FontTable.CheckSize(Cell, bitmap);
        if (_glyphs[code] is null)
        {
            Count++;
        }

        _glyphs[code] = bitmap.Clone();
    }

    internal MonoBitmap? GetShared(int code) => _glyphs[code];
}
