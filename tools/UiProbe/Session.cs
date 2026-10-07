using System.Windows.Threading;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Settings;
using Image2Gdram.Fonts.Wpf;
using Image2Gdram.Imaging.Wic;
using image2gdram_converter;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.UiProbe;

/// <summary>Главное окно с настоящими планировщиком, декодером и шрифтами; настройки — во временной папке.</summary>
internal sealed class Session : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-probe-" + Guid.NewGuid().ToString("N"));

    public Session()
    {
        Directory.CreateDirectory(_directory);
        var settings = new SettingsService(_directory, _directory);
        var localization = new LocalizationService();
        localization.UseApplicationResources();
        Decoder = new WicImageDecoder();
        Model = new MainViewModel(
            settings,
            settings.Load(),
            localization,
            new SilentDialogs(),
            new NoFiles(),
            new NoClipboard(),
            () => new RecalcScheduler(Dispatcher.CurrentDispatcher),
            new ManualSettingsAutosave(),
            Decoder,
            new WpfGlyphOutlineProvider());
        Window = new MainWindow { DataContext = Model };
    }

    public WicImageDecoder Decoder { get; }

    public MainViewModel Model { get; }

    public MainWindow Window { get; }

    public void Dispose()
    {
        Window.Close();
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private sealed class SilentDialogs : IDialogService
    {
        public bool Confirm(string message) => true;

        public SaveChoice AskSave(string message) => SaveChoice.Discard;

        public string? AskText(string message, string initial) => null;

        public void Alert(string message) => Console.Error.WriteLine("ALERT: " + message);

        public ImportPick? AskImport(IReadOnlyList<ImportedArray> arrays, FontCellSize cell, PackingOptions packing) => null;
    }

    private sealed class NoFiles : IFileDialogService
    {
        public string? PickOpenImage(string? folder) => null;

        public string? PickOpenSheet(string? folder) => null;

        public string? PickOpenImport(string? folder) => null;

        public string? PickOpenProject(string? folder) => null;

        public string? PickSaveProject(string? folder) => null;

        public string? PickFolder(string? folder, string title) => null;
    }

    private sealed class NoClipboard : IClipboardService
    {
        public void SetText(string text)
        {
        }

        public void SetGlyph(MonoBitmap glyph, bool invert)
        {
        }

        public bool TryGetGlyph(bool invert, out MonoBitmap? glyph)
        {
            glyph = null;
            return false;
        }
    }
}
