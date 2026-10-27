using System.ComponentModel;
using System.Globalization;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

/// <summary>N-67: смещение не больше 80 % ширины и высоты.</summary>
public class OffsetLimitTests : IDisposable
{
    private static readonly Dictionary<string, string> Text = TestFiles.LoadUiStrings();
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-limit-" + Guid.NewGuid().ToString("N"));

    public OffsetLimitTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Glyph_offset_follows_80_percent_of_the_6x8_cell()
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.SourceKind = FontSourceKind.TrueType;
        Assert.Equal(FontCellSize.Cell6x8, vm.Cell);

        vm.OffsetXText = "4";
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        vm.OffsetXText = "5";
        Assert.Equal(Format("Error.GlyphOffsetX", 4, 6), Assert.Single(Errors(vm, nameof(vm.OffsetXText))));
        Assert.False(vm.CanCopy);
        Assert.False(vm.CanSaveOutput);
        Assert.Equal(vm.CodeText, Assert.Single(Errors(vm, nameof(vm.OffsetXText))));

        vm.OffsetXText = "0";
        vm.OffsetYText = "6";
        Assert.Empty(Errors(vm, nameof(vm.OffsetYText)));
        Assert.True(vm.CanCopy);
        vm.OffsetYText = "7";
        Assert.Equal(Format("Error.GlyphOffsetY", 6, 8), Assert.Single(Errors(vm, nameof(vm.OffsetYText))));
        Assert.False(vm.CanCopy);
    }

    [Fact]
    public void Changing_the_cell_clamps_glyph_offsets()
    {
        var dialogs = new FakeDialogs();
        FontGeneratorViewModel vm = CreateFont(dialogs);
        vm.Cell = FontCellSize.Cell12x16;
        vm.OffsetXText = "9";
        vm.OffsetYText = "12";
        Assert.Equal(0, dialogs.ConfirmCount);

        vm.Cell = FontCellSize.Cell6x8;

        Assert.Equal(0, dialogs.ConfirmCount);
        Assert.Equal(FontCellSize.Cell6x8, vm.Cell);
        Assert.Equal("4", vm.OffsetXText);
        Assert.Equal("6", vm.OffsetYText);
        Assert.Equal(4, vm.ToParameters().GlyphOffsetX);
        Assert.Equal(6, vm.ToParameters().GlyphOffsetY);
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        Assert.Empty(Errors(vm, nameof(vm.OffsetYText)));
    }

    [Fact]
    public void Refusing_the_cell_change_keeps_glyph_offsets()
    {
        var dialogs = new FakeDialogs { ConfirmAnswer = false };
        FontGeneratorViewModel vm = CreateFont(dialogs);
        vm.Cell = FontCellSize.Cell12x16;
        vm.OffsetXText = "9";
        vm.OffsetYText = "12";
        vm.Editor.ApplyStroke(new[] { (0, 0) }, StrokePaint.Toggle);

        vm.Cell = FontCellSize.Cell6x8;

        Assert.Equal(1, dialogs.ConfirmCount);
        Assert.Equal(FontCellSize.Cell12x16, vm.Cell);
        Assert.Equal("9", vm.OffsetXText);
        Assert.Equal("12", vm.OffsetYText);
    }

    [Fact]
    public void Cell_change_clamps_glyph_offsets_for_a_non_truetype_source()
    {
        FontGeneratorViewModel vm = CreateFont();
        vm.Cell = FontCellSize.Cell12x16;
        vm.OffsetXText = "9";
        vm.OffsetYText = "12";
        vm.SourceKind = FontSourceKind.Manual;

        vm.Cell = FontCellSize.Cell6x8;

        Assert.Equal(4, vm.ToParameters().GlyphOffsetX);
        Assert.Equal(6, vm.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Loading_glyph_offset_20_on_an_8x8_cell_clamps_to_6()
    {
        FontGeneratorViewModel vm = CreateFont();
        FontTabParameters parameters = vm.ToParameters() with
        {
            CellWidth = 8,
            CellHeight = 8,
            GlyphOffsetX = 20,
            GlyphOffsetY = -20,
        };

        vm.Load(new FontProjectTab(parameters, new FontTable(FontCellSize.Cell8x8)));

        Assert.Equal(FontCellSize.Cell8x8, vm.Cell);
        Assert.Equal("6", vm.OffsetXText);
        Assert.Equal("-6", vm.OffsetYText);
        Assert.Equal(6, vm.ToParameters().GlyphOffsetX);
        Assert.Equal(-6, vm.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Loading_a_smaller_table_cell_clamps_glyph_offsets()
    {
        FontGeneratorViewModel vm = CreateFont();
        FontTabParameters parameters = vm.ToParameters() with
        {
            CellWidth = 12,
            CellHeight = 16,
            GlyphOffsetX = 9,
            GlyphOffsetY = 12,
        };

        vm.Load(new FontProjectTab(parameters, new FontTable(FontCellSize.Cell6x8)));

        Assert.Equal(FontCellSize.Cell6x8, vm.Cell);
        Assert.Equal(4, vm.ToParameters().GlyphOffsetX);
        Assert.Equal(6, vm.ToParameters().GlyphOffsetY);
    }

    [Fact]
    public void Import_that_changes_the_cell_clamps_glyph_offsets()
    {
        var dialogs = new FakeDialogs
        {
            ImportAnswer = new ImportPick(1, FontCellSize.Cell6x8, PackingOptions.Default),
        };
        FontGeneratorViewModel vm = CreateFont(dialogs);
        vm.Cell = FontCellSize.Cell12x16;
        vm.OffsetXText = "9";
        vm.OffsetYText = "12";
        string path = Path.Combine(_directory, "two.c");
        File.WriteAllText(path, "unsigned char one[] = { 0x01 };\nunsigned char two[] = { 0x80 };\n");

        vm.ImportFile(path);

        Assert.Equal(FontCellSize.Cell6x8, vm.Cell);
        Assert.Equal("4", vm.OffsetXText);
        Assert.Equal("6", vm.OffsetYText);
        Assert.Equal(4, vm.ToParameters().GlyphOffsetX);
        Assert.Equal(6, vm.ToParameters().GlyphOffsetY);
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

    private static string Format(string key, int limit, int side) =>
        string.Format(CultureInfo.InvariantCulture, Text[key], limit, side);

    private static IReadOnlyList<string> Errors(INotifyDataErrorInfo vm, string property) =>
        vm.GetErrors(property).Cast<object>().Select(error => error.ToString()!).ToArray();

    private FontGeneratorViewModel CreateFont(FakeDialogs? dialogs = null)
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        var vm = new FontGeneratorViewModel(
            new MapText(Text),
            dialogs ?? new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            new StubOutlines(),
            PresetCatalog.Shared,
            settings.UserPresets,
            settings.Font);
        vm.Family = "Stub";
        return vm;
    }

    private MainViewModel CreateMain(FakeDialogs dialogs, FakeFiles files, IImageDecoder decoder)
    {
        var service = new SettingsService(_directory, _directory);
        SettingsLoadResult loaded = service.Load();
        return new MainViewModel(
            service,
            loaded,
            new MapText(Text),
            dialogs,
            files,
            new FakeClipboard(),
            () => new ImmediateRecalcScheduler(),
            new ManualSettingsAutosave(),
            decoder,
            new StubOutlines());
    }
}
