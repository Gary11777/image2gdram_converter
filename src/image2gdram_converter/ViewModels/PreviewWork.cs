using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;

namespace image2gdram_converter.ViewModels;

internal enum PreviewKind
{
    /// <summary>Посчитать конвейер и код.</summary>
    Run,

    /// <summary>Оставить сетку и показать причину вместо кода.</summary>
    KeepAndBlock,

    /// <summary>Очистить сетку и показать сообщение.</summary>
    Clear,
}

internal sealed class PreviewRequest
{
    public PreviewKind Kind { get; init; }

    public string? Message { get; init; }

    public ProcessingOptions Processing { get; init; } = ProcessingOptions.Default;

    public PackingOptions Packing { get; init; } = PackingOptions.Default;

    public OutputOptions Output { get; init; } = OutputOptions.Default;

    public PresetInfo Preset { get; init; } = PresetInfo.Custom;

    public DecodedImage? Image { get; init; }

    public string? SourcePath { get; init; }

    public int FrameIndex { get; init; }

    public bool ExportAll { get; init; }

    public FramePixelOverrides Edits { get; init; } = new();
}

internal sealed class PreviewResult
{
    public PreviewKind Kind { get; init; }

    public string? Message { get; init; }

    public MonoBitmap? Bitmap { get; init; }

    public RgbaImage? Source { get; init; }

    public byte[]? FrameBytes { get; init; }

    public int FrameSize { get; init; }

    public bool ExportAll { get; init; }

    public int FrameNumber { get; init; }

    public OutputDocument? Document { get; init; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = Array.Empty<Diagnostic>();

    public int Width { get; init; }

    public int Height { get; init; }
}
