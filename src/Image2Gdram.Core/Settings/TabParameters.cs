using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Settings;

/// <summary>Параметры вкладки «Конвертер картинок» без ручных правок и без открытия файла.</summary>
public sealed record ImageTabParameters
{
    public ProcessingOptions Processing { get; init; } = ProcessingOptions.Default;

    public PackingOptions Packing { get; init; } = PackingOptions.Default;

    public OutputOptions Output { get; init; } = OutputOptions.Default;

    /// <summary>По умолчанию OLED: параметры упаковки совпадают с SSD1306 (решение N-35).</summary>
    public ColorScheme ColorScheme { get; init; } = ColorScheme.Oled;

    public PresetBinding Preset { get; init; } = PresetBinding.Custom;

    /// <summary>Выбранный кадр GIF, нумерация с 1 (решение N-20).</summary>
    public int SelectedFrame { get; init; } = 1;

    public bool ExportAllFrames { get; init; }

    public static ImageTabParameters CreateDefault() => new();
}

/// <summary>Параметры вкладки «Генератор шрифтов» без таблицы символов и без открытия файлов.</summary>
public sealed record FontTabParameters
{
    public const string DefaultPreviewText = "Привет! Hello 123";

    public int CellWidth { get; init; } = 6;

    public int CellHeight { get; init; } = 8;

    public FontSourceKind SourceKind { get; init; } = FontSourceKind.TrueType;

    public string Family { get; init; } = "";

    public int FontSizePx { get; init; } = 8;

    public bool Bold { get; init; }

    public bool Italic { get; init; }

    public int GlyphOffsetX { get; init; }

    public int GlyphOffsetY { get; init; }

    public GlyphRenderMode RenderMode { get; init; } = GlyphRenderMode.Antialiased;

    public int GlyphThreshold { get; init; } = 128;

    public SheetOptions Sheet { get; init; } = new();

    public string? ImportArrayName { get; init; }

    public CharRangePreset RangePresets { get; init; } = CharRangePreset.Latin | CharRangePreset.Cyrillic;

    public IReadOnlyList<int> CustomCodes { get; init; } = Array.Empty<int>();

    public string PreviewText { get; init; } = DefaultPreviewText;

    /// <summary>Масштаб предпросмотра строки, целый 1…8 (решение N-30). По умолчанию 2.</summary>
    public int PreviewScale { get; init; } = 2;

    public PackingOptions Packing { get; init; } = PackingOptions.Default;

    public OutputOptions Output { get; init; } = OutputOptions.Default with { ArrayName = "font" };

    public ColorScheme ColorScheme { get; init; } = ColorScheme.Oled;

    public PresetBinding Preset { get; init; } = PresetBinding.Custom;

    public FontCellSize Cell => FontCellSize.Get(CellWidth, CellHeight);

    public CharRangeSet ToRanges() => new(RangePresets, CustomCodes);

    public static FontTabParameters CreateDefault() => new();
}

/// <summary>Вкладка картинок в <c>settings.json</c>: параметры и последний путь. Файл при запуске не открывается.</summary>
public sealed record ImageSettingsTab
{
    public ImageTabParameters Parameters { get; init; } = ImageTabParameters.CreateDefault();

    public string? SourcePath { get; init; }

    public static ImageSettingsTab CreateDefault() => new();
}

/// <summary>Вкладка шрифтов в <c>settings.json</c>: параметры и последние пути листа и импорта.</summary>
public sealed record FontSettingsTab
{
    public FontTabParameters Parameters { get; init; } = FontTabParameters.CreateDefault();

    public string? SheetPath { get; init; }

    public string? ImportPath { get; init; }

    public static FontSettingsTab CreateDefault() => new();
}
