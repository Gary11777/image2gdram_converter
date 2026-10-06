using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using image2gdram_converter.Services;

namespace image2gdram_converter;

/// <summary>Подписи перечислений. Каждая фраза — ключ словаря.</summary>
public static class OptionLists
{
    public static IReadOnlyList<Labeled<BackgroundColor>> Backgrounds(ILocalizationService text) => new[]
    {
        new Labeled<BackgroundColor>(BackgroundColor.White, text.Get("Opt.Background.White")),
        new Labeled<BackgroundColor>(BackgroundColor.Black, text.Get("Opt.Background.Black")),
    };

    public static IReadOnlyList<Labeled<Rotation>> Rotations(ILocalizationService text) => new[]
    {
        new Labeled<Rotation>(Rotation.Rotate0, text.Get("Opt.Rotation.0")),
        new Labeled<Rotation>(Rotation.Rotate90, text.Get("Opt.Rotation.90")),
        new Labeled<Rotation>(Rotation.Rotate180, text.Get("Opt.Rotation.180")),
        new Labeled<Rotation>(Rotation.Rotate270, text.Get("Opt.Rotation.270")),
    };

    public static IReadOnlyList<Labeled<TargetSizeMode>> SizeModes(ILocalizationService text) => new[]
    {
        new Labeled<TargetSizeMode>(TargetSizeMode.Manual, text.Get("Opt.Size.Manual")),
        new Labeled<TargetSizeMode>(TargetSizeMode.MatchSource, text.Get("Opt.Size.Match")),
    };

    public static IReadOnlyList<Labeled<FitMode>> Fits(ILocalizationService text) => new[]
    {
        new Labeled<FitMode>(FitMode.None, text.Get("Opt.Fit.None")),
        new Labeled<FitMode>(FitMode.Fit, text.Get("Opt.Fit.Fit")),
        new Labeled<FitMode>(FitMode.Stretch, text.Get("Opt.Fit.Stretch")),
        new Labeled<FitMode>(FitMode.Fill, text.Get("Opt.Fit.Fill")),
    };

    public static IReadOnlyList<Labeled<Alignment>> Alignments(ILocalizationService text) => new[]
    {
        new Labeled<Alignment>(Alignment.TopLeft, text.Get("Opt.Align.TopLeft")),
        new Labeled<Alignment>(Alignment.TopCenter, text.Get("Opt.Align.TopCenter")),
        new Labeled<Alignment>(Alignment.TopRight, text.Get("Opt.Align.TopRight")),
        new Labeled<Alignment>(Alignment.MiddleLeft, text.Get("Opt.Align.MiddleLeft")),
        new Labeled<Alignment>(Alignment.Center, text.Get("Opt.Align.Center")),
        new Labeled<Alignment>(Alignment.MiddleRight, text.Get("Opt.Align.MiddleRight")),
        new Labeled<Alignment>(Alignment.BottomLeft, text.Get("Opt.Align.BottomLeft")),
        new Labeled<Alignment>(Alignment.BottomCenter, text.Get("Opt.Align.BottomCenter")),
        new Labeled<Alignment>(Alignment.BottomRight, text.Get("Opt.Align.BottomRight")),
    };

    public static IReadOnlyList<Labeled<ResampleMode>> Resamples(ILocalizationService text) => new[]
    {
        new Labeled<ResampleMode>(ResampleMode.AreaAverage, text.Get("Opt.Resample.Area")),
        new Labeled<ResampleMode>(ResampleMode.NearestNeighbor, text.Get("Opt.Resample.Nearest")),
    };

    public static IReadOnlyList<Labeled<BinarizeMode>> BinarizeModes(ILocalizationService text) => new[]
    {
        new Labeled<BinarizeMode>(BinarizeMode.Threshold, text.Get("Opt.Bin.Threshold")),
        new Labeled<BinarizeMode>(BinarizeMode.FloydSteinberg, text.Get("Opt.Bin.Floyd")),
        new Labeled<BinarizeMode>(BinarizeMode.Atkinson, text.Get("Opt.Bin.Atkinson")),
        new Labeled<BinarizeMode>(BinarizeMode.Bayer4, text.Get("Opt.Bin.Bayer4")),
        new Labeled<BinarizeMode>(BinarizeMode.Bayer8, text.Get("Opt.Bin.Bayer8")),
    };

    public static IReadOnlyList<Labeled<PackDirection>> Directions(ILocalizationService text) => new[]
    {
        new Labeled<PackDirection>(PackDirection.Horizontal, text.Get("Opt.Dir.Horizontal")),
        new Labeled<PackDirection>(PackDirection.Vertical, text.Get("Opt.Dir.Vertical")),
    };

    public static IReadOnlyList<Labeled<BitOrder>> BitOrders(ILocalizationService text) => new[]
    {
        new Labeled<BitOrder>(BitOrder.LsbFirst, text.Get("Opt.Bit.Lsb")),
        new Labeled<BitOrder>(BitOrder.MsbFirst, text.Get("Opt.Bit.Msb")),
    };

    public static IReadOnlyList<Labeled<int>> BitCounts(ILocalizationService text) => new[]
    {
        new Labeled<int>(8, text.Get("Opt.Bits.8")),
        new Labeled<int>(6, text.Get("Opt.Bits.6")),
    };

    public static IReadOnlyList<Labeled<PageTraversal>> Traversals(ILocalizationService text) => new[]
    {
        new Labeled<PageTraversal>(PageTraversal.ByPages, text.Get("Opt.Page.ByPages")),
        new Labeled<PageTraversal>(PageTraversal.ByColumns, text.Get("Opt.Page.ByColumns")),
    };

    public static IReadOnlyList<Labeled<ColorScheme>> Schemes(ILocalizationService text) => new[]
    {
        new Labeled<ColorScheme>(ColorScheme.Lcd, text.Get("Opt.Scheme.Lcd")),
        new Labeled<ColorScheme>(ColorScheme.Oled, text.Get("Opt.Scheme.Oled")),
    };

    public static IReadOnlyList<Labeled<OutputFormat>> Formats(ILocalizationService text) => new[]
    {
        new Labeled<OutputFormat>(OutputFormat.CKeilC51, text.Get("Opt.Format.C51")),
        new Labeled<OutputFormat>(OutputFormat.CStm32, text.Get("Opt.Format.Stm32")),
        new Labeled<OutputFormat>(OutputFormat.A51Module, text.Get("Opt.Format.A51Module")),
        new Labeled<OutputFormat>(OutputFormat.A51Include, text.Get("Opt.Format.A51Include")),
        new Labeled<OutputFormat>(OutputFormat.Bin, text.Get("Opt.Format.Bin")),
    };

    public static IReadOnlyList<Labeled<OutputEncoding>> Encodings(ILocalizationService text) => new[]
    {
        new Labeled<OutputEncoding>(OutputEncoding.Cp1251, text.Get("Opt.Enc.Cp1251")),
        new Labeled<OutputEncoding>(OutputEncoding.Utf8NoBom, text.Get("Opt.Enc.Utf8")),
    };

    public static IReadOnlyList<Labeled<AsmNumberFormat>> AsmNumbers(ILocalizationService text) => new[]
    {
        new Labeled<AsmNumberFormat>(AsmNumberFormat.Hex, text.Get("Opt.AsmNum.Hex")),
        new Labeled<AsmNumberFormat>(AsmNumberFormat.Binary, text.Get("Opt.AsmNum.Binary")),
    };

    public static IReadOnlyList<Labeled<AsmFileExtension>> AsmExtensions(ILocalizationService text) => new[]
    {
        new Labeled<AsmFileExtension>(AsmFileExtension.A51, text.Get("Opt.AsmExt.A51")),
        new Labeled<AsmFileExtension>(AsmFileExtension.Asm, text.Get("Opt.AsmExt.Asm")),
    };

    public static IReadOnlyList<Labeled<Stm32ElementType>> Stm32Types(ILocalizationService text) => new[]
    {
        new Labeled<Stm32ElementType>(Stm32ElementType.Uint8T, text.Get("Opt.Stm.Uint8")),
        new Labeled<Stm32ElementType>(Stm32ElementType.UnsignedChar, text.Get("Opt.Stm.UChar")),
    };

    public static IReadOnlyList<ZoomChoice> Zooms(ILocalizationService text)
    {
        var items = new List<ZoomChoice> { new(null, text.Get("Opt.Zoom.Fit")) };
        for (int scale = GridScale.Min; scale <= GridScale.Max; scale++)
        {
            items.Add(new ZoomChoice(scale, text.Format("Opt.Zoom.Scale", scale)));
        }

        return items;
    }
}

/// <summary><c>null</c> в <see cref="Scale"/> — режим «по размеру окна».</summary>
public sealed class ZoomChoice
{
    public ZoomChoice(int? scale, string caption)
    {
        Scale = scale;
        Caption = caption;
    }

    public int? Scale { get; }

    public string Caption { get; }
}
