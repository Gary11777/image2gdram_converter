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
    public void Manual_128x64_accepts_the_boundary_and_blocks_one_past_it()
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);

        vm.OffsetXText = "102";
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        vm.OffsetXText = "-102";
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        Assert.True(vm.CanCopy);

        vm.OffsetXText = "103";
        string xMessage = Assert.Single(Errors(vm, nameof(vm.OffsetXText)));
        Assert.Equal(Format("Error.OffsetX", 102, 128), xMessage);
        Assert.False(vm.CanCopy);
        Assert.False(vm.CanSaveOutput);
        Assert.False(vm.CopyCommand.CanExecute(null));
        Assert.False(vm.SaveOutputCommand.CanExecute(null));
        Assert.Equal(xMessage, vm.CodeText);

        vm.OffsetXText = "-103";
        Assert.Equal(Format("Error.OffsetX", 102, 128), Assert.Single(Errors(vm, nameof(vm.OffsetXText))));
        Assert.False(vm.CanCopy);

        vm.OffsetXText = "0";
        vm.OffsetYText = "51";
        Assert.Empty(Errors(vm, nameof(vm.OffsetYText)));
        Assert.True(vm.CanCopy);

        vm.OffsetYText = "52";
        Assert.Equal(Format("Error.OffsetY", 51, 64), Assert.Single(Errors(vm, nameof(vm.OffsetYText))));
        Assert.False(vm.CanCopy);
        Assert.False(vm.CanSaveOutput);
    }

    [Theory]
    [InlineData("100", "51", 51)]
    [InlineData("-100", "-51", -51)]
    public void Shrinking_width_clamps_the_offset_in_the_same_change(string before, string after, int pipeline)
    {
        var dialogs = new FakeDialogs();
        ImageConverterViewModel vm = CreateImage(dialogs);
        LoadWhite(vm);
        vm.OffsetXText = before;
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        Assert.True(vm.HasEdits);
        Assert.Equal(0, dialogs.ConfirmCount);

        vm.WidthText = "64";

        Assert.Equal(1, dialogs.ConfirmCount);
        Assert.Equal("64", vm.WidthText);
        Assert.Equal(after, vm.OffsetXText);
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        Assert.Equal(pipeline, vm.ToParameters().Processing.OffsetX);
        Assert.False(vm.HasEdits);
        Assert.True(vm.CanCopy);
        Assert.StartsWith("/*", vm.CodeText, StringComparison.Ordinal);
    }

    [Fact]
    public void Refusing_the_width_change_keeps_the_offset()
    {
        var dialogs = new FakeDialogs { ConfirmAnswer = false };
        ImageConverterViewModel vm = CreateImage(dialogs);
        LoadWhite(vm);
        vm.OffsetXText = "-100";
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);

        vm.WidthText = "64";

        Assert.Equal(1, dialogs.ConfirmCount);
        Assert.Equal("128", vm.WidthText);
        Assert.Equal("-100", vm.OffsetXText);
        Assert.Equal(-100, vm.ToParameters().Processing.OffsetX);
        Assert.True(vm.HasEdits);
    }

    [Fact]
    public void Growing_width_does_not_change_the_offset()
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.WidthText = "64";
        vm.OffsetXText = "51";

        vm.WidthText = "128";

        Assert.Equal("51", vm.OffsetXText);
        Assert.Equal(51, vm.ToParameters().Processing.OffsetX);
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
    }

    [Fact]
    public void Invalid_offset_text_survives_a_width_change_and_restore_uses_the_clamped_value()
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.OffsetXText = "100";
        vm.OffsetXText = "103";
        Assert.Equal(Format("Error.OffsetX", 102, 128), Assert.Single(Errors(vm, nameof(vm.OffsetXText))));

        vm.WidthText = "64";

        Assert.Equal("103", vm.OffsetXText);
        Assert.Equal(Format("Error.OffsetX", 51, 64), Assert.Single(Errors(vm, nameof(vm.OffsetXText))));
        Assert.Equal(51, vm.ToParameters().Processing.OffsetX);
        Assert.False(vm.CanCopy);

        vm.RestoreOffsetsCommand.Execute(null);

        Assert.Equal("51", vm.OffsetXText);
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        Assert.Equal(51, vm.ToParameters().Processing.OffsetX);
        Assert.True(vm.CanCopy);
    }

    [Fact]
    public void Match_source_uses_the_picture_after_rotation_and_the_fields_without_one()
    {
        ImageConverterViewModel vm = CreateImage();
        vm.SizeMode = TargetSizeMode.MatchSource;
        vm.OffsetXText = "102";
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        vm.OffsetXText = "103";
        Assert.Equal(Format("Error.OffsetX", 102, 128), Assert.Single(Errors(vm, nameof(vm.OffsetXText))));
        vm.OffsetXText = "0";

        Load(vm, 200, 100, "wide.png");
        vm.OffsetXText = "160";
        vm.OffsetYText = "80";
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        Assert.Empty(Errors(vm, nameof(vm.OffsetYText)));
        vm.OffsetXText = "161";
        Assert.Equal(Format("Error.OffsetX", 160, 200), Assert.Single(Errors(vm, nameof(vm.OffsetXText))));
        vm.OffsetXText = "160";

        vm.Rotation = Rotation.Rotate90;

        Assert.Equal(Rotation.Rotate90, vm.Rotation);
        Assert.Equal("80", vm.OffsetXText);
        Assert.Equal(80, vm.ToParameters().Processing.OffsetX);
        Assert.Equal("80", vm.OffsetYText);
        vm.OffsetYText = "160";
        Assert.Empty(Errors(vm, nameof(vm.OffsetYText)));
        vm.OffsetYText = "161";
        Assert.Equal(Format("Error.OffsetY", 160, 200), Assert.Single(Errors(vm, nameof(vm.OffsetYText))));
        vm.OffsetYText = "80";

        vm.UnloadPixels();

        Assert.Equal("80", vm.OffsetXText);
        Assert.Equal("51", vm.OffsetYText);
        Assert.Equal(80, vm.ToParameters().Processing.OffsetX);
        Assert.Equal(51, vm.ToParameters().Processing.OffsetY);
    }

    [Fact]
    public void Refusing_rotation_or_size_mode_keeps_the_offset()
    {
        var dialogs = new FakeDialogs { ConfirmAnswer = false };
        ImageConverterViewModel vm = CreateImage(dialogs);
        Load(vm, 200, 100, "wide.png");
        vm.SizeMode = TargetSizeMode.MatchSource;
        vm.OffsetXText = "160";
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);

        vm.Rotation = Rotation.Rotate90;

        Assert.Equal(1, dialogs.ConfirmCount);
        Assert.Equal(Rotation.Rotate0, vm.Rotation);
        Assert.Equal("160", vm.OffsetXText);
        Assert.Equal(160, vm.ToParameters().Processing.OffsetX);

        dialogs.ConfirmAnswer = true;
        vm.SizeMode = TargetSizeMode.Manual;
        Load(vm, 40, 20, "small.png");
        vm.OffsetXText = "100";
        vm.ApplyStroke(new[] { (2, 2) }, StrokePaint.Dot);
        dialogs.ConfirmAnswer = false;
        int confirms = dialogs.ConfirmCount;

        vm.SizeMode = TargetSizeMode.MatchSource;

        Assert.Equal(confirms + 1, dialogs.ConfirmCount);
        Assert.Equal(TargetSizeMode.Manual, vm.SizeMode);
        Assert.Equal("100", vm.OffsetXText);
        Assert.Equal(100, vm.ToParameters().Processing.OffsetX);
    }

    [Fact]
    public void A_smaller_preset_clamps_offsets()
    {
        ImageConverterViewModel vm = CreateImage();
        LoadWhite(vm);
        vm.WidthText = "240";
        vm.HeightText = "128";
        vm.OffsetXText = "180";
        vm.OffsetYText = "100";

        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "OLED128X64-0.96");

        Assert.Equal(128, vm.ToParameters().Processing.TargetWidth);
        Assert.Equal(64, vm.ToParameters().Processing.TargetHeight);
        Assert.Equal("102", vm.OffsetXText);
        Assert.Equal("51", vm.OffsetYText);
        Assert.Equal(102, vm.ToParameters().Processing.OffsetX);
        Assert.Equal(51, vm.ToParameters().Processing.OffsetY);
        Assert.Empty(Errors(vm, nameof(vm.OffsetXText)));
        Assert.Empty(Errors(vm, nameof(vm.OffsetYText)));
    }

    [Fact]
    public void Opening_a_match_source_project_clamps_after_the_picture_is_known()
    {
        var decoder = new MapDecoder();
        string stale = Path.Combine(_directory, "stale.png");
        File.WriteAllBytes(stale, new byte[] { 1 });
        decoder.Add(stale, Picture(8, 8));
        string wide = Path.Combine(_directory, "wide.png");
        File.WriteAllBytes(wide, new byte[] { 2 });
        decoder.Add(wide, Picture(400, 300));
        string project = Path.Combine(_directory, "wide.iiu");
        SaveProject(project, MatchSource(300), wide);
        var dialogs = new FakeDialogs();
        MainViewModel main = CreateMain(dialogs, new FakeFiles { NextOpenProject = project }, decoder);
        main.Image.LoadDecoded(decoder.Decode(stale), stale, resetFrame: true, userAction: false);

        main.OpenProjectCommand.Execute(null);

        Assert.Empty(dialogs.Alerts);
        Assert.Equal("300", main.Image.OffsetXText);
        Assert.Equal(300, main.Image.ToParameters().Processing.OffsetX);
        main.Image.OffsetXText = "320";
        Assert.Empty(Errors(main.Image, nameof(ImageConverterViewModel.OffsetXText)));
        main.Image.OffsetXText = "321";
        Assert.Equal(Format("Error.OffsetX", 320, 400), Assert.Single(Errors(main.Image, nameof(ImageConverterViewModel.OffsetXText))));
    }

    [Fact]
    public void Opening_a_match_source_project_without_the_picture_clamps_to_the_fields()
    {
        string project = Path.Combine(_directory, "missing.iiu");
        SaveProject(project, MatchSource(300), Path.Combine(_directory, "absent.png"));
        var decoder = new MapDecoder();
        string stale = Path.Combine(_directory, "stale.png");
        File.WriteAllBytes(stale, new byte[] { 1 });
        decoder.Add(stale, Picture(8, 8));
        var dialogs = new FakeDialogs();
        MainViewModel main = CreateMain(dialogs, new FakeFiles { NextOpenProject = project }, decoder);
        main.Image.LoadDecoded(decoder.Decode(stale), stale, resetFrame: true, userAction: false);

        main.OpenProjectCommand.Execute(null);

        Assert.Empty(dialogs.Alerts);
        Assert.Equal("102", main.Image.OffsetXText);
        Assert.Equal(102, main.Image.ToParameters().Processing.OffsetX);
    }

    [Fact]
    public void Opening_a_project_with_offset_500_on_width_128_clamps_without_throwing()
    {
        string project = Path.Combine(_directory, "wide-offset.iiu");
        ImageTabParameters parameters = ImageTabParameters.CreateDefault() with
        {
            Processing = ProcessingOptions.Default with { OffsetX = 500, TargetWidth = 128, TargetHeight = 64 },
        };
        SaveProject(project, parameters, sourcePath: null);
        var dialogs = new FakeDialogs();
        MainViewModel main = CreateMain(dialogs, new FakeFiles { NextOpenProject = project }, new MapDecoder());

        main.OpenProjectCommand.Execute(null);

        Assert.Empty(dialogs.Alerts);
        Assert.Equal("102", main.Image.OffsetXText);
        Assert.Equal(102, main.Image.ToParameters().Processing.OffsetX);
        Assert.Equal(128, main.Image.ToParameters().Processing.TargetWidth);
    }

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

    private static void LoadWhite(ImageConverterViewModel vm) => Load(vm, 8, 8, "white.png");

    private static void Load(ImageConverterViewModel vm, int width, int height, string name)
    {
        vm.LoadDecoded(
            Picture(width, height),
            name,
            resetFrame: true,
            userAction: false);
        vm.ArrayName = "logo";
    }

    private static DecodedImage Picture(int width, int height) =>
        new(ImageFileFormat.Png, new[] { new RgbaImage(width, height, 255, 255, 255, 255) });

    private static ImageTabParameters MatchSource(int offsetX) => ImageTabParameters.CreateDefault() with
    {
        Processing = ProcessingOptions.Default with
        {
            SizeMode = TargetSizeMode.MatchSource,
            TargetWidth = 128,
            TargetHeight = 64,
            OffsetX = offsetX,
        },
    };

    private static void SaveProject(string path, ImageTabParameters image, string? sourcePath) =>
        ProjectSerializer.Save(
            new ProjectDocument(new ImageProjectTab(image, sourcePath), FontProjectTab.CreateDefault()),
            path);

    private ImageConverterViewModel CreateImage(FakeDialogs? dialogs = null)
    {
        var settings = new SettingsService(_directory, _directory).Load().Settings;
        return new ImageConverterViewModel(
            new MapText(Text),
            dialogs ?? new FakeDialogs(),
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            new UnusedDecoder(),
            PresetCatalog.Shared,
            settings.UserPresets,
            ImageTabParameters.CreateDefault());
    }

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
