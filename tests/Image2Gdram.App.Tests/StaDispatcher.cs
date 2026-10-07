using System.Windows.Threading;

namespace Image2Gdram.App.Tests;

/// <summary>Отдельный STA-поток с работающим диспетчером, как поток интерфейса программы.</summary>
internal sealed class StaDispatcher : IDisposable
{
    private readonly Thread _thread;

    public StaDispatcher()
    {
        using var ready = new ManualResetEventSlim();
        Dispatcher? dispatcher = null;
        _thread = new Thread(() =>
        {
            dispatcher = Dispatcher.CurrentDispatcher;
            ready.Set();
            Dispatcher.Run();
        })
        {
            IsBackground = true,
            Name = "test-ui",
        };
        _thread.SetApartmentState(ApartmentState.STA);
        _thread.Start();
        ready.Wait();
        Dispatcher = dispatcher!;
    }

    public Dispatcher Dispatcher { get; }

    public int ThreadId => _thread.ManagedThreadId;

    /// <summary>Ждёт, пока диспетчер обработает всё, что поставлено в очередь с приоритетом выше фонового.</summary>
    public void Flush() => Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle);

    public void Dispose()
    {
        Dispatcher.InvokeShutdown();
        _thread.Join(TimeSpan.FromSeconds(10));
    }
}
