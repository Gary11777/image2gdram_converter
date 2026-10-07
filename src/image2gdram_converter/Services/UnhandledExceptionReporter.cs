using System.Windows.Threading;

namespace image2gdram_converter.Services;

/// <summary>
/// Три глобальных обработчика (п. 6 ТЗ, решение N-56): исключение в потоке интерфейса,
/// задача без наблюдателя и необработанное исключение домена приложения.
/// Сообщение с причиной всегда показывается в потоке интерфейса; пока одно сообщение открыто,
/// следующие не показываются, чтобы повторяющаяся ошибка не открывала окна без конца.
/// Сам обработчик исключений не выпускает.
/// </summary>
public sealed class UnhandledExceptionReporter
{
    private static readonly TimeSpan MarshalTimeout = TimeSpan.FromSeconds(30);

    private readonly Dispatcher _dispatcher;
    private readonly IDialogService _dialogs;
    private readonly ILocalizationService _text;
    private readonly Action<string> _fallback;
    private int _showing;

    public UnhandledExceptionReporter(
        Dispatcher dispatcher,
        IDialogService dialogs,
        ILocalizationService text,
        Action<string> fallback)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(fallback);
        _dispatcher = dispatcher;
        _dialogs = dialogs;
        _text = text;
        _fallback = fallback;
    }

    /// <summary>Сколько сообщений показано (в том числе запасным способом).</summary>
    public int ReportedCount { get; private set; }

    /// <summary>Сколько сообщений пропущено, потому что предыдущее ещё открыто.</summary>
    public int SuppressedCount { get; private set; }

    /// <summary><c>Application.DispatcherUnhandledException</c>: ошибка помечается обработанной, программа продолжает работу.</summary>
    public void OnDispatcherException(object? sender, DispatcherUnhandledExceptionEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        args.Handled = true;
        Report(args.Exception, terminating: false);
    }

    /// <summary><c>TaskScheduler.UnobservedTaskException</c>: приходит из потока финализатора.</summary>
    public void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        args.SetObserved();
        Report(Unwrap(args.Exception), terminating: false);
    }

    /// <summary>
    /// <c>AppDomain.UnhandledException</c>: приходит из потока, где возникла ошибка.
    /// Если процесс завершается, сообщение показывается синхронно, до выхода из обработчика.
    /// </summary>
    public void OnDomainException(object? sender, UnhandledExceptionEventArgs args)
    {
        ArgumentNullException.ThrowIfNull(args);
        if (args.ExceptionObject is Exception exception)
        {
            Report(exception, args.IsTerminating);
        }
    }

    public string Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        try
        {
            return _text.Format("Error.Unexpected", exception.Message);
        }
        catch (Exception)
        {
            return exception.Message;
        }
    }

    private void Report(Exception exception, bool terminating)
    {
        string message = Describe(exception);
        try
        {
            if (_dispatcher.CheckAccess())
            {
                Show(message);
            }
            else if (_dispatcher.HasShutdownStarted)
            {
                Fallback(message);
            }
            else if (terminating)
            {
                _dispatcher.Invoke(() => Show(message), DispatcherPriority.Send, CancellationToken.None, MarshalTimeout);
            }
            else
            {
                _dispatcher.BeginInvoke(() => Show(message));
            }
        }
        catch (Exception)
        {
            Fallback(message);
        }
    }

    private void Show(string message)
    {
        if (_showing > 0)
        {
            SuppressedCount++;
            return;
        }

        _showing++;
        try
        {
            ReportedCount++;
            _dialogs.Alert(message);
        }
        catch (Exception)
        {
            Fallback(message);
        }
        finally
        {
            _showing--;
        }
    }

    private void Fallback(string message)
    {
        try
        {
            _fallback(message);
        }
        catch (Exception)
        {
        }
    }

    private static Exception Unwrap(AggregateException exception)
    {
        AggregateException flat = exception.Flatten();
        return flat.InnerExceptions.Count == 1 ? flat.InnerExceptions[0] : flat;
    }
}
