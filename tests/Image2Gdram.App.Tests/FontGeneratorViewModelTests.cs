using System.ComponentModel;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

public class FontGeneratorViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-font-" + Guid.NewGuid().ToString("N"));

    public FontGeneratorViewModelTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Preview_text_starts_as_the_agreed_sample()
    {
        using var harness = new Harness(_directory);
        Assert.Equal(FontTabParameters.DefaultPreviewText, harness.Font.PreviewText);
        Assert.Equal(2, harness.Font.PreviewScale);
    }

    [Fact]
    public void Preset_sets_packing_and_scheme_but_not_the_cell()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.Presets.Selected = vm.Presets.Items.First(item => item.Preset?.Name == "WG240128A");

        Assert.Equal(PackDirection.Horizontal, vm.Direction);
        Assert.Equal(6, vm.Cell.Width);
        Assert.Equal(8, vm.Cell.Height);
        Assert.Equal("WG240128A", vm.PresetCaption);

        vm.Direction = PackDirection.Vertical;

        Assert.Equal("Пользовательский (на основе WG240128A)", vm.PresetCaption);
        Assert.Equal(6, vm.ToParameters().CellWidth);
    }

    [Fact]
    public void A_stroke_is_one_undo_step_and_the_origin_pixel_is_packed()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.IncludeDate = false;
        vm.Editor.ApplyStroke(new[] { (0, 0) }, StrokePaint.Toggle);

        Assert.True(vm.Table.GetGlyph(0)[0, 0]);
        Assert.True(vm.Table.IsManual(0));
        Assert.Contains("0x01", vm.CodeText, StringComparison.Ordinal);

        vm.UndoCommand.Execute(null);
        Assert.False(vm.Table.GetGlyph(0)[0, 0]);
        Assert.False(vm.CanUndo);

        vm.RedoCommand.Execute(null);
        Assert.True(vm.Table.GetGlyph(0)[0, 0]);
        vm.SetHover(0, 0);
        Assert.True(vm.HighlightLength > 0);
    }

    [Fact]
    public void Refusing_a_new_cell_keeps_the_manual_glyph()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.Editor.ApplyStroke(new[] { (1, 1) }, StrokePaint.Toggle);
        harness.Dialogs.ConfirmAnswer = false;

        vm.Cell = FontCellSize.Cell8x8;

        Assert.Equal(1, harness.Dialogs.ConfirmCount);
        Assert.Equal(6, vm.Cell.Width);
        Assert.True(vm.Table.IsManual(0));
    }

    [Fact]
    public void Accepting_a_new_cell_replaces_the_table()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.Editor.ApplyStroke(new[] { (1, 1) }, StrokePaint.Toggle);
        harness.Dialogs.ConfirmAnswer = true;

        vm.Cell = FontCellSize.Cell12x16;

        Assert.Equal(12, vm.Cell.Width);
        Assert.False(vm.Table.HasManualEdits);
        Assert.False(vm.CanUndo);
        Assert.True(vm.TraversalEnabled);
        Assert.False(vm.BitsEnabled);

        vm.Direction = PackDirection.Horizontal;
        Assert.True(vm.BitsEnabled);
        Assert.False(vm.TraversalEnabled);
    }

    [Fact]
    public void Revert_and_reset_restore_the_source_and_can_be_undone_only_for_one_glyph()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        vm.Editor.SelectedCode = 0x41;
        vm.Editor.ApplyStroke(new[] { (0, 0) }, StrokePaint.Toggle);
        Assert.True(vm.Table.IsManual(0x41));

        vm.Editor.RevertGlyph();
        Assert.False(vm.Table.IsManual(0x41));
        vm.UndoCommand.Execute(null);
        Assert.True(vm.Table.IsManual(0x41));

        harness.Dialogs.ConfirmAnswer = false;
        vm.ResetManualCommand.Execute(null);
        Assert.True(vm.Table.HasManualEdits);

        harness.Dialogs.ConfirmAnswer = true;
        vm.ResetManualCommand.Execute(null);
        Assert.False(vm.Table.HasManualEdits);
        Assert.False(vm.CanUndo);
    }

    [Fact]
    public void Regenerating_true_type_keeps_a_manual_glyph_and_lists_a_missing_one()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        Assert.False(vm.Table.IsEmpty(0x41));
        Assert.Contains("0x42", vm.MissingText, StringComparison.Ordinal);
        Assert.DoesNotContain("0x41", vm.MissingText, StringComparison.Ordinal);

        vm.Editor.SelectedCode = 0x41;
        bool painted = vm.Table.GetGlyph(0x41)[0, 0];
        vm.Editor.ApplyStroke(new[] { (0, 0) }, StrokePaint.Toggle);
        vm.OffsetXText = "2";

        Assert.True(vm.Table.IsManual(0x41));
        Assert.NotEqual(painted, vm.Table.GetGlyph(0x41)[0, 0]);
    }

    [Fact]
    public void An_invalid_custom_range_blocks_generation()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.CustomRangeText = "nope";

        Assert.True(vm.HasErrors);
        Assert.DoesNotContain("unsigned char", vm.CodeText, StringComparison.Ordinal);

        vm.CustomRangeText = "0x41";
        Assert.False(vm.HasErrors);
        Assert.Contains("unsigned char", vm.CodeText, StringComparison.Ordinal);
    }

    [Fact]
    public void One_array_is_imported_without_a_dialog_and_several_arrays_use_the_choice()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        string single = Path.Combine(_directory, "one.c");
        File.WriteAllText(single, "unsigned char code font[] = { 0x01 };\n");
        vm.ImportFile(single);

        Assert.Equal(0, harness.Dialogs.ImportAsks);
        Assert.True(vm.Table.GetGlyph(0)[0, 0]);
        Assert.Contains("1536", vm.WarningText, StringComparison.Ordinal);
        Assert.True(vm.HasUnsavedWork());

        string many = Path.Combine(_directory, "two.c");
        File.WriteAllText(many, "unsigned char one[] = { 0x01 };\nunsigned char two[] = { 0x80 };\n");
        harness.Dialogs.ImportAnswer = new ImportPick(1, FontCellSize.Cell6x8, PackingOptions.Default);
        vm.ImportFile(many);

        Assert.Equal(1, harness.Dialogs.ImportAsks);
        Assert.False(vm.Table.GetGlyph(0)[0, 0]);
        Assert.True(vm.Table.GetGlyph(0)[0, 7]);
    }

    [Fact]
    public void Refusing_import_keeps_manual_edits()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.Editor.ApplyStroke(new[] { (2, 2) }, StrokePaint.Toggle);
        harness.Dialogs.ConfirmAnswer = false;
        string path = Path.Combine(_directory, "font.c");
        File.WriteAllText(path, "unsigned char code font[] = { 0x01 };\n");

        vm.ImportFile(path);

        Assert.True(vm.Table.IsManual(0));
        Assert.True(vm.Table.GetGlyph(0)[2, 2]);
        Assert.False(vm.Table.GetGlyph(0)[0, 0]);
    }

    [Fact]
    public void A_sheet_pixel_is_read_only_inside_the_selected_range()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.Latin = false;
        vm.Cyrillic = false;
        vm.CustomRangeText = "0x00";
        var image = new RgbaImage(6, 8, 255, 255, 255, 255);
        image.SetPixel(0, 0, 0, 0, 0, 255);

        vm.LoadSheet(image, Path.Combine(_directory, "sheet.png"));

        Assert.True(vm.Table.GetGlyph(0)[0, 0]);
        Assert.False(vm.Table.GetGlyph(0)[1, 0]);
        Assert.True(vm.Table.IsEmpty(1));
    }

    [Fact]
    public void Paste_fits_a_smaller_glyph_into_the_top_left()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        var small = new MonoBitmap(2, 1);
        small[0, 0] = true;
        harness.Clipboard.SetGlyph(small, invert: false);

        vm.Editor.PasteGlyph();

        Assert.True(vm.Table.GetGlyph(0)[0, 0]);
        Assert.False(vm.Table.GetGlyph(0)[1, 0]);
        Assert.True(vm.Table.IsManual(0));
    }

    [Fact]
    public void Characters_outside_cp1251_are_marked_in_the_preview_map()
    {
        IReadOnlyList<PreviewGlyph> cells = StringPreviewMap.Map("A\u0100");
        Assert.Equal(0x41, cells[0].Code);
        Assert.False(cells[0].OutsideEncoding);
        Assert.True(cells[1].OutsideEncoding);
        Assert.Null(cells[1].Code);
    }

    [Fact]
    public void Glyph_text_round_trips_with_and_without_inversion()
    {
        var glyph = new MonoBitmap(3, 2);
        glyph[0, 0] = true;
        glyph[2, 1] = true;
        Assert.True(GlyphClipboard.TryParsePayload(GlyphClipboard.ToPayload(glyph), out MonoBitmap? payload));
        Assert.True(payload![0, 0]);
        Assert.True(payload[2, 1]);

        Assert.Equal("#..\n..#", GlyphClipboard.ToText(glyph, invert: false));
        Assert.True(GlyphClipboard.TryParseText(GlyphClipboard.ToText(glyph, invert: true), invert: true, out MonoBitmap? text));
        Assert.True(text![0, 0]);
        Assert.True(text[2, 1]);
        Assert.False(text[1, 0]);
    }

    [Fact]
    public void Copy_save_and_user_preset_work_on_the_font_tab()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        vm.IncludeDate = false;
        vm.ArrayName = "font_a";
        Assert.True(vm.CopyCommand.CanExecute(null));

        vm.CopyCommand.Execute(null);

        Assert.Equal(vm.Document!.Files[0].Text, harness.Clipboard.Text);
        vm.CodeIndex = 1;
        vm.CopyCommand.Execute(null);
        Assert.Equal(vm.Document.Files[1].Text, harness.Clipboard.Text);

        string output = Path.Combine(_directory, "out");
        Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = Path.Combine(output, "font_a.c");
        string code = vm.CodeText;
        byte[] cBefore = vm.Document.Files[0].Content.ToArray();
        byte[] hBefore = vm.Document.Files[1].Content.ToArray();
        vm.SaveOutputCommand.Execute(null);

        Assert.Equal("Сохранить Код", harness.Files.LastTitle);
        Assert.Equal("font_a", harness.Files.LastFileName);
        Assert.Equal("Файлы кода (*.c, *.h)|*.c", harness.Files.LastFilter);
        Assert.Equal(".c", harness.Files.LastDefaultExt);
        Assert.Equal("font_a", vm.ArrayName);
        Assert.Equal(code, vm.CodeText);
        Assert.Equal(cBefore, vm.Document.Files[0].Content);
        Assert.Equal(hBefore, vm.Document.Files[1].Content);
        Assert.Equal(vm.Document.Files[0].Content, File.ReadAllBytes(Path.Combine(output, "font_a.c")));
        Assert.Equal(vm.Document.Files[1].Content, File.ReadAllBytes(Path.Combine(output, "font_a.h")));
        File.WriteAllText(Path.Combine(output, "font_a.h"), "old");
        harness.Dialogs.ConfirmAnswer = false;
        vm.SaveOutputCommand.Execute(null);
        Assert.Equal("old", File.ReadAllText(Path.Combine(output, "font_a.h")));

        vm.Direction = PackDirection.Horizontal;
        harness.Dialogs.TextAnswer = "Шрифт T6963C";
        vm.SavePresetCommand.Execute(null);

        Preset saved = Assert.Single(harness.Users.Presets);
        Assert.Equal(PackDirection.Horizontal, saved.Packing.Direction);
        Assert.Equal("Шрифт T6963C", vm.PresetCaption);
        harness.Dialogs.ConfirmAnswer = true;
        vm.DeletePresetCommand.Execute(null);
        Assert.Empty(harness.Users.Presets);
        Assert.Empty(harness.Dialogs.Alerts);
    }

    [Fact]
    public void Save_dialog_on_font_tab_receives_array_name_title_filter_and_initial_folder()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        string fontDir = Path.Combine(_directory, "font_dir");
        string sheetDir = Path.Combine(_directory, "sheet_dir");
        string importDir = Path.Combine(_directory, "import_dir");
        string outputDir = Path.Combine(_directory, "out_dir");
        Directory.CreateDirectory(fontDir);
        Directory.CreateDirectory(sheetDir);
        Directory.CreateDirectory(importDir);
        Directory.CreateDirectory(outputDir);

        vm.SetFolders(fontDir, null);
        harness.Files.NextSaveOutput = null;
        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(fontDir, harness.Files.LastFolder);
        Assert.Equal(vm.ArrayName, harness.Files.LastFileName);
        Assert.Equal("Сохранить Код", harness.Files.LastTitle);
        Assert.Equal("Файлы кода (*.c, *.h)|*.c", harness.Files.LastFilter);
        Assert.Equal(".c", harness.Files.LastDefaultExt);

        vm.LoadSheet(new RgbaImage(6, 8, 255, 255, 255, 255), Path.Combine(sheetDir, "sheet.png"));
        vm.SaveOutputCommand.Execute(null);
        Assert.Equal(sheetDir, harness.Files.LastFolder);

        string importPath = Path.Combine(importDir, "font_in.c");
        File.WriteAllText(importPath, "unsigned char code f[] = { 0x00 };\n");
        vm.ImportFile(importPath);
        vm.SaveOutputCommand.Execute(null);
        Assert.Equal(importDir, harness.Files.LastFolder);

        vm.SetFolders(fontDir, outputDir);
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

    public static TheoryData<string> NewFontCFileNames => new()
    {
        "logo_new",
        "logo_new.c",
        "logo_new.h",
    };

    [Theory]
    [MemberData(nameof(NewFontCFileNames))]
    public void Save_on_font_tab_with_a_new_name_updates_array_name_regenerates_files_and_marks_name_customized(string entered)
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        vm.IncludeDate = false;
        string output = Path.Combine(_directory, "out_" + entered.Replace('.', '_'));
        Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = Path.Combine(output, entered);

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal("logo_new", vm.ArrayName);
        string cPath = Path.Combine(output, "logo_new.c");
        string hPath = Path.Combine(output, "logo_new.h");
        Assert.True(File.Exists(cPath));
        Assert.True(File.Exists(hPath));
        string cText = File.ReadAllText(cPath);
        string hText = File.ReadAllText(hPath);
        Assert.Contains("#include \"logo_new.h\"", cText, StringComparison.Ordinal);
        Assert.Contains("logo_new[256][LOGO_NEW_BYTES_PER_CHAR]", cText, StringComparison.Ordinal);
        Assert.Contains("#define LOGO_NEW_CHAR_WIDTH", hText, StringComparison.Ordinal);
        Assert.Contains("#define LOGO_NEW_CHAR_HEIGHT", hText, StringComparison.Ordinal);
        Assert.Contains("#define LOGO_NEW_BYTES_PER_CHAR", hText, StringComparison.Ordinal);
        Assert.Equal(new[] { "logo_new.c", "logo_new.h" }, vm.CodeFiles.Select(f => f.Caption));
        Assert.Equal(vm.Document!.Files[0].Content, File.ReadAllBytes(cPath));
        Assert.Equal(vm.Document.Files[1].Content, File.ReadAllBytes(hPath));

        vm.Cell = FontCellSize.Cell8x8;
        Assert.Equal("logo_new", vm.ArrayName);
    }

    [Theory]
    [InlineData("logo_new.BIN", "logo_new")]
    [InlineData("LOGO_NEW.BIN", "LOGO_NEW")]
    public void Save_bin_on_font_tab_strips_uppercase_extension_and_updates_array_name(string entered, string expectedName)
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        vm.Format = OutputFormat.Bin;
        string output = Path.Combine(_directory, "out_bin_" + expectedName);
        Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = Path.Combine(output, entered);

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(expectedName, vm.ArrayName);
        string binPath = Path.Combine(output, expectedName + ".bin");
        Assert.True(File.Exists(binPath));
        Assert.Equal(new[] { expectedName + ".bin" }, vm.CodeFiles.Select(f => f.Caption));
        Assert.Equal(vm.Document!.Files[0].Content, File.ReadAllBytes(binPath));

        vm.Cell = FontCellSize.Cell8x8;
        Assert.Equal(expectedName, vm.ArrayName);
    }

    [Fact]
    public void Save_on_font_tab_with_a_deferred_scheduler_still_writes_files_with_the_new_name()
    {
        var scheduler = new DeferredRecalcScheduler();
        using var harness = new Harness(_directory, scheduler);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        vm.IncludeDate = false;
        string output = Path.Combine(_directory, "out_deferred");
        Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = Path.Combine(output, "font_deferred.c");
        scheduler.Hold = true;

        vm.SaveOutputCommand.Execute(null);

        Assert.True(scheduler.PendingCount > 0);
        Assert.Equal("font_deferred", vm.ArrayName);
        string cPath = Path.Combine(output, "font_deferred.c");
        string hPath = Path.Combine(output, "font_deferred.h");
        Assert.True(File.Exists(cPath));
        Assert.True(File.Exists(hPath));
        Assert.Contains("#include \"font_deferred.h\"", File.ReadAllText(cPath), StringComparison.Ordinal);
        Assert.Contains("#define FONT_DEFERRED_BYTES_PER_CHAR", File.ReadAllText(hPath), StringComparison.Ordinal);
        Assert.Equal(new[] { "font_deferred.c", "font_deferred.h" }, vm.CodeFiles.Select(f => f.Caption));

        scheduler.RunPending();
        Assert.Equal("font_deferred", vm.ArrayName);
        Assert.Equal(new[] { "font_deferred.c", "font_deferred.h" }, vm.CodeFiles.Select(f => f.Caption));
    }

    public static TheoryData<OutputFormat, string> InvalidFontNames => new()
    {
        { OutputFormat.CKeilC51, "1abc" },
        { OutputFormat.CKeilC51, "лого" },
        { OutputFormat.CKeilC51, "code" },
        { OutputFormat.CKeilC51, "my-name" },
        { OutputFormat.CKeilC51, "logo.v2" },
        { OutputFormat.A51Module, new string('a', 28) },
    };

    [Theory]
    [MemberData(nameof(InvalidFontNames))]
    public void Save_on_font_tab_with_an_invalid_name_alerts_and_reopens_the_dialog_with_the_entered_text(OutputFormat format, string badName)
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        vm.Format = format;
        string originalName = vm.ArrayName;
        string output = Path.Combine(_directory, "out_bad_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(output);
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
        Assert.Empty(Directory.GetFiles(output));
    }

    [Fact]
    public void Save_on_font_tab_after_correcting_an_invalid_name_in_the_reopened_dialog_writes_the_valid_files()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        string output = Path.Combine(_directory, "out_retry");
        Directory.CreateDirectory(output);
        harness.Files.NextSaveOutputs.Enqueue(Path.Combine(output, "1abc.c"));
        harness.Files.NextSaveOutputs.Enqueue(Path.Combine(output, "fixed_font.c"));

        vm.SaveOutputCommand.Execute(null);

        Assert.Single(harness.Dialogs.Alerts);
        Assert.Equal(2, harness.Files.SaveOutputCalls);
        Assert.Equal("fixed_font", vm.ArrayName);
        Assert.True(File.Exists(Path.Combine(output, "fixed_font.c")));
        Assert.True(File.Exists(Path.Combine(output, "fixed_font.h")));
    }

    [Fact]
    public void Cancelling_the_first_save_dialog_on_font_tab_writes_nothing_and_keeps_the_array_name()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        string originalName = vm.ArrayName;
        string output = Path.Combine(_directory, "out_cancel");
        Directory.CreateDirectory(output);
        harness.Files.NextSaveOutput = null;

        vm.SaveOutputCommand.Execute(null);

        Assert.Equal(1, harness.Files.SaveOutputCalls);
        Assert.Equal(originalName, vm.ArrayName);
        Assert.Empty(harness.Dialogs.Alerts);
        Assert.Empty(Directory.GetFiles(output));
    }

    [Fact]
    public void Typing_array_name_on_the_font_panel_and_focusing_save_button_flushes_the_name_before_the_dialog()
    {
        using var ui = new StaDispatcher();
        var scheduler = new DeferredRecalcScheduler();
        using var harness = new Harness(_directory, scheduler);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        string output = Path.Combine(_directory, "out_flush");
        Directory.CreateDirectory(output);
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
                    new System.Windows.Data.Binding(nameof(FontGeneratorViewModel.ArrayName))
                    {
                        Source = vm,
                        Mode = System.Windows.Data.BindingMode.TwoWay,
                        UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged,
                        Delay = 400,
                    });
                image2gdram_converter.Controls.TextBoxCommit.SetFlush(box, true);
                box.Focus();
                box.Text = "panel_font";
                Assert.Equal("stub_6x8", vm.ArrayName);

                harness.Files.NextSaveOutput = Path.Combine(output, "panel_font.c");
                button.Focus();
                Assert.Equal("panel_font", vm.ArrayName);

                button.Command.Execute(null);
                Assert.Equal("panel_font", harness.Files.LastFileName);
                Assert.True(File.Exists(Path.Combine(output, "panel_font.c")));
                Assert.Contains("#include \"panel_font.h\"", File.ReadAllText(Path.Combine(output, "panel_font.c")), StringComparison.Ordinal);
            });
        }
        finally
        {
            ui.Dispatcher.Invoke(() => window?.Close());
        }
    }

    [Theory]
    [InlineData(OutputFormat.A51Module, AsmFileExtension.A51, ".a51")]
    [InlineData(OutputFormat.A51Include, AsmFileExtension.Asm, ".asm")]
    [InlineData(OutputFormat.Bin, AsmFileExtension.A51, ".bin")]
    public void Single_output_file_keeps_its_name_above_the_code(OutputFormat format, AsmFileExtension extension, string suffix)
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        UseStub(vm);
        vm.Format = format;
        vm.AsmExtension = extension;

        CodeFileItem file = Assert.Single(vm.CodeFiles);
        Assert.Equal(vm.ArrayName + suffix, file.Caption);
        Assert.True(vm.ShowCodeFiles);
        Assert.Equal(file.Text, vm.CodeText);
        Assert.Equal(0, vm.CodeIndex);
    }

    [Fact]
    public void Long_family_automatic_name_fits_the_a51_module_and_the_include_fragment()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        vm.Family = "LongTypefaceNameForEmbeddedDisplay";
        Assert.Equal(NameValidator.MaxLength, vm.ArrayName.Length);

        vm.Format = OutputFormat.A51Module;

        Assert.InRange(vm.ArrayName.Length, 1, NameValidator.MaxLengthA51Module);
        Assert.Empty(NameErrors(vm));
        Assert.True(vm.CanCopy);

        vm.Format = OutputFormat.A51Include;

        Assert.Empty(NameErrors(vm));
        Assert.Equal(NameValidator.MaxLength, vm.ArrayName.Length);
        Assert.True(vm.CanCopy);
    }

    [Fact]
    public void Long_sheet_path_automatic_name_fits_the_a51_module()
    {
        using var harness = new Harness(_directory);
        FontGeneratorViewModel vm = harness.Font;
        string path = Path.Combine(_directory, "character_sheet_with_a_very_long_file_name.png");
        string full = DefaultNameBuilder.FromFile(path, 6, 8, ArrayNameKind.Font, NameValidator.MaxLength);
        Assert.Equal(NameValidator.MaxLength, full.Length);
        FontTabParameters current = vm.ToParameters();
        FontTabParameters parameters = current with
        {
            SourceKind = FontSourceKind.Sheet,
            Output = current.Output with { ArrayName = full },
        };

        vm.Load(new FontProjectTab(parameters, new FontTable(FontCellSize.Cell6x8), path));
        vm.Format = OutputFormat.A51Module;

        Assert.InRange(vm.ArrayName.Length, 1, NameValidator.MaxLengthA51Module);
        Assert.Empty(NameErrors(vm));
        Assert.True(vm.CanCopy);

        vm.Format = OutputFormat.A51Include;

        Assert.Empty(NameErrors(vm));
        Assert.Equal(full, vm.ArrayName);
    }

    private static string[] NameErrors(FontGeneratorViewModel vm) =>
        ((INotifyDataErrorInfo)vm).GetErrors(nameof(FontGeneratorViewModel.ArrayName)).Cast<object>()
            .Select(error => error.ToString()!)
            .ToArray();

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

    private static void UseStub(FontGeneratorViewModel vm)
    {
        vm.Family = "Stub";
        vm.Latin = false;
        vm.Cyrillic = false;
        vm.CustomRangeText = "0x41, 0x42";
    }

    private sealed class Harness : IDisposable
    {
        public Harness(string directory, IRecalcScheduler? scheduler = null)
        {
            var settings = new SettingsService(directory, directory).Load().Settings;
            Dialogs = new FakeDialogs();
            Clipboard = new FakeClipboard();
            Files = new FakeFiles();
            Users = settings.UserPresets;
            Font = new FontGeneratorViewModel(
                new MapText(TestFiles.LoadUiStrings()),
                Dialogs,
                Files,
                Clipboard,
                scheduler ?? new ImmediateRecalcScheduler(),
                new UnusedDecoder(),
                new StubOutlines(),
                PresetCatalog.Shared,
                settings.UserPresets,
                settings.Font);
        }

        public FakeDialogs Dialogs { get; }

        public FakeClipboard Clipboard { get; }

        public FakeFiles Files { get; }

        public UserPresetStore Users { get; }

        public FontGeneratorViewModel Font { get; }

        public void Dispose()
        {
        }
    }
}
