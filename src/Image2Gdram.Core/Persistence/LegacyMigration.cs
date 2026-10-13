namespace Image2Gdram.Core.Persistence;

/// <summary>
/// Миграции файлов прежних версий формата (N-69): версия 1 хранила смещение Y по экранным осям
/// (положительное — вниз), версия 2 — по математическим (положительное — вверх).
/// </summary>
internal static class LegacyMigration
{
    /// <summary>Версия формата, в которой смещение Y хранится по экранным осям (положительное — вниз).</summary>
    public const int ScreenAxesOffsetYVersion = 1;

    /// <summary>Меняет знак смещения Y в DTO проекта версии <see cref="ScreenAxesOffsetYVersion"/> до валидации модели.</summary>
    public static void FlipOffsetY(ProjectFileDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        FlipProcessing(dto.Image?.Parameters?.Processing);
        FlipFont(dto.Font?.Parameters);
    }

    /// <summary>Меняет знак смещения Y в DTO настроек версии <see cref="ScreenAxesOffsetYVersion"/> до валидации модели.</summary>
    public static void FlipOffsetY(SettingsFileDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        FlipProcessing(dto.Image?.Parameters?.Processing);
        FlipFont(dto.Font?.Parameters);
    }

    private static void FlipProcessing(ProcessingDto? processing)
    {
        if (processing is not null)
        {
            processing.OffsetY = -processing.OffsetY;
        }
    }

    private static void FlipFont(FontParametersDto? font)
    {
        if (font is not null)
        {
            font.GlyphOffsetY = -font.GlyphOffsetY;
        }
    }
}
