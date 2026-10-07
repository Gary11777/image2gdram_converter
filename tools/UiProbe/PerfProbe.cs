using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Threading;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Processing;
using image2gdram_converter;
using image2gdram_converter.ViewModels;

namespace Image2Gdram.UiProbe;

/// <summary>
/// Замеры п. 5.1 ТЗ (решение N-58): время от изменения параметра во ViewModel до того, как новый код
/// попал в окно и диспетчер закончил разметку и отрисовку. В замер входят пауза планировщика (40 мс),
/// расчёт в фоне, заполнение окна кода и сетки.
/// </summary>
internal static class PerfProbe
{
    private const int Warmup = 2;
    private const int Runs = 9;

    public static async Task<bool> RunAsync(StringBuilder report, string root)
    {
        using var session = new Session();
        session.Window.Show();
        await Program.Idle();

        var rows = new List<Row>();
        ImageConverterViewModel image = session.Model.Image;
        FontGeneratorViewModel font = session.Model.Font;

        string pattern = Path.Combine(root, "testdata", "test_pattern_240x128.png");
        await Setup(() =>
        {
            image.LoadDecoded(session.Decoder.Decode(pattern), pattern, resetFrame: true, userAction: false);
            image.Presets.Selected = image.Presets.Items.First(item => item.Preset?.Name == "WG240128A");
        });
        await ImageScenarios(rows, "240x128", 200, image, "200", "240");

        await Setup(() =>
        {
            image.WidthText = "1024";
            image.HeightText = "1024";
            image.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { Photo(1024, 1024) }), "photo_1024.png", resetFrame: true, userAction: false);
            image.Direction = PackDirection.Vertical;
        });
        await ImageScenarios(rows, "1024x1024", 1000, image, "1000", "1024");
        await Setup(() =>
            image.LoadDecoded(new DecodedImage(ImageFileFormat.Png, new[] { Photo(2048, 2048) }), "photo_2048.png", resetFrame: true, userAction: false));
        rows.Add(await Scenario("1024x1024 (исходник 2048x2048)", 1000, image, "масштабирование", () => image.WidthText = "1000", () => image.WidthText = "1024"));
        rows.Add(await Scenario("1024x1024 (исходник 2048x2048)", 1000, image, "порог", () => image.Threshold = 90, () => image.Threshold = 170));

        session.Model.SelectedTab = 1;
        await Setup(() =>
        {
            font.SourceKind = FontSourceKind.TrueType;
            font.Family = "Consolas";
            font.RenderMode = GlyphRenderMode.Antialiased;
        });
        foreach ((FontCellSize cell, int size) in new[] { (FontCellSize.Cell6x8, 8), (FontCellSize.Cell8x8, 9), (FontCellSize.Cell12x16, 16) })
        {
            await Setup(() =>
            {
                font.Cell = cell;
                font.FontSizeText = size.ToString(CultureInfo.InvariantCulture);
            });
            string group = "шрифт " + cell.Width.ToString(CultureInfo.InvariantCulture) + "x" + cell.Height.ToString(CultureInfo.InvariantCulture);
            string next = (size + 1).ToString(CultureInfo.InvariantCulture);
            string back = size.ToString(CultureInfo.InvariantCulture);
            rows.Add(await Scenario(group, 200, font, "размер шрифта (растеризация 256 символов)", () => font.FontSizeText = next, () => font.FontSizeText = back));
            rows.Add(await Scenario(group, 200, font, "жирный", () => font.Bold = !font.Bold, () => font.Bold = !font.Bold));
            rows.Add(await Scenario(group, 200, font, "порог сглаживания", () => font.GlyphThreshold = 90, () => font.GlyphThreshold = 170));
            rows.Add(await Scenario(group, 200, font, "инверсия", () => font.Invert = !font.Invert, () => font.Invert = !font.Invert));
            rows.Add(await Scenario(group, 200, font, "направление упаковки", () => font.Direction = Flip(font.Direction), () => font.Direction = Flip(font.Direction)));
            rows.Add(await Scenario(group, 200, font, "формат A51 / C51", () => font.Format = font.Format == OutputFormat.A51Module ? OutputFormat.CKeilC51 : OutputFormat.A51Module, () => font.Format = font.Format == OutputFormat.A51Module ? OutputFormat.CKeilC51 : OutputFormat.A51Module));
            rows.Add(await Scenario(group, 200, font, "правка точки в редакторе", () => font.Editor.ApplyStroke(new[] { (1, 1) }, StrokePaint.Toggle), () => font.Editor.ApplyStroke(new[] { (1, 1) }, StrokePaint.Toggle)));
        }

        bool ok = rows.All(row => row.RenderMax <= row.Limit);
        report.AppendLine("# Замеры п. 5.1 ТЗ");
        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"Дата: {DateTime.Now:yyyy-MM-dd HH:mm}; машина: {Environment.MachineName}, {Environment.ProcessorCount} логических процессоров; .NET {Environment.Version}; сборка {Build()}.");
        report.AppendLine(CultureInfo.InvariantCulture, $"Метод: изменение параметра во ViewModel настоящего окна → новый код в окне и завершённая отрисовка (диспетчер дошёл до ContextIdle). В замер входит пауза планировщика 40 мс. Прогрев {Warmup}, замеров {Runs}; в таблице медиана и максимум, мс.");
        report.AppendLine();
        report.AppendLine("| Объект | Изменение | Код готов, медиана | Отрисовано, медиана | Отрисовано, максимум | Предел | Итог |");
        report.AppendLine("|---|---|---:|---:|---:|---:|---|");
        foreach (Row row in rows)
        {
            report.AppendLine(CultureInfo.InvariantCulture, $"| {row.Group} | {row.Change} | {row.ApplyMedian:F0} | {row.RenderMedian:F0} | {row.RenderMax:F0} | {row.Limit} | {(row.RenderMax <= row.Limit ? "OK" : "ПРЕВЫШЕН")} |");
        }

        report.AppendLine();
        report.AppendLine(ok ? "Все замеры укладываются в пределы п. 5.1." : "Есть превышения пределов п. 5.1.");
        report.AppendLine();
        return ok;
    }

    private static async Task ImageScenarios(List<Row> rows, string group, int limit, ImageConverterViewModel vm, string otherWidth, string width)
    {
        rows.Add(await Scenario(group, limit, vm, "порог (ползунок)", () => vm.Threshold = 90, () => vm.Threshold = 170));
        rows.Add(await Scenario(group, limit, vm, "Флойд — Стейнберг / порог", () => vm.Binarize = BinarizeMode.FloydSteinberg, () => vm.Binarize = BinarizeMode.Threshold));
        rows.Add(await Scenario(group, limit, vm, "Байер 8x8 / Аткинсон", () => vm.Binarize = BinarizeMode.Bayer8, () => vm.Binarize = BinarizeMode.Atkinson));
        rows.Add(await Scenario(group, limit, vm, "инверсия", () => vm.Invert = !vm.Invert, () => vm.Invert = !vm.Invert));
        rows.Add(await Scenario(group, limit, vm, "направление упаковки", () => vm.Direction = Flip(vm.Direction), () => vm.Direction = Flip(vm.Direction)));
        rows.Add(await Scenario(group, limit, vm, "формат A51 / C51", () => vm.Format = OutputFormat.A51Module, () => vm.Format = OutputFormat.CKeilC51));
        rows.Add(await Scenario(group, limit, vm, "формат STM32 / BIN", () => vm.Format = OutputFormat.CStm32, () => vm.Format = OutputFormat.Bin));
        rows.Add(await Scenario(group, limit, vm, "масштабирование (ширина)", () => vm.WidthText = otherWidth, () => vm.WidthText = width));
        rows.Add(await Scenario(group, limit, vm, "ближайший сосед / усреднение", () => vm.Resample = ResampleMode.NearestNeighbor, () => vm.Resample = ResampleMode.AreaAverage));
        rows.Add(await Scenario(group, limit, vm, "поворот 180° / 0°", () => vm.Rotation = Rotation.Rotate180, () => vm.Rotation = Rotation.Rotate0));
        rows.Add(await Scenario(group, limit, vm, "штрих в сетке", () => vm.ApplyStroke(new[] { (3, 3), (4, 3), (5, 3) }, StrokePaint.Dot), () => vm.UndoCommand.Execute(null)));
        await Setup(() =>
        {
            vm.Format = OutputFormat.CKeilC51;
            vm.Binarize = BinarizeMode.Threshold;
        });
    }

    private static async Task<Row> Scenario(string group, int limit, INotifyPropertyChanged vm, string change, Action first, Action second)
    {
        var apply = new List<double>();
        var render = new List<double>();
        for (int i = 0; i < Warmup + Runs; i++)
        {
            (double a, double r) = await Measure(vm, i % 2 == 0 ? first : second);
            if (i >= Warmup)
            {
                apply.Add(a);
                render.Add(r);
            }
        }

        if ((Warmup + Runs) % 2 == 1)
        {
            await Measure(vm, second);
        }

        Console.WriteLine(FormattableString.Invariant($"{group} / {change}: {Median(render):F0} ms (max {render.Max():F0})"));
        return new Row(group, change, Median(apply), Median(render), render.Max(), limit);
    }

    /// <summary>Подготовка без замера: дождаться, пока отработают пауза планировщика, расчёт и отрисовка.</summary>
    private static async Task Setup(Action change)
    {
        change();
        await Task.Delay(1500);
        await Program.Idle(DispatcherPriority.ApplicationIdle);
    }

    private static async Task<(double Apply, double Render)> Measure(INotifyPropertyChanged vm, Action change)
    {
        await Program.Idle(DispatcherPriority.ApplicationIdle);
        var done = new TaskCompletionSource<(double, double)>();
        var watch = new Stopwatch();
        PropertyChangedEventHandler? handler = null;
        handler = (_, args) =>
        {
            if (args.PropertyName != nameof(ImageConverterViewModel.Document))
            {
                return;
            }

            vm.PropertyChanged -= handler;
            double applied = watch.Elapsed.TotalMilliseconds;
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.ContextIdle,
                () => done.TrySetResult((applied, watch.Elapsed.TotalMilliseconds)));
        };
        vm.PropertyChanged += handler;
        watch.Start();
        change();
        Task finished = await Task.WhenAny(done.Task, Task.Delay(TimeSpan.FromSeconds(60)));
        if (finished != done.Task)
        {
            vm.PropertyChanged -= handler;
            throw new TimeoutException("The view model did not publish a new document.");
        }

        return await done.Task;
    }

    /// <summary>Детерминированное «фото»: градиенты и шум с фиксированным зерном.</summary>
    private static RgbaImage Photo(int width, int height)
    {
        var image = new RgbaImage(width, height);
        var random = new Random(20261007);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int noise = random.Next(-24, 25);
                byte r = (byte)Math.Clamp((x * 255 / width) + noise, 0, 255);
                byte g = (byte)Math.Clamp((y * 255 / height) + noise, 0, 255);
                byte b = (byte)Math.Clamp(((x + y) * 127 / (width + height)) + 64 + noise, 0, 255);
                image.SetPixel(x, y, r, g, b, 255);
            }
        }

        return image;
    }

    private static PackDirection Flip(PackDirection direction) =>
        direction == PackDirection.Horizontal ? PackDirection.Vertical : PackDirection.Horizontal;

    private static double Median(List<double> values)
    {
        double[] sorted = values.Order().ToArray();
        return sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[(sorted.Length / 2) - 1] + sorted[sorted.Length / 2]) / 2;
    }

    private static string Build() =>
#if DEBUG
        "Debug";
#else
        "Release";
#endif

    private sealed record Row(string Group, string Change, double ApplyMedian, double RenderMedian, double RenderMax, int Limit);
}
