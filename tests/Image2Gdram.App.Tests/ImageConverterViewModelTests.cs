using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
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

    [Fact]
    public void Choosing_a_preset_with_another_size_renames_the_automatic_array()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        Assert.Equal("white_128x64", vm.ArrayName);

        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "WG240128A");

        Assert.Equal("white_240x128", vm.ArrayName);
        Assert.Equal("white_240x128.c", vm.Document!.Files[0].FileName);

        vm.ArrayName = "screen";
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "OLED128X64-0.96");

        Assert.Equal("screen", vm.ArrayName);
    }

    [Fact]
    public void Animated_gif_starts_at_frame_1_keeps_edits_per_frame_and_exports_all_frames()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        vm.WidthText = "8";
        vm.HeightText = "8";
        vm.IncludeDate = false;
        vm.Format = OutputFormat.Bin;
        var frames = new RgbaImage[3];
        for (int i = 0; i < frames.Length; i++)
        {
            frames[i] = new RgbaImage(8, 8, 255, 255, 255, 255);
            frames[i].SetPixel(i, 0, 0, 0, 0, 255);
        }

        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Gif, frames), "anim.gif", resetFrame: true, userAction: true);

        Assert.True(vm.IsAnimated);
        Assert.Equal(new[] { 1, 2, 3 }, vm.Frames);
        Assert.Equal(1, vm.SelectedFrame);
        Assert.Equal(new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0 }, vm.Document!.Files[0].Content);

        vm.SelectedFrame = 2;
        Assert.False(vm.Preview![0, 0]);
        Assert.True(vm.Preview[1, 0]);
        Assert.Equal(new byte[] { 0, 0x01, 0, 0, 0, 0, 0, 0 }, vm.Document!.Files[0].Content);

        vm.ApplyStroke(new[] { (7, 7) }, StrokePaint.Dot);
        vm.SelectedFrame = 1;
        Assert.True(vm.HasEdits);
        Assert.False(vm.Preview![7, 7]);
        vm.SelectedFrame = 2;
        Assert.True(vm.Preview![7, 7]);

        vm.ExportAllFrames = true;
        Assert.Equal(
            new byte[] { 0x01, 0, 0, 0, 0, 0, 0, 0, 0, 0x01, 0, 0, 0, 0, 0, 0x80, 0, 0, 0x01, 0, 0, 0, 0, 0 },
            vm.Document!.Files[0].Content);

        vm.Format = OutputFormat.CKeilC51;
        string header = vm.Document!.Files[1].Text;
        Assert.Matches(@"#define ANIM_8X8_FRAMES\s+3\r\n", header);
        Assert.Matches(@"#define ANIM_8X8_FRAME_SIZE\s+8\r\n", header);
        Assert.Matches(@"#define ANIM_8X8_SIZE\s+24\r\n", header);
        Assert.Contains("anim_8x8[ANIM_8X8_FRAMES][ANIM_8X8_FRAME_SIZE];", header);
    }

    [Fact]
    public void Copy_puts_the_text_of_the_shown_file_on_the_clipboard()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.IncludeDate = false;

        vm.CopyCommand.Execute(null);

        Assert.Equal(vm.Document!.Files[0].Text, harness.Clipboard.Text);
        Assert.Contains("\r\n", harness.Clipboard.Text);
        Assert.DoesNotContain("\n", harness.Clipboard.Text!.Replace("\r\n", string.Empty));

        vm.CodeIndex = 1;
        vm.CopyCommand.Execute(null);

        Assert.Equal(vm.Document.Files[1].Text, harness.Clipboard.Text);
    }

    [Fact]
    public void Save_writes_the_c_and_h_pair_and_asks_before_overwriting()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.IncludeDate = false;
        string output = Path.Combine(harness.Directory, "out");
        System.IO.Directory.CreateDirectory(output);
        harness.Files.NextFolder = output;

        vm.SaveOutputCommand.Execute(null);

        string c = Path.Combine(output, "white_128x64.c");
        string h = Path.Combine(output, "white_128x64.h");
        Assert.Equal(vm.Document!.Files[0].Content, File.ReadAllBytes(c));
        Assert.Equal(vm.Document.Files[1].Content, File.ReadAllBytes(h));
        Assert.Equal(0, harness.Dialogs.ConfirmCount);

        File.WriteAllText(c, "old");
        harness.Dialogs.ConfirmAnswer = false;
        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(1, harness.Dialogs.ConfirmCount);
        Assert.Equal("old", File.ReadAllText(c));

        harness.Dialogs.ConfirmAnswer = true;
        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(2, harness.Dialogs.ConfirmCount);
        Assert.Equal(vm.Document.Files[0].Content, File.ReadAllBytes(c));
        Assert.Empty(harness.Dialogs.Alerts);
    }

    [Fact]
    public void User_preset_is_saved_renamed_and_deleted_and_built_in_ones_stay()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        vm.WidthText = "100";
        vm.Invert = true;
        harness.Dialogs.TextAnswer = "Мой дисплей";

        vm.SavePresetCommand.Execute(null);

        Preset saved = Assert.Single(harness.Users.Presets);
        Assert.Equal("Мой дисплей", saved.Name);
        Assert.Equal(100, saved.Width);
        Assert.True(saved.Packing.Invert);
        Assert.Equal("Мой дисплей", vm.PresetCaption);
        Assert.True(vm.RenamePresetCommand.CanExecute(null));

        harness.Dialogs.TextAnswer = "Другой";
        vm.RenamePresetCommand.Execute(null);

        Assert.Equal("Другой", Assert.Single(harness.Users.Presets).Name);
        Assert.Equal("Другой", vm.PresetCaption);

        harness.Dialogs.ConfirmAnswer = false;
        vm.DeletePresetCommand.Execute(null);
        Assert.Single(harness.Users.Presets);

        harness.Dialogs.ConfirmAnswer = true;
        vm.DeletePresetCommand.Execute(null);
        Assert.Empty(harness.Users.Presets);

        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "WG240128A");
        Assert.False(vm.RenamePresetCommand.CanExecute(null));
        Assert.False(vm.DeletePresetCommand.CanExecute(null));
        Assert.Equal(6, PresetCatalog.Shared.BuiltIn.Count);
    }

    [Fact]
    public void Array_above_64_kilobytes_shows_the_c51_warning_on_the_tab()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        vm.WidthText = "1024";
        vm.HeightText = "1024";
        LoadWhite(vm);

        Assert.Contains("131072", vm.WarningText, StringComparison.Ordinal);
        Assert.True(vm.CanCopy);

        vm.Format = OutputFormat.CStm32;

        Assert.Equal(string.Empty, vm.WarningText);
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
            Files = new FakeFiles();
            Clipboard = new FakeClipboard();
            Users = settings.UserPresets;
            Image = new ImageConverterViewModel(
                new MapText(TestFiles.LoadUiStrings()),
                Dialogs,
                Files,
                Clipboard,
                new ImmediateRecalcScheduler(),
                new UnusedDecoder(),
                PresetCatalog.Shared,
                settings.UserPresets,
                ImageTabParameters.CreateDefault());
        }

        public string Directory { get; }

        public FakeDialogs Dialogs { get; }

        public FakeFiles Files { get; }

        public FakeClipboard Clipboard { get; }

        public UserPresetStore Users { get; }

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
