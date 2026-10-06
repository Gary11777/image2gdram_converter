using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

public class ImageConverterViewModelTests
{
    [Fact]
    public void Editing_a_preset_parameter_shows_custom_based_on_that_preset()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        Preset preset = PresetCatalog.Shared.BuiltIn.First(item => item.Name == "OLED128X64-0.96");
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == preset.Name);

        vm.WidthText = "100";

        Assert.Equal("Пользовательский (на основе OLED128X64-0.96)", vm.PresetCaption);
        Assert.Equal(100, vm.ToParameters().Processing.TargetWidth);
        Assert.Equal(preset.Name, vm.ToParameters().Preset.BasedOn);
    }

    [Fact]
    public void Refusing_to_reset_edits_restores_the_parameter()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        Assert.True(vm.HasEdits);
        Assert.True(vm.Preview![1, 1]);
        harness.Dialogs.ConfirmAnswer = false;

        vm.Threshold = 200;

        Assert.Equal(1, harness.Dialogs.ConfirmCount);
        Assert.Equal(128, vm.Threshold);
        Assert.True(vm.HasEdits);
        Assert.True(vm.Preview[1, 1]);
    }

    [Fact]
    public void Accepting_the_reset_clears_edits_and_history()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        harness.Dialogs.ConfirmAnswer = true;

        vm.Threshold = 200;

        Assert.Equal(200, vm.Threshold);
        Assert.False(vm.HasEdits);
        Assert.False(vm.CanUndo);
        Assert.False(vm.Preview![1, 1]);
    }

    [Fact]
    public void Choosing_another_preset_is_rolled_back_when_edits_stay()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "WG240128A");
        LoadWhite(vm);
        vm.ApplyStroke(new[] { (2, 2) }, StrokePaint.Dot);
        harness.Dialogs.ConfirmAnswer = false;

        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "OLED128X64-0.96");

        Assert.Equal(240, vm.ToParameters().Processing.TargetWidth);
        Assert.True(vm.HasEdits);
        Assert.Equal("WG240128A", vm.PresetCaption);
    }

    [Fact]
    public void A_stroke_is_one_undo_step()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.ApplyStroke(new[] { (0, 0), (1, 0) }, StrokePaint.Dot);
        Assert.True(vm.Preview![0, 0]);
        Assert.True(vm.Preview[1, 0]);

        vm.UndoCommand.Execute(null);

        Assert.False(vm.Preview[0, 0]);
        Assert.False(vm.Preview[1, 0]);
        Assert.False(vm.CanUndo);
        Assert.True(vm.CanRedo);

        vm.RedoCommand.Execute(null);

        Assert.True(vm.Preview[0, 0]);
        Assert.True(vm.Preview[1, 0]);
    }

    [Fact]
    public void Invalid_width_blocks_generation_and_keeps_edits()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        int confirms = harness.Dialogs.ConfirmCount;

        vm.WidthText = "0";

        Assert.Equal(confirms, harness.Dialogs.ConfirmCount);
        Assert.True(vm.HasEdits);
        Assert.False(vm.CanCopy);
        Assert.Contains("1", vm.CodeText);
        Assert.Contains("1024", vm.CodeText);
    }

    [Fact]
    public void Hover_reports_the_packed_byte_of_the_origin()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        vm.WidthText = "4";
        vm.HeightText = "4";
        var pixels = new RgbaImage(4, 4, 255, 255, 255, 255);
        pixels.SetPixel(0, 0, 0, 0, 0, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { pixels }), "dot.png", resetFrame: true, userAction: false);
        vm.IncludeDate = false;

        vm.SetHover(0, 0);

        Assert.Contains("X: 0", vm.HoverText);
        Assert.Contains("Y: 0", vm.HoverText);
        Assert.Contains("0x01", vm.HoverText);
        Assert.Contains("00000001", vm.HoverText);
        Assert.True(vm.HighlightLength > 0);
    }

    [Fact]
    public void Horizontal_mode_hides_page_traversal_and_a_short_height_does_too()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        Assert.True(vm.TraversalEnabled);
        Assert.False(vm.BitsEnabled);

        vm.Direction = Image2Gdram.Core.Packing.PackDirection.Horizontal;

        Assert.False(vm.TraversalEnabled);
        Assert.True(vm.BitsEnabled);

        vm.Direction = Image2Gdram.Core.Packing.PackDirection.Vertical;
        vm.HeightText = "8";

        Assert.False(vm.TraversalEnabled);
    }

    private static void LoadWhite(ImageConverterViewModel vm)
    {
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "white.png", resetFrame: true, userAction: false);
    }

    private sealed class Harness : IDisposable
    {
        public Harness()
        {
            Directory = Path.Combine(Path.GetTempPath(), "i2g-app-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(Directory);
            var settings = new SettingsService(Directory, Directory).Load().Settings;
            Dialogs = new FakeDialogs();
            Image = new ImageConverterViewModel(
                new MapText(TestFiles.LoadUiStrings()),
                Dialogs,
                new FakeFiles(),
                new FakeClipboard(),
                new ImmediateRecalcScheduler(),
                new UnusedDecoder(),
                PresetCatalog.Shared,
                settings.UserPresets,
                ImageTabParameters.CreateDefault());
        }

        public string Directory { get; }

        public FakeDialogs Dialogs { get; }

        public ImageConverterViewModel Image { get; }

        public void Dispose()
        {
            try
            {
                System.IO.Directory.Delete(Directory, recursive: true);
            }
            catch (IOException)
            {
            }
        }
    }
}
