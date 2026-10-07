using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Settings;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.App.Tests;

public class MainViewModelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-main-" + Guid.NewGuid().ToString("N"));

    public MainViewModelTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Closing_without_a_project_or_edits_does_not_ask()
    {
        var dialogs = new FakeDialogs();
        MainViewModel main = Create(dialogs);
        main.Image.Threshold = 90;

        Assert.True(main.TryClose());
        Assert.Equal(0, dialogs.SaveCount);
    }

    [Fact]
    public void Closing_with_pixel_edits_asks_and_cancel_keeps_the_window()
    {
        var dialogs = new FakeDialogs { SaveAnswer = SaveChoice.Cancel };
        MainViewModel main = Create(dialogs);
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        main.Image.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "white.png", true, true);
        main.Image.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);

        Assert.False(main.TryClose());
        Assert.Equal(1, dialogs.SaveCount);
        Assert.True(main.Image.HasEdits);
    }

    [Fact]
    public void Discard_closes_and_still_writes_settings()
    {
        var dialogs = new FakeDialogs { SaveAnswer = SaveChoice.Discard };
        MainViewModel main = Create(dialogs);
        var image = new RgbaImage(8, 8, 255, 255, 255, 255);
        main.Image.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { image }), "white.png", true, true);
        main.Image.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);

        Assert.True(main.TryClose());

        SettingsLoadResult loaded = new SettingsService(_directory, _directory).Load();
        Assert.Null(loaded.Diagnostic);
        Assert.Equal(128, loaded.Settings.Image.Parameters.Processing.TargetWidth);
        Assert.Equal("white.png", Path.GetFileName(loaded.Settings.Image.SourcePath));
    }

    [Fact]
    public void Closing_with_a_drawn_glyph_asks_and_cancel_keeps_the_window()
    {
        var dialogs = new FakeDialogs { SaveAnswer = SaveChoice.Cancel };
        MainViewModel main = Create(dialogs);
        main.Font.Editor.ApplyStroke(new[] { (0, 0) }, StrokePaint.Toggle);

        Assert.False(main.TryClose());
        Assert.Equal(1, dialogs.SaveCount);
        Assert.True(main.Font.Table.IsManual(0));
    }

    [Fact]
    public void Parameter_change_is_saved_on_the_autosave_callback()
    {
        var dialogs = new FakeDialogs();
        var autosave = new ManualSettingsAutosave();
        MainViewModel main = Create(dialogs, autosave);
        main.Image.Threshold = 40;
        Assert.True(autosave.Scheduled > 0);

        autosave.RunPending();

        SettingsLoadResult loaded = new SettingsService(_directory, _directory).Load();
        Assert.Equal(40, loaded.Settings.Image.Parameters.Processing.Threshold);
    }

    [Fact]
    public void Exit_asks_the_window_to_close_so_that_cancel_in_the_prompt_keeps_it()
    {
        var dialogs = new FakeDialogs { SaveAnswer = SaveChoice.Cancel };
        MainViewModel main = Create(dialogs);
        main.Image.LoadDecoded(White(), "white.png", true, true);
        main.Image.ApplyStroke(new[] { (1, 1) }, StrokePaint.Dot);
        int requests = 0;
        main.CloseRequested += (_, _) => requests++;

        main.ExitCommand.Execute(null);

        Assert.Equal(1, requests);
        Assert.False(main.TryClose());
        Assert.True(main.Image.HasEdits);
    }

    [Fact]
    public void Output_of_one_tab_never_overwrites_a_source_file_of_the_other_tab()
    {
        var dialogs = new FakeDialogs { ConfirmAnswer = true };
        var files = new FakeFiles { NextFolder = _directory };
        MainViewModel main = Create(dialogs, files: files);
        string import = Path.Combine(_directory, "white_128x64.c");
        const string importText = "unsigned char code font[] = { 0x01 };\n";
        File.WriteAllText(import, importText);
        main.Font.ImportFile(import);
        main.Image.LoadDecoded(White(), Path.Combine(_directory, "white.png"), true, true);
        Assert.Equal("white_128x64", main.Image.ArrayName);

        main.Image.SaveOutputCommand.Execute(null);

        Assert.Equal(importText, File.ReadAllText(import));
        Assert.False(File.Exists(Path.Combine(_directory, "white_128x64.h")));
        Assert.Contains(dialogs.Alerts, alert => alert.Contains(import, StringComparison.OrdinalIgnoreCase));

        string picture = Path.Combine(_directory, "pic.bin");
        File.WriteAllBytes(picture, new byte[] { 1, 2, 3 });
        main.Image.LoadDecoded(White(), picture, true, true);
        main.Font.Format = OutputFormat.Bin;
        main.Font.ArrayName = "pic";

        main.Font.SaveOutputCommand.Execute(null);

        Assert.Equal(new byte[] { 1, 2, 3 }, File.ReadAllBytes(picture));
        Assert.Contains(dialogs.Alerts, alert => alert.Contains(picture, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Saved_settings_are_restored_into_both_tabs_and_a_corrupt_file_is_reported()
    {
        MainViewModel first = Create(new FakeDialogs());
        first.Image.Threshold = 77;
        first.Image.Invert = true;
        first.Image.Format = OutputFormat.CStm32;
        first.Font.Invert = true;
        first.Font.PreviewText = "Тест 42";
        Assert.True(first.TryClose());

        MainViewModel second = Create(new FakeDialogs());

        Assert.Equal(77, second.Image.Threshold);
        Assert.True(second.Image.Invert);
        Assert.Equal(OutputFormat.CStm32, second.Image.Format);
        Assert.True(second.Font.Invert);
        Assert.Equal("Тест 42", second.Font.PreviewText);
        Assert.Equal(string.Empty, second.StatusText);

        File.WriteAllText(Path.Combine(_directory, "settings.json"), "{ broken");
        MainViewModel third = Create(new FakeDialogs());

        Assert.NotEqual(string.Empty, third.StatusText);
        Assert.Equal(128, third.Image.Threshold);
        Assert.False(third.Font.Invert);
    }

    [Fact]
    public void Saved_project_reopens_with_parameters_edits_and_glyphs()
    {
        string image = Path.Combine(_directory, "pic.png");
        File.WriteAllBytes(image, new byte[] { 0 });
        var decoder = new MapDecoder();
        decoder.Add(image, White());
        string project = Path.Combine(_directory, "work.iiu");
        MainViewModel first = Create(new FakeDialogs(), files: new FakeFiles { NextSaveProject = project }, decoder: decoder);
        first.Image.LoadDecoded(decoder.Decode(image), image, true, true);
        first.Image.Threshold = 99;
        first.Image.ApplyStroke(new[] { (2, 3) }, StrokePaint.Dot);
        first.Font.Invert = true;
        first.Font.Editor.ApplyStroke(new[] { (1, 1) }, StrokePaint.Toggle);

        first.SaveProjectCommand.Execute(null);

        Assert.True(File.Exists(project));
        MainViewModel second = Create(new FakeDialogs(), files: new FakeFiles { NextOpenProject = project }, decoder: decoder);
        second.OpenProjectCommand.Execute(null);

        Assert.Equal(99, second.Image.Threshold);
        Assert.Equal(Path.GetFullPath(image), Path.GetFullPath(second.Image.SourcePath!));
        Assert.True(second.Image.HasEdits);
        Assert.True(second.Image.Preview![2, 3]);
        Assert.True(second.Font.Invert);
        Assert.True(second.Font.Table.IsManual(0));
        Assert.True(second.Font.Table.GetGlyph(0)[1, 1]);
        Assert.Contains("work.iiu", second.WindowTitle, StringComparison.Ordinal);
        Assert.True(second.TryClose());
    }

    [Fact]
    public void Startup_note_is_shown_in_the_status_bar()
    {
        MainViewModel main = Create(new FakeDialogs(), startupNote: "Словарь en-US не найден");

        Assert.Contains("Словарь en-US не найден", main.StatusText, StringComparison.Ordinal);
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

    private static DecodedImage White() =>
        new(ImageFileFormat.Png, new[] { new RgbaImage(8, 8, 255, 255, 255, 255) });

    private MainViewModel Create(
        FakeDialogs dialogs,
        ManualSettingsAutosave? autosave = null,
        FakeFiles? files = null,
        IImageDecoder? decoder = null,
        string? startupNote = null)
    {
        var service = new SettingsService(_directory, _directory);
        SettingsLoadResult loaded = service.Load();
        return new MainViewModel(
            service,
            loaded,
            new MapText(TestFiles.LoadUiStrings()),
            dialogs,
            files ?? new FakeFiles(),
            new FakeClipboard(),
            () => new ImmediateRecalcScheduler(),
            autosave ?? new ManualSettingsAutosave(),
            decoder ?? new UnusedDecoder(),
            new StubOutlines(),
            startupNote);
    }
}
