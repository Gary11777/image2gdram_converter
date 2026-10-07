using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Image2Gdram.Core.Text;

namespace Image2Gdram.UiProbe;

/// <summary>
/// Проверка интерфейса этапа 8: замеры п. 5.1 ТЗ на настоящем окне (<c>perf</c>) и отрисовка окна
/// минимального размера при масштабе Windows 100–200 % с поиском обрезанных элементов (<c>dpi</c>).
/// Запуск: <c>dotnet run -c Release --project tools/UiProbe -- [all|perf|dpi] [папка отчёта]</c>.
/// </summary>
public static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        string mode = args.Length > 0 ? args[0].ToLowerInvariant() : "all";
        string output = args.Length > 1 ? Path.GetFullPath(args[1]) : Path.Combine(RepoRoot(), "artifacts", "ui-probe");
        if (mode is not ("all" or "perf" or "dpi"))
        {
            Console.Error.WriteLine("Usage: UiProbe [all|perf|dpi] [output folder]");
            return 64;
        }

        Directory.CreateDirectory(output);
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        Console.OutputEncoding = Encoding.UTF8;
        Cp1251.RegisterEncodingProvider();
        var app = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        app.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("pack://application:,,,/image2gdram_converter;component/Resources/Shared.xaml", UriKind.Absolute),
        });

        int exit = 0;
        app.Startup += async (_, _) =>
        {
            try
            {
                var report = new StringBuilder();
                bool ok = true;
                if (mode is "all" or "perf")
                {
                    ok &= await PerfProbe.RunAsync(report, RepoRoot());
                }

                if (mode is "all" or "dpi")
                {
                    ok &= await DpiProbe.RunAsync(report, RepoRoot(), output);
                }

                string path = Path.Combine(output, "report.md");
                File.WriteAllText(path, report.ToString(), new UTF8Encoding(false));
                Console.WriteLine(report.ToString());
                Console.WriteLine("Report: " + path);
                exit = ok ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                exit = 2;
            }
            finally
            {
                app.Shutdown();
            }
        };
        app.Run();
        return exit;
    }

    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "specification.md")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }

    /// <summary>Ждёт, пока диспетчер обработает всё с приоритетом выше <paramref name="priority"/>.</summary>
    public static Task Idle(DispatcherPriority priority = DispatcherPriority.ContextIdle) =>
        Dispatcher.CurrentDispatcher.InvokeAsync(() => { }, priority).Task;
}
