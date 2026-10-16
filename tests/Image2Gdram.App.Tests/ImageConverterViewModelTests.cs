using System.ComponentModel;
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
        string c = Path.Combine(output, "white_128x64.c");
        string h = Path.Combine(output, "white_128x64.h");
        harness.Files.NextSaveOutput = c;
        string arrayName = vm.ArrayName;
        string code = vm.CodeText;
        byte[] cBefore = vm.Document!.Files[0].Content.ToArray();
        byte[] hBefore = vm.Document.Files[1].Content.ToArray();

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal("Сохранить Код", harness.Files.LastTitle);
        Assert.Equal("white_128x64", harness.Files.LastFileName);
        Assert.Equal("Файлы кода (*.c, *.h)|*.c", harness.Files.LastFilter);
        Assert.Equal(".c", harness.Files.LastDefaultExt);
        Assert.Equal(arrayName, vm.ArrayName);
        Assert.Equal(code, vm.CodeText);
        Assert.Equal(cBefore, vm.Document.Files[0].Content);
        Assert.Equal(hBefore, vm.Document.Files[1].Content);
        Assert.Equal(vm.Document.Files[0].Content, File.ReadAllBytes(c));
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

        vm.Format = OutputFormat.Bin;
        string binCode = vm.CodeText;
        byte[] bin = vm.Document.Files[0].Content.ToArray();
        string binFile = vm.Document.Files[0].FileName;
        harness.Files.NextSaveOutput = Path.Combine(output, binFile);

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(arrayName, harness.Files.LastFileName);
        Assert.Equal("Файлы кода (*.bin)|*.bin", harness.Files.LastFilter);
        Assert.Equal(".bin", harness.Files.LastDefaultExt);
        Assert.Equal(arrayName, vm.ArrayName);
        Assert.Equal(binCode, vm.CodeText);
        Assert.Equal(bin, vm.Document.Files[0].Content);
        Assert.Equal(bin, File.ReadAllBytes(Path.Combine(output, binFile)));
    }

    [Fact]
    public void Save_dialog_receives_array_name_title_filter_and_initial_folder()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        string sourceDir = Path.Combine(harness.Directory, "src_img");
        string outputDir = Path.Combine(harness.Directory, "out_img");
        System.IO.Directory.CreateDirectory(sourceDir);
        System.IO.Directory.CreateDirectory(outputDir);
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), Path.Combine(sourceDir, "logo.png"), resetFrame: true, userAction: false);

        harness.Files.NextSaveOutput = null;
        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(1, harness.Files.SaveOutputCalls);
        Assert.Equal(sourceDir, harness.Files.LastFolder);
        Assert.Equal("logo_128x64", harness.Files.LastFileName);
        Assert.Equal("Сохранить Код", harness.Files.LastTitle);
        Assert.Equal("Файлы кода (*.c, *.h)|*.c", harness.Files.LastFilter);
        Assert.Equal(".c", harness.Files.LastDefaultExt);

        vm.SetFolders(sourceDir, outputDir);
        vm.Format = OutputFormat.CStm32;
        vm.SaveOutputCommand.Execute(null);
        Assert.Equal(outputDir, harness.Files.LastFolder);
        Assert.Equal("Файлы кода (*.c, *.h)|*.c", harness.Files.LastFilter);
        Assert.Equal(".c", harness.Files.LastDefaultExt);

        vm.Format = OutputFormat.A51Module;
        vm.AsmExtension = AsmFileExtension.A51;
        vm.SaveOutputCommand.Execute(null);
        Assert.Equal("Файлы кода (*.a51)|*.a51", harness.Files.LastFilter);
        Assert.Equal(".a51", harness.Files.LastDefaultExt);

        vm.Format = OutputFormat.A51Include;
        vm.AsmExtension = AsmFileExtension.Asm;
        vm.SaveOutputCommand.Execute(null);
        Assert.Equal("Файлы кода (*.asm)|*.asm", harness.Files.LastFilter);
        Assert.Equal(".asm", harness.Files.LastDefaultExt);

        vm.Format = OutputFormat.Bin;
        vm.SaveOutputCommand.Execute(null);
        Assert.Equal("Файлы кода (*.bin)|*.bin", harness.Files.LastFilter);
        Assert.Equal(".bin", harness.Files.LastDefaultExt);
    }

    public static TheoryData<string> NewCFileNames => new()
    {
        "logo_new",
        "logo_new.c",
        "logo_new.h",
    };

    [Theory]
    [MemberData(nameof(NewCFileNames))]
    public void Save_with_a_new_name_updates_array_name_regenerates_files_and_marks_name_customized(string entered)
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.IncludeDate = false;
        string output = Path.Combine(harness.Directory, "out");
        System.IO.Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = Path.Combine(output, entered);

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal("logo_new", vm.ArrayName);
        string cPath = Path.Combine(output, "logo_new.c");
        string hPath = Path.Combine(output, "logo_new.h");
        Assert.True(File.Exists(cPath));
        Assert.True(File.Exists(hPath));
        Assert.False(File.Exists(Path.Combine(output, "white_128x64.c")));
        Assert.False(File.Exists(Path.Combine(output, "white_128x64.h")));
        string cText = File.ReadAllText(cPath);
        string hText = File.ReadAllText(hPath);
        Assert.Contains("#include \"logo_new.h\"", cText, StringComparison.Ordinal);
        Assert.Contains("logo_new[LOGO_NEW_SIZE]", cText, StringComparison.Ordinal);
        Assert.Contains("#define LOGO_NEW_WIDTH", hText, StringComparison.Ordinal);
        Assert.Contains("#define LOGO_NEW_HEIGHT", hText, StringComparison.Ordinal);
        Assert.Contains("#define LOGO_NEW_SIZE", hText, StringComparison.Ordinal);
        Assert.Equal(new[] { "logo_new.c", "logo_new.h" }, vm.CodeFiles.Select(f => f.Caption));
        Assert.Equal(vm.Document!.Files[0].Content, File.ReadAllBytes(cPath));
        Assert.Equal(vm.Document.Files[1].Content, File.ReadAllBytes(hPath));

        vm.WidthText = "64";
        Assert.Equal("logo_new", vm.ArrayName);
    }

    [Theory]
    [InlineData("logo_new.BIN", "logo_new")]
    [InlineData("LOGO_NEW.BIN", "LOGO_NEW")]
    public void Save_bin_strips_uppercase_extension_and_updates_array_name(string entered, string expectedName)
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.Format = OutputFormat.Bin;
        string output = Path.Combine(harness.Directory, "out_bin");
        System.IO.Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = Path.Combine(output, entered);

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(expectedName, vm.ArrayName);
        string binPath = Path.Combine(output, expectedName + ".bin");
        Assert.True(File.Exists(binPath));
        Assert.Equal(new[] { expectedName + ".bin" }, vm.CodeFiles.Select(f => f.Caption));
        Assert.Equal(vm.Document!.Files[0].Content, File.ReadAllBytes(binPath));

        vm.WidthText = "64";
        Assert.Equal(expectedName, vm.ArrayName);
    }

    [Fact]
    public void Save_with_a_deferred_scheduler_still_writes_files_with_the_new_name()
    {
        var scheduler = new DeferredRecalcScheduler();
        using var harness = new Harness(scheduler);
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.IncludeDate = false;
        string output = Path.Combine(harness.Directory, "out_deferred");
        System.IO.Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = Path.Combine(output, "logo_deferred.c");
        scheduler.Hold = true;

        vm.SaveOutputCommand.Execute(null);

        Assert.True(scheduler.PendingCount > 0);
        Assert.Equal("logo_deferred", vm.ArrayName);
        string cPath = Path.Combine(output, "logo_deferred.c");
        string hPath = Path.Combine(output, "logo_deferred.h");
        Assert.True(File.Exists(cPath));
        Assert.True(File.Exists(hPath));
        Assert.Contains("#include \"logo_deferred.h\"", File.ReadAllText(cPath), StringComparison.Ordinal);
        Assert.Contains("#define LOGO_DEFERRED_SIZE", File.ReadAllText(hPath), StringComparison.Ordinal);
        Assert.Equal(new[] { "logo_deferred.c", "logo_deferred.h" }, vm.CodeFiles.Select(f => f.Caption));

        scheduler.RunPending();
        Assert.Equal("logo_deferred", vm.ArrayName);
        Assert.Equal(new[] { "logo_deferred.c", "logo_deferred.h" }, vm.CodeFiles.Select(f => f.Caption));
    }

    public static TheoryData<OutputFormat, string> InvalidNames => new()
    {
        { OutputFormat.CKeilC51, "1abc" },
        { OutputFormat.CKeilC51, "лого" },
        { OutputFormat.CKeilC51, "code" },
        { OutputFormat.CKeilC51, "my-name" },
        { OutputFormat.CKeilC51, "logo.v2" },
        { OutputFormat.A51Module, new string('a', 28) },
    };

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void Save_with_an_invalid_name_alerts_and_reopens_the_dialog_with_the_entered_text(OutputFormat format, string badName)
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.Format = format;
        string originalName = vm.ArrayName;
        string output = Path.Combine(harness.Directory, "out_bad");
        System.IO.Directory.CreateDirectory(output);
        harness.Files.NextSaveOutputs.Enqueue(Path.Combine(output, badName));
        harness.Files.NextSaveOutputs.Enqueue(null);

        vm.SaveOutputCommand.Execute(null);

        var text = new MapText(TestFiles.LoadUiStrings());
        string expectedAlert = UserText.Name(text, NameValidator.Validate(badName, format));
        Assert.Equal(new[] { expectedAlert }, harness.Dialogs.Alerts);
        Assert.Equal(2, harness.Files.SaveOutputCalls);
        Assert.Equal(output, harness.Files.LastFolder);
        Assert.Equal(badName, harness.Files.LastFileName);
        Assert.Equal(originalName, vm.ArrayName);
        Assert.Empty(System.IO.Directory.GetFiles(output));
    }

    [Fact]
    public void Save_after_correcting_an_invalid_name_in_the_reopened_dialog_writes_the_valid_files()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        string output = Path.Combine(harness.Directory, "out_retry");
        System.IO.Directory.CreateDirectory(output);
        harness.Files.NextSaveOutputs.Enqueue(Path.Combine(output, "1abc.c"));
        harness.Files.NextSaveOutputs.Enqueue(Path.Combine(output, "fixed_logo.c"));

        vm.SaveOutputCommand.Execute(null);

        Assert.Single(harness.Dialogs.Alerts);
        Assert.Equal(2, harness.Files.SaveOutputCalls);
        Assert.Equal("fixed_logo", vm.ArrayName);
        Assert.True(File.Exists(Path.Combine(output, "fixed_logo.c")));
        Assert.True(File.Exists(Path.Combine(output, "fixed_logo.h")));
    }

    [Fact]
    public void Cancelling_the_first_save_dialog_writes_nothing_and_keeps_the_array_name()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        string originalName = vm.ArrayName;
        string output = Path.Combine(harness.Directory, "out_cancel");
        System.IO.Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = null;

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(1, harness.Files.SaveOutputCalls);
        Assert.Equal(originalName, vm.ArrayName);
        Assert.Empty(harness.Dialogs.Alerts);
        Assert.Empty(System.IO.Directory.GetFiles(output));
    }

    [Fact]
    public void Typing_array_name_on_the_panel_and_focusing_save_button_flushes_the_name_before_the_dialog()
    {
        using var ui = new StaDispatcher();
        var scheduler = new DeferredRecalcScheduler();
        using var harness = new Harness(scheduler);
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        string output = Path.Combine(harness.Directory, "out_flush");
        System.IO.Directory.CreateDirectory(output);
        scheduler.Hold = true;
        System.Windows.Window? window = null;
        try
        {
            ui.Dispatcher.Invoke(() =>
            {
                var box = new System.Windows.Controls.TextBox { Width = 120 };
                var button = new System.Windows.Controls.Button
                {
                    Content = "Сохранить Код",
                    Focusable = true,
                    Command = vm.SaveOutputCommand,
                };
                var panel = new System.Windows.Controls.StackPanel();
                panel.Children.Add(box);
                panel.Children.Add(button);
                window = new System.Windows.Window
                {
                    Width = 220,
                    Height = 120,
                    Left = -20000,
                    Top = -20000,
                    ShowInTaskbar = false,
                    ShowActivated = true,
                    WindowStyle = System.Windows.WindowStyle.None,
                    Content = panel,
                };
                window.Show();
                System.Windows.Data.BindingOperations.SetBinding(
                    box,
                    System.Windows.Controls.TextBox.TextProperty,
                    new System.Windows.Data.Binding(nameof(ImageConverterViewModel.ArrayName))
                    {
                        Source = vm,
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged,
                        Delay = 400,
                    });
                image2gdram_converter.Controls.TextBoxCommit.SetFlush(box, true);
                box.Focus();
                box.Text = "panel_logo";
                Assert.Equal("white_128x64", vm.ArrayName);

                harness.Files.NextSaveOutput = Path.Combine(output, "panel_logo.c");
                button.Focus();
                Assert.Equal("panel_logo", vm.ArrayName);

                button.Command.Execute(null);
                Assert.Equal("panel_logo", harness.Files.LastFileName);
                Assert.True(File.Exists(Path.Combine(output, "panel_logo.c")));
                Assert.Contains("#include \"panel_logo.h\"", File.ReadAllText(Path.Combine(output, "panel_logo.c")), StringComparison.Ordinal);
            });
        }
        finally
        {
            ui.Dispatcher.Invoke(() => window?.Close());
        }
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

    [Theory]
    [InlineData(OutputFormat.A51Module, AsmFileExtension.A51, ".a51")]
    [InlineData(OutputFormat.A51Include, AsmFileExtension.Asm, ".asm")]
    [InlineData(OutputFormat.Bin, AsmFileExtension.A51, ".bin")]
    public void Single_output_file_keeps_its_name_above_the_code(OutputFormat format, AsmFileExtension extension, string suffix)
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        LoadWhite(vm);
        vm.Format = format;
        vm.AsmExtension = extension;

        CodeFileItem file = Assert.Single(vm.CodeFiles);
        Assert.Equal(vm.ArrayName + suffix, file.Caption);
        Assert.True(vm.ShowCodeFiles);
        Assert.Equal(0, vm.CodeIndex);
        Assert.Equal(file.Text, vm.CodeText);

        vm.SetHover(0, 0);

        Assert.True(vm.HighlightLength > 0);
        Assert.Equal(0, vm.CodeIndex);
    }

    [Fact]
    public void Automatic_name_from_a_long_file_fits_an_a51_module_when_the_picture_opens_after_load_state()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        string path = Path.Combine(harness.Directory, "very_long_source_picture_name.png");
        string full = DefaultNameBuilder.FromFile(path, 128, 64, ArrayNameKind.Image, NameValidator.MaxLength);
        Assert.Equal(NameValidator.MaxLength, full.Length);
        ImageTabParameters parameters = ImageTabParameters.CreateDefault() with
        {
            Output = OutputOptions.Default with { ArrayName = full },
        };

        vm.LoadState(parameters, schedule: false);
        vm.LoadDecoded(
            new DecodedImage(ImageFileFormat.Png, new[] { new RgbaImage(8, 8, 255, 255, 255, 255) }),
            path,
            resetFrame: false,
            userAction: false);
        vm.Format = OutputFormat.A51Module;

        Assert.Empty(NameErrors(vm));
        Assert.InRange(vm.ArrayName.Length, 1, NameValidator.MaxLengthA51Module);
        Assert.True(vm.CanCopy);

        vm.Format = OutputFormat.A51Include;

        Assert.Empty(NameErrors(vm));
        Assert.Equal(full, vm.ArrayName);
        Assert.True(vm.CanCopy);

        vm.Format = OutputFormat.CKeilC51;

        Assert.Equal(full, vm.ArrayName);
        Assert.Empty(NameErrors(vm));
    }

    [Fact]
    public void Manual_name_typed_after_load_state_is_not_replaced_when_the_picture_opens()
    {
        using var harness = new Harness();
        ImageConverterViewModel vm = harness.Image;
        string path = Path.Combine(harness.Directory, "very_long_source_picture_name.png");
        string full = DefaultNameBuilder.FromFile(path, 128, 64, ArrayNameKind.Image, NameValidator.MaxLength);
        ImageTabParameters parameters = ImageTabParameters.CreateDefault() with
        {
            Output = OutputOptions.Default with { ArrayName = full },
        };

        vm.LoadState(parameters, schedule: false);
        vm.ArrayName = "screen";
        vm.LoadDecoded(
            new DecodedImage(ImageFileFormat.Png, new[] { new RgbaImage(8, 8, 255, 255, 255, 255) }),
            path,
            resetFrame: false,
            userAction: false);
        vm.Format = OutputFormat.A51Module;

        Assert.Equal("screen", vm.ArrayName);
        Assert.Empty(NameErrors(vm));
        Assert.True(vm.CanCopy);
    }

    private static string[] NameErrors(ImageConverterViewModel vm) =>
        ((INotifyDataErrorInfo)vm).GetErrors(nameof(ImageConverterViewModel.ArrayName)).Cast<object>()
            .Select(error => error.ToString()!)
            .ToArray();

    private static void LoadWhite(ImageConverterViewModel vm)
    {
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        vm.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "white.png", resetFrame: true, userAction: false);
    }

    private sealed class Harness : IDisposable
    {
        public Harness(IRecalcScheduler? scheduler = null)
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
                scheduler ?? new ImmediateRecalcScheduler(),
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
