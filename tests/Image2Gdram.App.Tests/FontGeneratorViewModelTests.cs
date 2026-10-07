using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
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
        harness.Files.NextFolder = output;
        vm.SaveOutputCommand.Execute(null);

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
        public Harness(string directory)
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
                new ImmediateRecalcScheduler(),
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
