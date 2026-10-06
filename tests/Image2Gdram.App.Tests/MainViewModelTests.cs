using Image2Gdram.Core.Imaging;
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

    private MainViewModel Create(FakeDialogs dialogs, ManualSettingsAutosave? autosave = null)
    {
        var service = new SettingsService(_directory, _directory);
        SettingsLoadResult loaded = service.Load();
        return new MainViewModel(
            service,
            loaded,
            new MapText(TestFiles.LoadUiStrings()),
            dialogs,
            new FakeFiles(),
            new FakeClipboard(),
            new ImmediateRecalcScheduler(),
            autosave ?? new ManualSettingsAutosave(),
            new UnusedDecoder());
    }
}
