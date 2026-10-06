using System.Windows;
using System.Windows.Threading;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Settings;
using Image2Gdram.Core.Text;
using Image2Gdram.Imaging.Wic;
using image2gdram_converter.Services;
using image2gdram_converter.ViewModels;

namespace image2gdram_converter;

public partial class App : Application
{
    private IDialogService? _dialogs;
    private ILocalizationService? _text;

    protected override void OnStartup(StartupEventArgs e)
    {
        Cp1251.RegisterEncodingProvider();
        DispatcherUnhandledException += OnDispatcherException;
        TaskScheduler.UnobservedTaskException += OnUnobserved;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;

        var localization = new LocalizationService();
        localization.UseApplicationResources();
        _text = localization;
        var dialogs = new DialogService(localization);
        _dialogs = dialogs;
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
                new RecalcScheduler(Dispatcher),
                new SettingsAutosave(Dispatcher),
                new WicImageDecoder()),
        };
        MainWindow = window;
        window.Show();
        base.OnStartup(e);
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs args)
    {
        args.Handled = true;
        Report(args.Exception);
    }

    private void OnUnobserved(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        args.SetObserved();
        Dispatcher.BeginInvoke(() => Report(args.Exception));
    }

    private void OnDomainException(object sender, UnhandledExceptionEventArgs args)
    {
        if (args.ExceptionObject is Exception exception)
        {
            Report(exception);
        }
    }

    private void Report(Exception exception)
    {
        string message = _text is null
            ? exception.Message
            : _text.Format("Error.Unexpected", exception.Message);
        if (_dialogs is not null)
        {
            _dialogs.Alert(message);
            return;
        }

        MessageBox.Show(message);
    }
}
