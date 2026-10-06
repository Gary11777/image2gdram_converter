using System.Windows.Threading;

namespace image2gdram_converter.Services;

/// <summary>
/// Ждёт короткую паузу, отменяет предыдущий запуск и считает в фоне.
/// Результат возвращается в поток диспетчера; устаревший результат отбрасывается.
/// </summary>
public sealed class RecalcScheduler : IRecalcScheduler
{
    private readonly DispatcherTimer _timer;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CancellationTokenSource? _cancel;
    private int _ticket;
    private Func<CancellationToken, object?>? _compute;
    private Action<object?>? _apply;
    private Action<Exception>? _onError;

    public RecalcScheduler(Dispatcher dispatcher, int delayMilliseconds = 40)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _timer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(delayMilliseconds),
        };
        _timer.Tick += OnTick;
    }

    public void Schedule(Func<CancellationToken, object?> compute, Action<object?> apply, Action<Exception> onError)
    {
        ArgumentNullException.ThrowIfNull(compute);
        ArgumentNullException.ThrowIfNull(apply);
        ArgumentNullException.ThrowIfNull(onError);
        _compute = compute;
        _apply = apply;
        _onError = onError;
        _timer.Stop();
        _timer.Start();
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        if (_compute is null || _apply is null || _onError is null)
        {
            return;
        }

        _cancel?.Cancel();
        var cancel = new CancellationTokenSource();
        _cancel = cancel;
        int ticket = ++_ticket;
        Func<CancellationToken, object?> compute = _compute;
        Action<object?> apply = _apply;
        Action<Exception> onError = _onError;
        try
        {
            object? result = await Task.Run(() => Run(compute, cancel.Token), cancel.Token).ConfigureAwait(true);
            if (ticket == _ticket && !cancel.IsCancellationRequested)
            {
                apply(result);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            if (ticket == _ticket)
            {
                onError(ex);
            }
        }
    }

    private object? Run(Func<CancellationToken, object?> compute, CancellationToken token)
    {
        _gate.Wait(token);
        try
        {
            token.ThrowIfCancellationRequested();
            return compute(token);
        }
        finally
        {
            _gate.Release();
        }
    }
}
