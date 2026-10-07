using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Output;

namespace Image2Gdram.UiProbe;

/// <summary>
/// Масштаб Windows 100–200 % (п. 5.1 ТЗ, решение N-57): содержимое окна минимального размера 1024×680
/// размечается и рисуется с DPI 96, 120, 144, 168 и 192. Для каждой вкладки сохраняется PNG и ищутся
/// элементы, которым разметка дала меньше места, чем им нужно (обрезанные подписи, поля, кнопки).
/// </summary>
internal static class DpiProbe
{
    private static readonly double[] Scales = { 1.0, 1.25, 1.5, 1.75, 2.0 };

    public static async Task<bool> RunAsync(StringBuilder report, string root, string output)
    {
        Size client = await MinimumClientSize();
        using var session = new Session();
        string pattern = Path.Combine(root, "testdata", "test_pattern_240x128.png");
        session.Model.Image.LoadDecoded(session.Decoder.Decode(pattern), pattern, resetFrame: true, userAction: false);
        session.Model.Image.Presets.Selected = session.Model.Image.Presets.Items.First(item => item.Preset?.Name == "WG240128A");
        session.Model.Font.SourceKind = FontSourceKind.TrueType;
        session.Model.Font.Family = "Consolas";
        session.Model.Font.Cell = FontCellSize.Cell8x8;
        await Task.Delay(1500);
        await Program.Idle(DispatcherPriority.ApplicationIdle);

        var content = (FrameworkElement)session.Window.Content;
        session.Window.Content = null;
        content.DataContext = session.Model;

        bool ok = true;
        report.AppendLine("# Масштаб Windows 100–200 %");
        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"Клиентская область окна минимального размера 1024x680: {client.Width:F0}x{client.Height:F0} единиц WPF. Для каждого масштаба содержимое размечено и нарисовано с соответствующим DPI; PNG лежат рядом с отчётом.");
        report.AppendLine();
        report.AppendLine("| Вкладка | Масштаб | Пикселей | Обрезанных элементов | Файл |");
        report.AppendLine("|---|---:|---|---:|---|");
        var details = new StringBuilder();
        foreach (int tab in new[] { 0, 1 })
        {
            session.Model.SelectedTab = tab;
            foreach (double scale in Scales)
            {
                VisualTreeHelper.SetRootDpi(content, new DpiScale(scale, scale));
                content.Measure(client);
                content.Arrange(new Rect(client));
                content.UpdateLayout();
                await Program.Idle(DispatcherPriority.ApplicationIdle);
                content.UpdateLayout();

                int width = (int)Math.Ceiling(client.Width * scale);
                int height = (int)Math.Ceiling(client.Height * scale);
                var bitmap = new RenderTargetBitmap(width, height, 96 * scale, 96 * scale, PixelFormats.Pbgra32);
                bitmap.Render(new Background(client));
                bitmap.Render(content);
                string file = FormattableString.Invariant($"tab{tab + 1}_{scale * 100:F0}.png");
                Save(bitmap, Path.Combine(output, file));

                List<string> clipped = Clipped(content);
                ok &= clipped.Count == 0;
                report.AppendLine(CultureInfo.InvariantCulture, $"| {tab + 1} | {scale * 100:F0} % | {width}x{height} | {clipped.Count} | {file} |");
                foreach (string item in clipped)
                {
                    details.AppendLine(CultureInfo.InvariantCulture, $"- вкладка {tab + 1}, {scale * 100:F0} %: {item}");
                }
            }
        }

        content.ClearValue(FrameworkElement.DataContextProperty);
        session.Window.Content = content;
        session.Window.Width = 1024;
        session.Window.Height = 680;
        session.Window.Show();
        foreach (int tab in new[] { 0, 1 })
        {
            session.Model.SelectedTab = tab;
            await Task.Delay(500);
            await Program.Idle(DispatcherPriority.ApplicationIdle);
            double system = VisualTreeHelper.GetDpi(content).DpiScaleX;
            var shot = new RenderTargetBitmap(
                (int)Math.Ceiling(content.ActualWidth * system),
                (int)Math.Ceiling(content.ActualHeight * system),
                96 * system,
                96 * system,
                PixelFormats.Pbgra32);
            shot.Render(new Background(new Size(content.ActualWidth, content.ActualHeight)));
            shot.Render(content);
            Save(shot, Path.Combine(output, FormattableString.Invariant($"window_tab{tab + 1}.png")));
        }

        session.Window.Hide();
        report.AppendLine();
        report.AppendLine(CultureInfo.InvariantCulture, $"Снимки настоящего окна 1024x680 при масштабе системы {VisualTreeHelper.GetDpi(content).DpiScaleX * 100:F0} %: window_tab1.png, window_tab2.png.");
        report.AppendLine();
        report.Append(details.Length == 0 ? "Обрезанных элементов нет.\n" : details.ToString());
        report.AppendLine();
        return ok;
    }

    private static void Save(BitmapSource bitmap, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(path);
        encoder.Save(stream);
    }

    /// <summary>Клиентская область настоящего окна размером 1024×680 при текущем масштабе системы.</summary>
    private static async Task<Size> MinimumClientSize()
    {
        var probe = new Border();
        var window = new Window
        {
            Width = 1024,
            Height = 680,
            Content = probe,
            ShowActivated = false,
            WindowStartupLocation = WindowStartupLocation.CenterScreen,
        };
        window.Show();
        await Program.Idle(DispatcherPriority.ApplicationIdle);
        var size = new Size(probe.ActualWidth, probe.ActualHeight);
        window.Close();
        return size;
    }

    /// <summary>Видимые элементы управления и подписи, которым досталось меньше места, чем требуется.</summary>
    private static List<string> Clipped(DependencyObject root)
    {
        var found = new List<string>();
        Walk(root, found);
        return found;
    }

    private static void Walk(DependencyObject node, List<string> found)
    {
        if (node is UIElement { Visibility: not Visibility.Visible })
        {
            return;
        }

        if (node is FrameworkElement element && element is Control or TextBlock
            && element.TemplatedParent is null
            && LayoutInformation.GetLayoutClip(element) is not null
            && (element.DesiredSize.Width > element.RenderSize.Width + 0.5 || element.DesiredSize.Height > element.RenderSize.Height + 0.5)
            && !InsideScrolledContent(element))
        {
            found.Add(FormattableString.Invariant(
                $"{element.GetType().Name} «{Describe(element)}»: нужно {element.DesiredSize.Width:F1}x{element.DesiredSize.Height:F1}, выделено {element.RenderSize.Width:F1}x{element.RenderSize.Height:F1}"));
        }

        int count = VisualTreeHelper.GetChildrenCount(node);
        for (int i = 0; i < count; i++)
        {
            Walk(VisualTreeHelper.GetChild(node, i), found);
        }
    }

    /// <summary>Содержимое прокручиваемой области может быть больше окна: до него можно докрутить.</summary>
    private static bool InsideScrolledContent(DependencyObject element)
    {
        for (DependencyObject? parent = VisualTreeHelper.GetParent(element); parent is not null; parent = VisualTreeHelper.GetParent(parent))
        {
            if (parent is ScrollContentPresenter presenter && presenter.TemplatedParent is ScrollViewer viewer
                && (viewer.HorizontalScrollBarVisibility != ScrollBarVisibility.Disabled))
            {
                return true;
            }
        }

        return false;
    }

    private static string Describe(FrameworkElement element) => element switch
    {
        TextBlock text => text.Text,
        ContentControl { Content: string text } => text,
        TextBox box => box.Text,
        _ => element.Name,
    };

    /// <summary>Белый фон окна под содержимым, как у настоящего окна.</summary>
    private sealed class Background : DrawingVisual
    {
        public Background(Size size)
        {
            using DrawingContext context = RenderOpen();
            context.DrawRectangle(SystemColors.WindowBrush, null, new Rect(size));
        }
    }
}
