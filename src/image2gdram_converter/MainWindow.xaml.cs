using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using image2gdram_converter.ViewModels;

namespace image2gdram_converter;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
        SourceInitialized += (_, _) => FitToMonitor();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        FitToMonitor();
    }

    private void FitToMonitor()
    {
        if (TryGetWorkArea(out Rect area))
        {
            (Size minimum, Size size) = WindowFit.Fit(new Size(Width, Height), area);
            MinWidth = minimum.Width;
            MinHeight = minimum.Height;
            Width = size.Width;
            Height = size.Height;
            if (double.IsFinite(Left) && double.IsFinite(Top))
            {
                Left = Math.Clamp(Left, area.Left, Math.Max(area.Left, area.Right - size.Width));
                Top = Math.Clamp(Top, area.Top, Math.Max(area.Top, area.Bottom - size.Height));
            }
        }
    }

    /// <summary>Рабочая область монитора окна в единицах WPF.</summary>
    private bool TryGetWorkArea(out Rect area)
    {
        area = Rect.Empty;
        IntPtr handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return false;
        }

        IntPtr monitor = NativeMethods.MonitorFromWindow(handle, NativeMethods.MonitorDefaultToNearest);
        var info = new NativeMethods.MonitorInfo { Size = Marshal.SizeOf<NativeMethods.MonitorInfo>() };
        if (monitor == IntPtr.Zero || !NativeMethods.GetMonitorInfo(monitor, ref info))
        {
            return false;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(this);
        NativeMethods.RectInt work = info.Work;
        area = new Rect(
            work.Left / dpi.DpiScaleX,
            work.Top / dpi.DpiScaleY,
            (work.Right - work.Left) / dpi.DpiScaleX,
            (work.Bottom - work.Top) / dpi.DpiScaleY);
        return true;
    }

    private void OnClosing(object? sender, CancelEventArgs args)
    {
        if (DataContext is MainViewModel model && !model.TryClose())
        {
            args.Cancel = true;
        }
    }

    private static class NativeMethods
    {
        public const int MonitorDefaultToNearest = 2;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [StructLayout(LayoutKind.Sequential)]
        public struct RectInt
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MonitorInfo
        {
            public int Size;
            public RectInt Monitor;
            public RectInt Work;
            public int Flags;
        }
    }
}
