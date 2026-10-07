using System.Windows;
using Image2Gdram.Core.Settings;
using Image2Gdram.Core.Text;
using Image2Gdram.Fonts.Wpf;
using Image2Gdram.Imaging.Wic;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace image2gdram_converter;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Cp1251.RegisterEncodingProvider();
        var localization = new LocalizationService();
        localization.UseApplicationResources();
        var dialogs = new DialogService(localization);
        var reporter = new UnhandledExceptionReporter(Dispatcher, dialogs, localization, message => MessageBox.Show(message));
        DispatcherUnhandledException += reporter.OnDispatcherException;
        TaskScheduler.UnobservedTaskException += reporter.OnUnobservedTaskException;
        AppDomain.CurrentDomain.UnhandledException += reporter.OnDomainException;

        var settingsService = new SettingsService();
        SettingsLoadResult loaded = settingsService.Load();
        localization.TryLoadExternal(loaded.Settings.Language, AppContext.BaseDirectory);

        var window = new MainWindow
        {
            DataContext = new MainViewModel(
                settingsService,
                loaded,
                localization,
                dialogs,
                new FileDialogService(localization),
                new ClipboardService(),
                () => new RecalcScheduler(Dispatcher),
                new SettingsAutosave(Dispatcher),
                new WicImageDecoder(),
                new WpfGlyphOutlineProvider()),
        };
        MainWindow = window;
        window.Show();
        base.OnStartup(e);
    }
}
