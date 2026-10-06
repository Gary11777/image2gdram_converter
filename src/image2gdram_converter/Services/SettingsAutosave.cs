using System.Windows.Threading;

namespace image2gdram_converter.Services;

/// <summary>Пишет настройки через секунду после последнего изменения (решение N-25).</summary>
public sealed class SettingsAutosave : ISettingsAutosave
{
    private readonly DispatcherTimer _timer;
    private Action? _save;

    public SettingsAutosave(Dispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromSeconds(1),
        };
        _timer.Tick += (_, _) =>
        {
            _timer.Stop();
            _save?.Invoke();
        };
    }

    public void Schedule(Action save)
    {
        ArgumentNullException.ThrowIfNull(save);
        _save = save;
        _timer.Stop();
        _timer.Start();
    }

    public void Cancel() => _timer.Stop();
}
