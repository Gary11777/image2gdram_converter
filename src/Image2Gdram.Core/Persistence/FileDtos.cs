using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Persistence;

internal sealed class PresetFileDto
{
    public int FormatVersion { get; set; }

    public List<PresetDto>? Presets { get; set; }
}

internal sealed class PresetDto
{
    public string? Name { get; set; }

    public string? Controller { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public ColorScheme ColorScheme { get; set; }

    public PackingDto? Packing { get; set; }
}

internal sealed class PackingDto
{
    public PixelFormat PixelFormat { get; set; } = PixelFormat.Mono1bpp;

    public PackDirection Direction { get; set; } = PackDirection.Vertical;

    public BitOrder BitOrder { get; set; } = BitOrder.LsbFirst;

    public int BitsPerByte { get; set; } = 8;

    public PageTraversal PageTraversal { get; set; } = PageTraversal.ByPages;

    public bool Invert { get; set; }

    public ByteOrder ByteOrder { get; set; } = ByteOrder.BigEndian;
}

internal sealed class ProcessingDto
{
    public BackgroundColor Background { get; set; } = BackgroundColor.White;

    public Rotation Rotation { get; set; } = Rotation.Rotate0;

    public bool FlipHorizontal { get; set; }

    public bool FlipVertical { get; set; }

    public TargetSizeMode SizeMode { get; set; } = TargetSizeMode.Manual;

    public int TargetWidth { get; set; } = 128;

    public int TargetHeight { get; set; } = 64;

    public FitMode Fit { get; set; } = FitMode.Fit;

    public Alignment Alignment { get; set; } = Alignment.Center;

    public int OffsetX { get; set; }

    public int OffsetY { get; set; }

    public ResampleMode Resample { get; set; } = ResampleMode.AreaAverage;

    public BinarizeMode Binarize { get; set; } = BinarizeMode.Threshold;

    public int Threshold { get; set; } = ProcessingOptions.DefaultThreshold;
}

internal sealed class OutputDto
{
    public OutputFormat Format { get; set; } = OutputFormat.CKeilC51;

    public string? ArrayName { get; set; } = "image";

    public OutputEncoding Encoding { get; set; } = OutputEncoding.Cp1251;

    public int BytesPerLine { get; set; } = 16;

    public AsmNumberFormat AsmNumberFormat { get; set; } = AsmNumberFormat.Hex;

    public AsmFileExtension AsmFileExtension { get; set; } = AsmFileExtension.A51;

    public Stm32ElementType Stm32ElementType { get; set; } = Stm32ElementType.Uint8T;

    public bool IncludeDate { get; set; } = true;
}

internal sealed class PresetBindingDto
{
    public string? Name { get; set; }

    public string? BasedOn { get; set; }
}

internal sealed class SheetDto
{
    public int CellWidth { get; set; } = 6;

    public int CellHeight { get; set; } = 8;

    public int MarginX { get; set; }

    public int MarginY { get; set; }

    public int SpacingX { get; set; }

    public int SpacingY { get; set; }

    public int CharsPerRow { get; set; } = 16;

    public int FirstCode { get; set; }

    public int Threshold { get; set; } = 128;
}

internal sealed class ImageParametersDto
{
    public int SelectedFrame { get; set; } = 1;

    public bool ExportAllFrames { get; set; }

    public ColorScheme ColorScheme { get; set; } = ColorScheme.Oled;

    public PresetBindingDto? Preset { get; set; }

    public ProcessingDto? Processing { get; set; }

    public PackingDto? Packing { get; set; }

    public OutputDto? Output { get; set; }
}

internal sealed class FontParametersDto
{
    public int CellWidth { get; set; } = 6;

    public int CellHeight { get; set; } = 8;

    public FontSourceKind SourceKind { get; set; } = FontSourceKind.TrueType;

    public string? Family { get; set; }

    public int FontSizePx { get; set; } = 8;

    public bool Bold { get; set; }

    public bool Italic { get; set; }

    public int GlyphOffsetX { get; set; }

    public int GlyphOffsetY { get; set; }

    public GlyphRenderMode RenderMode { get; set; } = GlyphRenderMode.Antialiased;

    public int GlyphThreshold { get; set; } = 128;

    public SheetDto? Sheet { get; set; }

    public string? ImportArrayName { get; set; }

    public List<string>? RangePresets { get; set; }

    public List<int>? CustomCodes { get; set; }

    public string? PreviewText { get; set; }

    public ColorScheme ColorScheme { get; set; } = ColorScheme.Oled;

    public PresetBindingDto? Preset { get; set; }

    public PackingDto? Packing { get; set; }

    public OutputDto? Output { get; set; }
}

internal sealed class SettingsFileDto
{
    public int FormatVersion { get; set; }

    public string? Language { get; set; }

    public ImageSettingsDto? Image { get; set; }

    public FontSettingsDto? Font { get; set; }

    public List<PresetDto>? UserPresets { get; set; }

    public List<string>? RecentFiles { get; set; }

    public FoldersDto? Folders { get; set; }
}

internal sealed class ImageSettingsDto
{
    public string? SourcePath { get; set; }

    public ImageParametersDto? Parameters { get; set; }
}

internal sealed class FontSettingsDto
{
    public string? SheetPath { get; set; }

    public string? ImportPath { get; set; }

    public FontParametersDto? Parameters { get; set; }
}

internal sealed class FoldersDto
{
    public string? OpenImage { get; set; }

    public string? OpenFont { get; set; }

    public string? SaveOutput { get; set; }

    public string? OpenProject { get; set; }

    public string? SaveProject { get; set; }
}

internal sealed class ProjectFileDto
{
    public int FormatVersion { get; set; }

    public ProjectImageDto? Image { get; set; }

    public ProjectFontDto? Font { get; set; }
}

internal sealed class ProjectImageDto
{
    public ImageParametersDto? Parameters { get; set; }

    public StoredPathDto? Source { get; set; }

    public List<FrameEditsDto>? Edits { get; set; }
}

internal sealed class ProjectFontDto
{
    public FontParametersDto? Parameters { get; set; }

    public StoredPathDto? Sheet { get; set; }

    public StoredPathDto? Import { get; set; }

    public List<GlyphDto>? Glyphs { get; set; }
}

internal sealed class StoredPathDto
{
    public string? Absolute { get; set; }

    public string? Relative { get; set; }
}

internal sealed class FrameEditsDto
{
    public int Frame { get; set; }

    public List<PixelDto>? Pixels { get; set; }
}

internal sealed class PixelDto
{
    public int X { get; set; }

    public int Y { get; set; }

    public bool Active { get; set; }
}

internal sealed class GlyphDto
{
    public string? Source { get; set; }

    public string? Manual { get; set; }
}
