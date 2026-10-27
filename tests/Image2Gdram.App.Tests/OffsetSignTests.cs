using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>N-69: знак смещения Y — плюс вверх, минус вниз — на обеих вкладках.</summary>
public class OffsetSignTests : IDisposable
{
    private static readonly Dictionary<string, string> Text = TestFiles.LoadUiStrings();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-sign-" + Guid.NewGuid().ToString("N"));

    public OffsetSignTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Glyph_offset_y_minus_moves_the_glyph_down()
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = FontSourceKind.TrueType;

        // Без смещения квадрат 2x2 занимает строки 4-5 ячейки 6x8.
        AssertSquare(vm.Table.GetGlyph(0x41), firstRow: 4);

        vm.OffsetYText = "-1";
        AssertSquare(vm.Table.GetGlyph(0x41), firstRow: 5);

        vm.OffsetYText = "1";
        AssertSquare(vm.Table.GetGlyph(0x41), firstRow: 3);
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

    private static void AssertSquare(MonoBitmap glyph, int firstRow)
    {
        // Квадрат 2x2 у левого края ячейки: столбцы 0-1, строки firstRow..firstRow+1.
        for (int y = 0; y < glyph.Height; y++)
        {
            for (int x = 0; x < glyph.Width; x++)
            {
                Assert.Equal(x is 0 or 1 && y >= firstRow && y < firstRow + 2, glyph[x, y]);
            }
        }
    }

    private static void Load(ImageConverterViewModel vm, int width, int height, string name)
    {
        var image = new RgbaImage(width, height, 255, 255, 255, 255);
        for (int y = 0; y < height / 2; y++)
        {
            for (int x = 0; x < width; x++)
            {
                image.SetPixel(x, y, 0, 0, 0, 255);
            }
        }

        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), name, resetFrame: true, userAction: false);
        vm.ArrayName = "logo";
    }

    private ImageConverterViewModel CreateImage()
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new ImageConverterViewModel(
            new MapText(Text),
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
        var vm = new FontGeneratorViewModel(
            new MapText(Text),
            new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            new SmallGlyphOutlines(),
            PresetCatalog.Shared,
            settings.UserPresets,
            settings.Font);
        vm.Family = SmallGlyphOutlines.Family;
        return vm;
    }

    /// <summary>Фейковый провайдер: квадрат 2x2 над базовой линией у левого края (строки 4-5 ячейки 6x8).</summary>
    private sealed class SmallGlyphOutlines : IGlyphOutlineProvider
    {
        public const string Family = "Sign Stub";

        public IReadOnlyList<string> GetInstalledFamilies() => new[] { Family };

        public bool TryGetMetrics(FontFaceSpec face, out FontMetrics metrics)
        {
            metrics = new FontMetrics(6, 2);
            return string.Equals(face.Family, Family, StringComparison.OrdinalIgnoreCase);
        }

        public GlyphOutline? GetOutline(FontFaceSpec face, char character)
        {
            if (!string.Equals(face.Family, Family, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (character == ' ')
            {
                return GlyphOutline.Empty;
            }

            OutlinePoint[] square =
            {
                new(0, -2),
                new(2, -2),
                new(2, 0),
                new(0, 0),
            };
            return new GlyphOutline(new[] { square });
        }
    }
}
