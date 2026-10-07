using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Packing;
using image2gdram_converter.Services;

namespace Image2Gdram.App.Tests;

public class UnhandledExceptionReporterTests
{
    [Fact]
    public void Dispatcher_exception_is_reported_on_the_ui_thread_and_the_program_keeps_running()
    {
        using var ui = new StaDispatcher();
        var dialogs = new RecordingDialogs();
        var reporter = Create(ui, dialogs, out _);
        ui.Dispatcher.UnhandledException += reporter.OnDispatcherException;

        ui.Dispatcher.BeginInvoke(new Action(() => throw new InvalidOperationException("boom-dispatcher")));
        ui.Flush();

        (string message, int thread) = Assert.Single(dialogs.Alerts);
        Assert.Equal("Необработанная ошибка: boom-dispatcher", message);
        Assert.Equal(ui.ThreadId, thread);
        Assert.Equal(42, ui.Dispatcher.Invoke(() => 42));
    }

    [Fact]
    public void Unobserved_task_exception_is_observed_and_reported_on_the_ui_thread()
    {
        using var ui = new StaDispatcher();
        var dialogs = new RecordingDialogs();
        var reporter = Create(ui, dialogs, out _);
        TaskScheduler.UnobservedTaskException += reporter.OnUnobservedTaskException;
        try
        {
            for (int attempt = 0; attempt < 5 && !dialogs.Alerts.Any(item => item.Message.Contains("boom-task")); attempt++)
            {
                DropFaultedTask();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                ui.Flush();
            }
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= reporter.OnUnobservedTaskException;
        }

        (string message, int thread) = dialogs.Alerts.First(item => item.Message.Contains("boom-task"));
        Assert.Equal("Необработанная ошибка: boom-task", message);
        Assert.Equal(ui.ThreadId, thread);
    }

    [Fact]
    public async Task Domain_exception_from_a_worker_thread_is_shown_on_the_ui_thread_before_the_handler_returns()
    {
        using var ui = new StaDispatcher();
        var dialogs = new RecordingDialogs();
        var reporter = Create(ui, dialogs, out _);

        await Task.Run(() => reporter.OnDomainException(null, new UnhandledExceptionEventArgs(new InvalidOperationException("boom-domain"), true)));

        (string message, int thread) = Assert.Single(dialogs.Alerts);
        Assert.Equal("Необработанная ошибка: boom-domain", message);
        Assert.Equal(ui.ThreadId, thread);
    }

    [Fact]
    public async Task Domain_exception_that_does_not_terminate_is_queued_to_the_ui_thread()
    {
        using var ui = new StaDispatcher();
        var dialogs = new RecordingDialogs();
        var reporter = Create(ui, dialogs, out _);

        await Task.Run(() => reporter.OnDomainException(null, new UnhandledExceptionEventArgs(new InvalidOperationException("later"), false)));
        ui.Flush();

        (_, int thread) = Assert.Single(dialogs.Alerts);
        Assert.Equal(ui.ThreadId, thread);
    }

    [Fact]
    public void Failing_dialog_falls_back_and_nothing_escapes()
    {
        using var ui = new StaDispatcher();
        var dialogs = new RecordingDialogs { Throw = true };
        var reporter = Create(ui, dialogs, out ConcurrentQueue<string> fallback);
        ui.Dispatcher.UnhandledException += reporter.OnDispatcherException;

        ui.Dispatcher.BeginInvoke(new Action(() => throw new InvalidOperationException("boom-fallback")));
        ui.Flush();

        Assert.Equal("Необработанная ошибка: boom-fallback", Assert.Single(fallback));
        Assert.Equal(1, ui.Dispatcher.Invoke(() => 1));
    }

    [Fact]
    public void An_error_while_the_message_is_open_does_not_open_another_one()
    {
        using var ui = new StaDispatcher();
        var dialogs = new RecordingDialogs();
        var reporter = Create(ui, dialogs, out _);
        ui.Dispatcher.UnhandledException += reporter.OnDispatcherException;
        dialogs.OnAlert = () =>
        {
            var frame = new DispatcherFrame();
            ui.Dispatcher.BeginInvoke(new Action(() => throw new InvalidOperationException("again")));
            ui.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        };

        ui.Dispatcher.BeginInvoke(new Action(() => throw new InvalidOperationException("first")));
        ui.Flush();

        Assert.Single(dialogs.Alerts);
        Assert.Equal(1, ui.Dispatcher.Invoke(() => reporter.ReportedCount));
        Assert.Equal(1, ui.Dispatcher.Invoke(() => reporter.SuppressedCount));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void DropFaultedTask()
    {
        Task task = Task.Run(() => throw new InvalidOperationException("boom-task"));
        ((IAsyncResult)task).AsyncWaitHandle.WaitOne();
        while (!task.IsCompleted)
        {
            Thread.Yield();
        }
    }

    private static UnhandledExceptionReporter Create(StaDispatcher ui, RecordingDialogs dialogs, out ConcurrentQueue<string> fallback)
    {
        var queue = new ConcurrentQueue<string>();
        fallback = queue;
        return new UnhandledExceptionReporter(ui.Dispatcher, dialogs, new MapText(TestFiles.LoadUiStrings()), queue.Enqueue);
    }

    private sealed class RecordingDialogs : IDialogService
    {
        private readonly ConcurrentQueue<(string Message, int Thread)> _alerts = new();

        public bool Throw { get; init; }

        public Action? OnAlert { get; set; }

        public IReadOnlyList<(string Message, int Thread)> Alerts => _alerts.ToArray();

        public void Alert(string message)
        {
            _alerts.Enqueue((message, Environment.CurrentManagedThreadId));
            if (Throw)
            {
                throw new InvalidOperationException("dialog failed");
            }

            Action? next = OnAlert;
            OnAlert = null;
            next?.Invoke();
        }

        public bool Confirm(string message) => true;

        public SaveChoice AskSave(string message) => SaveChoice.Discard;

        public string? AskText(string message, string initial) => null;

        public ImportPick? AskImport(IReadOnlyList<ImportedArray> arrays, FontCellSize cell, PackingOptions packing) => null;
    }
}
