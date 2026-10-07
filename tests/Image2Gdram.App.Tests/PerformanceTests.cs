using System.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;
using Image2Gdram.Fonts.Wpf;
using Image2Gdram.Imaging.Wic;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

[CollectionDefinition(nameof(PerformanceCollection), DisableParallelization = true)]
public sealed class PerformanceCollection
{
}

/// <summary>
/// Страховка от регрессий времени п. 5.1 ТЗ (решение N-58): расчёт и применение результата во ViewModel
/// без паузы планировщика и отрисовки. Пределы взяты с запасом на паузу 40 мс и отрисовку,
/// полные замеры на настоящем окне делает <c>tools/UiProbe</c>.
/// </summary>
[Collection(nameof(PerformanceCollection))]
public class PerformanceTests : IDisposable
{
    private const int SmallLimit = 150;
    private const int LargeLimit = 900;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-perf-" + Guid.NewGuid().ToString("N"));

    public PerformanceTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Image_240x128_updates_within_the_limit()
    {
        ImageConverterViewModel vm = CreateImage();
        string pattern = Path.Combine(TestFiles.RepoRoot(), "testdata", "test_pattern_240x128.png");
        vm.LoadDecoded(new WicImageDecoder().Decode(pattern), pattern, resetFrame: true, userAction: false);
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "WG240128A");

        AssertMedian(SmallLimit, () => vm.Threshold = vm.Threshold == 90 ? 170 : 90);
        AssertMedian(SmallLimit, () => vm.Binarize = vm.Binarize == BinarizeMode.FloydSteinberg ? BinarizeMode.Threshold : BinarizeMode.FloydSteinberg);
        AssertMedian(SmallLimit, () => vm.WidthText = vm.WidthText == "200" ? "240" : "200");
        AssertMedian(SmallLimit, () => vm.Format = vm.Format == OutputFormat.A51Module ? OutputFormat.CKeilC51 : OutputFormat.A51Module);
    }

    [Fact]
    public void Image_1024x1024_updates_within_the_limit()
    {
        ImageConverterViewModel vm = CreateImage();
        vm.WidthText = "1024";
        vm.HeightText = "1024";
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { Photo(1024, 1024) }), "photo.png", resetFrame: true, userAction: false);

        AssertMedian(LargeLimit, () => vm.Threshold = vm.Threshold == 90 ? 170 : 90);
        AssertMedian(LargeLimit, () => vm.Binarize = vm.Binarize == BinarizeMode.FloydSteinberg ? BinarizeMode.Threshold : BinarizeMode.FloydSteinberg);
        AssertMedian(LargeLimit, () => vm.Direction = vm.Direction == PackDirection.Vertical ? PackDirection.Horizontal : PackDirection.Vertical);
        AssertMedian(LargeLimit, () => vm.WidthText = vm.WidthText == "1000" ? "1024" : "1000");
        AssertMedian(LargeLimit, () => vm.Format = vm.Format == OutputFormat.A51Module ? OutputFormat.CKeilC51 : OutputFormat.A51Module);
    }

    [Theory]
    [InlineData(6, 8, 8)]
    [InlineData(8, 8, 9)]
    [InlineData(12, 16, 16)]
    public void Font_table_updates_within_the_limit(int width, int height, int size)
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = FontSourceKind.TrueType;
        vm.Family = "Consolas";
        vm.RenderMode = GlyphRenderMode.Antialiased;
        vm.Cell = FontCellSize.Get(width, height);
        string small = size.ToString(System.Globalization.CultureInfo.InvariantCulture);
        string large = (size + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
        vm.FontSizeText = small;
        Assert.NotNull(vm.Document);

        AssertMedian(SmallLimit, () => vm.FontSizeText = vm.FontSizeText == small ? large : small);
        AssertMedian(SmallLimit, () => vm.GlyphThreshold = vm.GlyphThreshold == 90 ? 170 : 90);
        AssertMedian(SmallLimit, () => vm.Invert = !vm.Invert);
        AssertMedian(SmallLimit, () => vm.Format = vm.Format == OutputFormat.A51Module ? OutputFormat.CKeilC51 : OutputFormat.A51Module);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private static void AssertMedian(int limit, Action change)
    {
        change();
        change();
        var times = new double[7];
        for (int i = 0; i < times.Length; i++)
        {
            var watch = Stopwatch.StartNew();
            change();
            times[i] = watch.Elapsed.TotalMilliseconds;
        }

        Array.Sort(times);
        double median = times[times.Length / 2];
        Assert.True(median <= limit, FormattableString.Invariant($"Median {median:F0} ms exceeds {limit} ms."));
    }

    private static RgbaImage Photo(int width, int height)
    {
        var image = new RgbaImage(width, height);
        var random = new Random(20261007);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int noise = random.Next(-24, 25);
                image.SetPixel(
                    x,
                    y,
                    (byte)Math.Clamp((x * 255 / width) + noise, 0, 255),
                    (byte)Math.Clamp((y * 255 / height) + noise, 0, 255),
                    (byte)Math.Clamp(128 + noise, 0, 255),
                    255);
            }
        }

        return image;
    }

    private ImageConverterViewModel CreateImage()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new ImageConverterViewModel(
            new MapText(TestFiles.LoadUiStrings()),
            new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            ImageTabParameters.CreateDefault());
    }

    private FontGeneratorViewModel CreateFont()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new FontGeneratorViewModel(
            new MapText(TestFiles.LoadUiStrings()),
            new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            new WpfGlyphOutlineProvider(),
            PresetCatalog.Shared,
            settings.UserPresets,
            settings.Font);
    }
}
