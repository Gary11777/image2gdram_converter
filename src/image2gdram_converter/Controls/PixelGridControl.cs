using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;

namespace image2gdram_converter.Controls;

public sealed class PixelStrokeEventArgs : EventArgs
{
    public PixelStrokeEventArgs(IReadOnlyList<(int X, int Y)> points, StrokePaint paint)
    {
        Points = points;
        Paint = paint;
    }

    public IReadOnlyList<(int X, int Y)> Points { get; }

    public StrokePaint Paint { get; }
}

public sealed class PixelHoverEventArgs : EventArgs
{
    public PixelHoverEventArgs(int? x, int? y)
    {
        X = x;
        Y = y;
    }

    public int? X { get; }

    public int? Y { get; }
}

/// <summary>
///  :  ,    .
///   ,     ViewModel.
/// </summary>
public sealed class PixelGridControl : Grid
{
    public static readonly DependencyProperty PixelsProperty = DependencyProperty.Register(
        nameof(Pixels), typeof(MonoBitmap), typeof(PixelGridControl), new PropertyMetadata(null, Redraw));

    public static readonly DependencyProperty InvertProperty = DependencyProperty.Register(
        nameof(Invert), typeof(bool), typeof(PixelGridControl), new PropertyMetadata(false, Redraw));

    public static readonly DependencyProperty SchemeProperty = DependencyProperty.Register(
        nameof(Scheme), typeof(ColorScheme), typeof(PixelGridControl), new PropertyMetadata(ColorScheme.Oled, Redraw));

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(int), typeof(PixelGridControl), new PropertyMetadata(8, LayoutChanged));

    public static readonly DependencyProperty FitToWindowProperty = DependencyProperty.Register(
        nameof(FitToWindow), typeof(bool), typeof(PixelGridControl), new PropertyMetadata(true, LayoutChanged));

    public static readonly DependencyProperty ShowGridProperty = DependencyProperty.Register(
        nameof(ShowGrid), typeof(bool), typeof(PixelGridControl), new PropertyMetadata(true, LayoutChanged));

    public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(
        nameof(Direction), typeof(PackDirection), typeof(PixelGridControl), new PropertyMetadata(PackDirection.Vertical, LayoutChanged));

    public static readonly DependencyProperty BitsPerByteProperty = DependencyProperty.Register(
        nameof(BitsPerByte), typeof(int), typeof(PixelGridControl), new PropertyMetadata(8, LayoutChanged));

    private readonly ScrollViewer _scroll;
    private readonly Grid _host;
    private readonly Image _image;
    private readonly GridOverlay _overlay;
    private WriteableBitmap? _bitmap;
    private int _zoom = 1;
    private bool _drawing;
    private bool _moved;
    private StrokePaint _paint;
    private int _lastX;
    private int _lastY;
    private readonly List<(int X, int Y)> _points = new();

    public PixelGridControl()
    {
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        Focusable = true;
        _scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Background = Brushes.Black,
        };
        _host = new Grid { Background = Brushes.Transparent };
        _image = new Image { Stretch = Stretch.Fill, SnapsToDevicePixels = true };
        RenderOptions.SetBitmapScalingMode(_image, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(_image, EdgeMode.Aliased);
        _overlay = new GridOverlay(this);
        _host.Children.Add(_image);
        _host.Children.Add(_overlay);
        _scroll.Content = _host;
        Children.Add(_scroll);
        _scroll.SizeChanged += (_, _) => ApplyZoom();
        _host.MouseLeftButtonDown += OnMouseDown;
        _host.MouseRightButtonDown += OnMouseDown;
        _host.MouseMove += OnMouseMove;
        _host.MouseLeftButtonUp += OnMouseUp;
        _host.MouseRightButtonUp += OnMouseUp;
        _host.MouseLeave += (_, _) => HoverChanged?.Invoke(this, new PixelHoverEventArgs(null, null));
    }

    public event EventHandler<PixelStrokeEventArgs>? StrokeCompleted;

    public event EventHandler<PixelHoverEventArgs>? HoverChanged;

    public MonoBitmap? Pixels
    {
        get => (MonoBitmap?)GetValue(PixelsProperty);
        set => SetValue(PixelsProperty, value);
    }

    public bool Invert
    {
        get => (bool)GetValue(InvertProperty);
        set => SetValue(InvertProperty, value);
    }

    public ColorScheme Scheme
    {
        get => (ColorScheme)GetValue(SchemeProperty);
        set => SetValue(SchemeProperty, value);
    }

    public int Scale
    {
        get => (int)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
    }

    public bool FitToWindow
    {
        get => (bool)GetValue(FitToWindowProperty);
        set => SetValue(FitToWindowProperty, value);
    }

    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    public PackDirection Direction
    {
        get => (PackDirection)GetValue(DirectionProperty);
        set => SetValue(DirectionProperty, value);
    }

    public int BitsPerByte
    {
        get => (int)GetValue(BitsPerByteProperty);
        set => SetValue(BitsPerByteProperty, value);
    }

    internal int Zoom => _zoom;

    private static void Redraw(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((PixelGridControl)sender).RebuildBitmap();

    private static void LayoutChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((PixelGridControl)sender).ApplyZoom();

    private void RebuildBitmap()
    {
        MonoBitmap? pixels = Pixels;
        _scroll.Background = Scheme == ColorScheme.Lcd ? _lcdBackBrush : Brushes.Black;
        if (pixels is null)
        {
            _image.Source = null;
            _bitmap = null;
            _host.Width = 0;
            _host.Height = 0;
            return;
        }

        int width = pixels.Width;
        int height = pixels.Height;
        if (_bitmap is null || _bitmap.PixelWidth != width || _bitmap.PixelHeight != height)
        {
            _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        }

        var buffer = new byte[width * height * 4];
        bool invert = Invert;
        ColorScheme scheme = Scheme;
        int offset = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                WriteColor(buffer, offset, PixelPaint.Displayed(pixels[x, y], invert), scheme);
                offset += 4;
            }
        }

        _bitmap.WritePixels(new Int32Rect(0, 0, width, height), buffer, width * 4, 0);
        _image.Source = _bitmap;
        ApplyZoom();
    }

    private void ApplyZoom()
    {
        MonoBitmap? pixels = Pixels;
        if (pixels is null)
        {
            return;
        }

        int zoom = FitToWindow
            ? GridScale.Fit(_scroll.ViewportWidth, _scroll.ViewportHeight, pixels.Width, pixels.Height)
            : Math.Clamp(Scale, GridScale.Min, GridScale.Max);
        _zoom = zoom;
        _image.Width = pixels.Width * zoom;
        _image.Height = pixels.Height * zoom;
        _overlay.InvalidateVisual();
    }

    private bool TryCell(Point point, out int x, out int y)
    {
        x = 0;
        y = 0;
        MonoBitmap? pixels = Pixels;
        if (pixels is null || _zoom < 1)
        {
            return false;
        }

        x = (int)Math.Floor(point.X / _zoom);
        y = (int)Math.Floor(point.Y / _zoom);
        return (uint)x < (uint)pixels.Width && (uint)y < (uint)pixels.Height;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs args)
    {
        if (!TryCell(args.GetPosition(_host), out int x, out int y))
        {
            return;
        }

        Focus();
        _drawing = true;
        _moved = false;
        _paint = args.ChangedButton == MouseButton.Right ? StrokePaint.Background : StrokePaint.Dot;
        _points.Clear();
        _points.Add((x, y));
        _lastX = x;
        _lastY = y;
        _host.CaptureMouse();
        args.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs args)
    {
        if (!TryCell(args.GetPosition(_host), out int x, out int y))
        {
            HoverChanged?.Invoke(this, new PixelHoverEventArgs(null, null));
            return;
        }

        HoverChanged?.Invoke(this, new PixelHoverEventArgs(x, y));
        if (!_drawing || (x == _lastX && y == _lastY))
        {
            return;
        }

        _moved = true;
        AddLine(_lastX, _lastY, x, y);
        _lastX = x;
        _lastY = y;
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs args)
    {
        if (!_drawing)
        {
            return;
        }

        _drawing = false;
        _host.ReleaseMouseCapture();
        StrokePaint paint = _paint == StrokePaint.Background
            ? StrokePaint.Background
            : _moved ? StrokePaint.Dot : StrokePaint.Toggle;
        StrokeCompleted?.Invoke(this, new PixelStrokeEventArgs(_points.ToArray(), paint));
        _points.Clear();
        args.Handled = true;
    }

    private void AddLine(int x0, int y0, int x1, int y1)
    {
        int dx = Math.Abs(x1 - x0);
        int sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0);
        int sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;
        bool skip = true;
        while (true)
        {
            if (!skip)
            {
                _points.Add((x0, y0));
            }

            skip = false;
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int doubled = 2 * err;
            if (doubled >= dy)
            {
                err += dy;
                x0 += sx;
            }

            if (doubled <= dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static void WriteColor(byte[] buffer, int offset, bool on, ColorScheme scheme)
    {
        if (scheme == ColorScheme.Lcd)
        {
            buffer[offset] = on ? (byte)0x18 : (byte)0xD0;
            buffer[offset + 1] = on ? (byte)0x24 : (byte)0xE4;
            buffer[offset + 2] = on ? (byte)0x1A : (byte)0xD8;
        }
        else
        {
            buffer[offset] = on ? (byte)0xFF : (byte)0;
            buffer[offset + 1] = on ? (byte)0xF2 : (byte)0;
            buffer[offset + 2] = on ? (byte)0xE6 : (byte)0;
        }

        buffer[offset + 3] = 0xFF;
    }

    private static readonly SolidColorBrush _lcdBackBrush = Create(Color.FromRgb(0xD8, 0xE4, 0xD0));

    private static SolidColorBrush Create(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private sealed class GridOverlay : FrameworkElement
    {
        private readonly PixelGridControl _owner;

        public GridOverlay(PixelGridControl owner)
        {
            _owner = owner;
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext context)
        {
            MonoBitmap? pixels = _owner.Pixels;
            if (pixels is null || !_owner.ShowGrid)
            {
                return;
            }

            int zoom = _owner.Zoom;
            int step = GridScale.ThickStep(_owner.Direction, _owner.BitsPerByte);
            bool thin = zoom >= GridScale.ThinFrom;
            bool thick = zoom >= GridScale.ThickFrom;
            if (!thin && !thick)
            {
                return;
            }

            Pen thinPen = _owner.Scheme == ColorScheme.Lcd ? _lcdThin : _oledThin;
            Pen thickPen = _owner.Scheme == ColorScheme.Lcd ? _lcdThick : _oledThick;
            bool horizontalPack = _owner.Direction == PackDirection.Horizontal;
            for (int x = 1; x < pixels.Width; x++)
            {
                bool boundary = horizontalPack && step > 0 && x % step == 0;
                Pen? pen = boundary ? (thick ? thickPen : null) : (thin ? thinPen : null);
                if (pen is null)
                {
                    continue;
                }

                double position = Snap(x * zoom);
                context.DrawLine(pen, new Point(position, 0), new Point(position, pixels.Height * zoom));
            }

            for (int y = 1; y < pixels.Height; y++)
            {
                bool boundary = !horizontalPack && step > 0 && y % step == 0;
                Pen? pen = boundary ? (thick ? thickPen : null) : (thin ? thinPen : null);
                if (pen is null)
                {
                    continue;
                }

                double position = Snap(y * zoom);
                context.DrawLine(pen, new Point(0, position), new Point(pixels.Width * zoom, position));
            }
        }

        private static double Snap(double value) => Math.Round(value) + 0.5;

        private static readonly Pen _lcdThin = Pen(Color.FromRgb(0x90, 0xA0, 0x90), 1);
        private static readonly Pen _lcdThick = Pen(Color.FromRgb(0x20, 0x30, 0x20), 2);
        private static readonly Pen _oledThin = Pen(Color.FromRgb(0x40, 0x40, 0x40), 1);
        private static readonly Pen _oledThick = Pen(Color.FromRgb(0xA0, 0xA0, 0xA0), 2);

        private static Pen Pen(Color color, double thickness)
        {
            var pen = new Pen(new SolidColorBrush(color), thickness);
            pen.Freeze();
            if (pen.Brush.CanFreeze)
            {
                pen.Brush.Freeze();
            }

            return pen;
        }
    }
}
