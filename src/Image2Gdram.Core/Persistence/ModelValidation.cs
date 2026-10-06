using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;

namespace Image2Gdram.Core.Persistence;

/// <summary>Границы параметров при чтении и записи. Те же диапазоны, что у конвейера и источников шрифта.</summary>
internal static class ModelValidation
{
    public static void Ensure(ImageTabParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.Processing);
        ArgumentNullException.ThrowIfNull(parameters.Packing);
        ArgumentNullException.ThrowIfNull(parameters.Output);
        ArgumentNullException.ThrowIfNull(parameters.Preset);
        Ensure(parameters.Processing);
        Ensure(parameters.Packing);
        Ensure(parameters.Output);
        EnsureScheme(parameters.ColorScheme);
        if (parameters.SelectedFrame < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.SelectedFrame, "Frame number starts at 1.");
        }
    }

    public static void Ensure(FontTabParameters parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(parameters.Packing);
        ArgumentNullException.ThrowIfNull(parameters.Output);
        ArgumentNullException.ThrowIfNull(parameters.Sheet);
        ArgumentNullException.ThrowIfNull(parameters.Preset);
        ArgumentNullException.ThrowIfNull(parameters.Family);
        ArgumentNullException.ThrowIfNull(parameters.PreviewText);
        ArgumentNullException.ThrowIfNull(parameters.CustomCodes);
        if (!FontCellSize.TryGet(parameters.CellWidth, parameters.CellHeight, out _))
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), $"{parameters.CellWidth}x{parameters.CellHeight}", "Font cell must be 6x8, 8x8 or 12x16.");
        }

        if (!Enum.IsDefined(parameters.SourceKind))
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.SourceKind, "Unknown font source.");
        }

        if (parameters.FontSizePx is < FontFaceSpec.MinSizePx or > FontFaceSpec.MaxSizePx)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.FontSizePx, "Font size must be in 1..64.");
        }

        if (parameters.GlyphOffsetX is < TrueTypeOptions.MinOffset or > TrueTypeOptions.MaxOffset
            || parameters.GlyphOffsetY is < TrueTypeOptions.MinOffset or > TrueTypeOptions.MaxOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), "Glyph offset must be in -32..32.");
        }

        if (!Enum.IsDefined(parameters.RenderMode))
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.RenderMode, "Unknown render mode.");
        }

        if (parameters.GlyphThreshold is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(parameters), parameters.GlyphThreshold, "Threshold must be in 0..255.");
        }

        Ensure(parameters.Sheet);
        Ensure(parameters.Packing);
        Ensure(parameters.Output);
        EnsureScheme(parameters.ColorScheme);
        EnsureRanges(parameters.RangePresets, parameters.CustomCodes);
    }

    private static void Ensure(ProcessingOptions options)
    {
        if (!Enum.IsDefined(options.Background) || !Enum.IsDefined(options.Rotation) || !Enum.IsDefined(options.SizeMode)
            || !Enum.IsDefined(options.Fit) || !Enum.IsDefined(options.Alignment) || !Enum.IsDefined(options.Resample)
            || !Enum.IsDefined(options.Binarize))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Processing options contain an undefined enum value.");
        }

        if (options.Threshold is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(options), options.Threshold, "Threshold must be in 0..255.");
        }

        if (options.OffsetX is < ProcessingOptions.MinOffset or > ProcessingOptions.MaxOffset
            || options.OffsetY is < ProcessingOptions.MinOffset or > ProcessingOptions.MaxOffset)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Offset must be in -1024..1024.");
        }

        if (options.SizeMode == TargetSizeMode.Manual
            && (options.TargetWidth is < ProcessingOptions.MinTargetSide or > ProcessingOptions.MaxTargetSide
                || options.TargetHeight is < ProcessingOptions.MinTargetSide or > ProcessingOptions.MaxTargetSide))
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Target size must be in 1..1024.");
        }
    }

    private static void Ensure(PackingOptions packing)
    {
        if (!Enum.IsDefined(packing.PixelFormat) || !Enum.IsDefined(packing.Direction) || !Enum.IsDefined(packing.BitOrder)
            || !Enum.IsDefined(packing.PageTraversal) || !Enum.IsDefined(packing.ByteOrder)
            || packing.BitsPerByte is not (6 or 8))
        {
            throw new ArgumentOutOfRangeException(nameof(packing), "Packing options are invalid.");
        }
    }

    private static void Ensure(OutputOptions output)
    {
        if (output.ArrayName is null)
        {
            throw new ArgumentNullException(nameof(output), "Array name is missing.");
        }

        if (!Enum.IsDefined(output.Format) || !Enum.IsDefined(output.Encoding)
            || !Enum.IsDefined(output.AsmNumberFormat) || !Enum.IsDefined(output.AsmFileExtension)
            || !Enum.IsDefined(output.Stm32ElementType)
            || output.BytesPerLine is < OutputOptions.MinBytesPerLine or > OutputOptions.MaxBytesPerLine)
        {
            throw new ArgumentOutOfRangeException(nameof(output), "Output options are invalid.");
        }
    }

    private static void Ensure(SheetOptions sheet)
    {
        if (sheet.CellWidth is < 1 or > 64 || sheet.CellHeight is < 1 or > 64
            || sheet.MarginX is < 0 or > 1024 || sheet.MarginY is < 0 or > 1024
            || sheet.SpacingX is < 0 or > 256 || sheet.SpacingY is < 0 or > 256
            || sheet.CharsPerRow is < 1 or > 256
            || sheet.FirstCode is < 0 or > 0xFF
            || sheet.Threshold is < 0 or > 255)
        {
            throw new ArgumentOutOfRangeException(nameof(sheet), "Sheet options are outside their ranges.");
        }
    }

    private static void EnsureScheme(ColorScheme scheme)
    {
        if (!Enum.IsDefined(scheme))
        {
            throw new ArgumentOutOfRangeException(nameof(scheme), scheme, "Unknown color scheme.");
        }
    }

    private static void EnsureRanges(CharRangePreset presets, IReadOnlyList<int> customCodes)
    {
        if ((presets & ~(CharRangePreset.Latin | CharRangePreset.Cyrillic | CharRangePreset.OtherCp1251)) != 0)
        {
            throw new ArgumentOutOfRangeException(nameof(presets), presets, "Unknown character range.");
        }

        var seen = new HashSet<int>();
        foreach (int code in customCodes)
        {
            if (code is < 0 or > 0xFF || !seen.Add(code))
            {
                throw new ArgumentOutOfRangeException(nameof(customCodes), code, "Custom character codes must be unique and in 0..255.");
            }
        }
    }
}
