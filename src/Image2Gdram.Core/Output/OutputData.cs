using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Output;

/// <summary>Упакованные данные и сведения для заголовка-комментария; вход генераторов вывода.</summary>
public abstract class OutputData
{
    private protected OutputData(PackingOptions packing, PresetInfo preset)
    {
        ArgumentNullException.ThrowIfNull(packing);
        ArgumentNullException.ThrowIfNull(preset);
        Packing = packing;
        Preset = preset;
    }

    public PackingOptions Packing { get; }

    public PresetInfo Preset { get; }

    /// <summary>Общий размер массива в байтах.</summary>
    public abstract int TotalBytes { get; }

    /// <summary>Все байты массива подряд (для BIN и карты позиций).</summary>
    public abstract byte[] GetAllBytes();
}

/// <summary>Исходный файл изображения для строки «Источник» (решение N-02).</summary>
public sealed record ImageSourceInfo
{
    public ImageSourceInfo(string fileName, int sourceFrameCount = 1, int selectedFrame = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentOutOfRangeException.ThrowIfLessThan(sourceFrameCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(selectedFrame, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(selectedFrame, sourceFrameCount);
        FileName = Path.GetFileName(fileName);
        SourceFrameCount = sourceFrameCount;
        SelectedFrame = selectedFrame;
    }

    /// <summary>Только имя файла, без пути.</summary>
    public string FileName { get; }

    /// <summary>Число кадров в исходнике (больше 1 — анимированный GIF).</summary>
    public int SourceFrameCount { get; }

    /// <summary>Выбранный кадр, нумерация с 1 (решение N-20).</summary>
    public int SelectedFrame { get; }
}

/// <summary>
/// Изображение: один кадр или все кадры анимированного GIF (п. 4.1.1 ТЗ, решение D-11).
/// Каждый кадр — упакованный растр <see cref="Width"/>×<see cref="Height"/>.
/// </summary>
public sealed class ImageOutputData : OutputData
{
    private readonly byte[][] _frames;

    public ImageOutputData(int width, int height, byte[] data, PackingOptions packing, ImageSourceInfo source, PresetInfo preset)
        : this(width, height, new[] { data }, allFrames: false, packing, source, preset)
    {
    }

    public ImageOutputData(
        int width,
        int height,
        IReadOnlyList<byte[]> frames,
        bool allFrames,
        PackingOptions packing,
        ImageSourceInfo source,
        PresetInfo preset)
        : base(packing, preset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentNullException.ThrowIfNull(frames);
        ArgumentNullException.ThrowIfNull(source);
        if (frames.Count == 0)
        {
            throw new ArgumentException("At least one frame is required.", nameof(frames));
        }

        if (!allFrames && frames.Count != 1)
        {
            throw new ArgumentException("A single-frame export must contain exactly one frame.", nameof(frames));
        }

        int frameSize = frames[0]?.Length ?? 0;
        if (frameSize == 0 || frames.Any(f => f is null || f.Length != frameSize))
        {
            throw new ArgumentException("All frames must be non-empty and of the same length.", nameof(frames));
        }

        if ((long)frameSize * frames.Count > int.MaxValue)
        {
            throw new ArgumentException("The array is too large.", nameof(frames));
        }

        Width = width;
        Height = height;
        _frames = frames.Select(f => (byte[])f.Clone()).ToArray();
        AllFrames = allFrames;
        Source = source;
    }

    public int Width { get; }

    public int Height { get; }

    /// <summary>Экспорт всех кадров двумерным массивом <c>[число кадров][размер кадра]</c>.</summary>
    public bool AllFrames { get; }

    public ImageSourceInfo Source { get; }

    public int FrameCount => _frames.Length;

    public int FrameSize => _frames[0].Length;

    public override int TotalBytes => FrameSize * FrameCount;

    public ReadOnlySpan<byte> GetFrame(int index) => _frames[index];

    public override byte[] GetAllBytes()
    {
        var all = new byte[TotalBytes];
        for (int i = 0; i < _frames.Length; i++)
        {
            _frames[i].CopyTo(all, i * FrameSize);
        }

        return all;
    }
}

public enum FontSourceKind
{
    /// <summary>Шрифт TrueType / OpenType Windows.</summary>
    TrueType,

    /// <summary>Растровый лист символов.</summary>
    Sheet,

    /// <summary>Импорт массива из файла.</summary>
    Import,

    /// <summary>Ручное рисование, без источника.</summary>
    Manual,
}

/// <summary>Источник шрифта для строки «Шрифт» (решение N-02).</summary>
public sealed record FontSourceInfo
{
    private FontSourceInfo(FontSourceKind kind)
    {
        Kind = kind;
    }

    public static FontSourceInfo Manual { get; } = new(FontSourceKind.Manual);

    public FontSourceKind Kind { get; }

    /// <summary>Гарнитура (TrueType).</summary>
    public string? Family { get; private init; }

    /// <summary>Размер в пикселях (TrueType).</summary>
    public int SizePx { get; private init; }

    public bool Bold { get; private init; }

    public bool Italic { get; private init; }

    /// <summary>Имя файла листа или импортированного файла, без пути.</summary>
    public string? FileName { get; private init; }

    /// <summary>Имя импортированного массива (идентификатор C или метка A51), если есть.</summary>
    public string? ArrayName { get; private init; }

    public static FontSourceInfo TrueType(string family, int sizePx, bool bold = false, bool italic = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(family);
        ArgumentOutOfRangeException.ThrowIfLessThan(sizePx, 1);
        return new FontSourceInfo(FontSourceKind.TrueType) { Family = family, SizePx = sizePx, Bold = bold, Italic = italic };
    }

    public static FontSourceInfo Sheet(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return new FontSourceInfo(FontSourceKind.Sheet) { FileName = Path.GetFileName(fileName) };
    }

    public static FontSourceInfo Import(string fileName, string? arrayName = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        return new FontSourceInfo(FontSourceKind.Import)
        {
            FileName = Path.GetFileName(fileName),
            ArrayName = string.IsNullOrWhiteSpace(arrayName) ? null : arrayName,
        };
    }
}

/// <summary>Таблица шрифта: 256 символов по N байт, символ c — байты c·N … c·N + N − 1 (решение F-04).</summary>
public sealed class FontOutputData : OutputData
{
    public const int CharCount = 256;

    private readonly byte[] _data;

    public FontOutputData(int cellWidth, int cellHeight, byte[] data, PackingOptions packing, FontSourceInfo source, PresetInfo preset)
        : base(packing, preset)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(cellWidth, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(cellHeight, 1);
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(source);
        if (data.Length == 0 || data.Length % CharCount != 0)
        {
            throw new ArgumentException("Font data must contain 256 glyphs of equal size.", nameof(data));
        }

        CellWidth = cellWidth;
        CellHeight = cellHeight;
        _data = (byte[])data.Clone();
        Source = source;
    }

    public int CellWidth { get; }

    public int CellHeight { get; }

    public FontSourceInfo Source { get; }

    public int BytesPerChar => _data.Length / CharCount;

    public override int TotalBytes => _data.Length;

    public ReadOnlySpan<byte> GetGlyph(int code) => _data.AsSpan(code * BytesPerChar, BytesPerChar);

    public override byte[] GetAllBytes() => (byte[])_data.Clone();
}
