using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Output;

namespace Image2Gdram.Core.Fonts;

/// <summary>
/// Источник изображений символов (п. 4.2.2 ТЗ): TrueType, растровый лист или импортированный массив.
/// Ручное рисование — не источник, а переопределения в <see cref="FontTable"/> (решения D-14, N-27).
/// Параметры источника задаются при создании экземпляра; <see cref="Render"/> не меняет состояние,
/// кроме внутренних кэшей, и детерминирован.
/// </summary>
public interface IGlyphSource
{
    /// <summary>Описание источника для строки «Шрифт» заголовка-комментария (решение N-02).</summary>
    FontSourceInfo Info { get; }

    /// <summary>
    /// Строит растры символов размером <paramref name="cell"/>. TTF и лист заполняют только коды из
    /// <paramref name="ranges"/>; импорт заполняет все 256 кодов и диапазоны не учитывает (решение N-27).
    /// </summary>
    GlyphSourceResult Render(FontCellSize cell, CharRangeSet ranges, CancellationToken cancellationToken = default);
}

/// <summary>Результат источника: растры, коды без глифа (только TTF, решение N-23) и неблокирующие сообщения.</summary>
public sealed class GlyphSourceResult
{
    public GlyphSourceResult(GlyphSet glyphs, IReadOnlyList<int>? missingCodes = null, IReadOnlyList<Diagnostic>? diagnostics = null)
    {
        ArgumentNullException.ThrowIfNull(glyphs);
        Glyphs = glyphs;
        MissingCodes = missingCodes?.ToArray() ?? Array.Empty<int>();
        Diagnostics = diagnostics?.ToArray() ?? Array.Empty<Diagnostic>();
    }

    public GlyphSet Glyphs { get; }

    /// <summary>Коды из выбранных диапазонов, которых нет в шрифте, по возрастанию.</summary>
    public IReadOnlyList<int> MissingCodes { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
