using System.Collections.Concurrent;
using System.Diagnostics;
using image2gdram_converter.Services;

namespace Image2Gdram.App.Tests;

public class RecalcSchedulerTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void Rapid_changes_within_the_pause_are_computed_once_with_the_last_parameters()
    {
        using var ui = new StaDispatcher();
        var applied = new ConcurrentQueue<(object? Value, int Thread)>();
        int computes = 0;
        ui.Dispatcher.Invoke(() =>
        {
            var scheduler = new RecalcScheduler(ui.Dispatcher, 30);
            for (int i = 1; i <= 5; i++)
            {
                int value = i;
                scheduler.Schedule(
                    _ =>
                    {
                        Interlocked.Increment(ref computes);
                        return value;
                    },
                    result => applied.Enqueue((result, Environment.CurrentManagedThreadId)),
                    _ => { });
            }
        });

        WaitFor(() => !applied.IsEmpty);
        Thread.Sleep(150);
        ui.Flush();

        (object? value, int thread) = Assert.Single(applied);
        Assert.Equal(5, value);
        Assert.Equal(ui.ThreadId, thread);
        Assert.Equal(1, Volatile.Read(ref computes));
    }

    [Fact]
    public void A_new_change_cancels_the_running_computation_and_only_the_new_result_is_applied()
    {
        using var ui = new StaDispatcher();
        var applied = new ConcurrentQueue<object?>();
        using var started = new ManualResetEventSlim();
        CancellationToken first = default;
        RecalcScheduler scheduler = ui.Dispatcher.Invoke(() => new RecalcScheduler(ui.Dispatcher, 10));
        ui.Dispatcher.Invoke(() => scheduler.Schedule(
            token =>
            {
                first = token;
                started.Set();
                token.WaitHandle.WaitOne(Timeout);
                token.ThrowIfCancellationRequested();
                return "old";
            },
            applied.Enqueue,
            _ => { }));
        Assert.True(started.Wait(Timeout));

        var watch = Stopwatch.StartNew();
        Assert.Equal(7, ui.Dispatcher.Invoke(() => 7));
        Assert.True(watch.ElapsedMilliseconds < 1000, "The UI thread must not wait for the computation.");
        ui.Dispatcher.Invoke(() => scheduler.Schedule(_ => "new", applied.Enqueue, _ => { }));

        WaitFor(() => !applied.IsEmpty);
        Thread.Sleep(100);
        ui.Flush();

        Assert.Equal("new", Assert.Single(applied));
        Assert.True(first.IsCancellationRequested);
    }

    [Fact]
    public void A_late_result_of_a_computation_that_ignores_cancellation_is_dropped()
    {
        using var ui = new StaDispatcher();
        var applied = new ConcurrentQueue<object?>();
        using var started = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        RecalcScheduler scheduler = ui.Dispatcher.Invoke(() => new RecalcScheduler(ui.Dispatcher, 10));
        ui.Dispatcher.Invoke(() => scheduler.Schedule(
            _ =>
            {
                started.Set();
                release.Wait(Timeout);
                return "stale";
            },
            applied.Enqueue,
            _ => { }));
        Assert.True(started.Wait(Timeout));
        ui.Dispatcher.Invoke(() => scheduler.Schedule(_ => "fresh", applied.Enqueue, _ => { }));
        Thread.Sleep(50);

        release.Set();
        WaitFor(() => !applied.IsEmpty);
        Thread.Sleep(100);
        ui.Flush();

        Assert.Equal("fresh", Assert.Single(applied));
    }

    [Fact]
    public void A_failing_computation_reports_the_error_on_the_ui_thread_and_applies_nothing()
    {
        using var ui = new StaDispatcher();
        var applied = new ConcurrentQueue<object?>();
        var errors = new ConcurrentQueue<(Exception Error, int Thread)>();
        ui.Dispatcher.Invoke(() => new RecalcScheduler(ui.Dispatcher, 10).Schedule(
            _ => throw new InvalidOperationException("compute failed"),
            applied.Enqueue,
            error => errors.Enqueue((error, Environment.CurrentManagedThreadId))));

        WaitFor(() => !errors.IsEmpty);

        (Exception error, int thread) = Assert.Single(errors);
        Assert.Equal("compute failed", error.Message);
        Assert.Equal(ui.ThreadId, thread);
        Assert.Empty(applied);
    }

    [Fact]
    public void Both_tabs_get_their_results_when_they_change_within_the_same_pause()
    {
        using var ui = new StaDispatcher();
        string directory = Path.Combine(Path.GetTempPath(), "i2g-sched-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            image2gdram_converter.ViewModels.MainViewModel model = ui.Dispatcher.Invoke(() =>
            {
                var service = new Image2Gdram.Core.Settings.SettingsService(directory, directory);
                var vm = new image2gdram_converter.ViewModels.MainViewModel(
                    service,
                    service.Load(),
                    new MapText(TestFiles.LoadUiStrings()),
                    new FakeDialogs(),
                    new FakeFiles(),
                    new FakeClipboard(),
                    () => new RecalcScheduler(ui.Dispatcher, 30),
                    new ManualSettingsAutosave(),
                    new UnusedDecoder(),
                    new StubOutlines());
                var white = new Image2Gdram.Core.Imaging.RgbaImage(8, 8, 255, 255, 255, 255);
                vm.Image.LoadDecoded(
                    new Image2Gdram.Core.Imaging.DecodedImage(Image2Gdram.Core.Imaging.ImageFileFormat.Png, new[] { white }),
                    "white.png",
                    resetFrame: true,
                    userAction: false);
                vm.Font.Family = "Stub";
                return vm;
            });

            WaitFor(() => ui.Dispatcher.Invoke(() => model.Image.Document is not null && model.Font.Document is not null));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static void WaitFor(Func<bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition())
        {
            Assert.True(watch.Elapsed < Timeout, "Timed out.");
            Thread.Sleep(5);
        }
    }
}
