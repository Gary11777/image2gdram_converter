using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Processing;
using Rotation = Image2Gdram.Core.Processing.Rotation;

namespace image2gdram_converter.Controls;

/// <summary>
/// Исходник и прямоугольник фрагмента (N-80). Рисует в <see cref="OnRender"/>, без элемента на каждый пиксель.
/// Геометрия — <see cref="CropGeometry"/>; здесь только мышь, клавиатура и картинка.
/// </summary>
public sealed class SourceCropControl : FrameworkElement
{
    private const double MarginDip = 8;
    private const double HandleDip = 6;

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(RgbaImage),
        typeof(SourceCropControl),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnSourceChanged));

    public static readonly DependencyProperty CropProperty = DependencyProperty.Register(
        nameof(Crop),
        typeof(CropViewport),
        typeof(SourceCropControl),
        new FrameworkPropertyMetadata(default(CropViewport), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault | FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TargetWidthProperty = DependencyProperty.Register(
        nameof(TargetWidth), typeof(int), typeof(SourceCropControl), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TargetHeightProperty = DependencyProperty.Register(
        nameof(TargetHeight), typeof(int), typeof(SourceCropControl), new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty RotationProperty = DependencyProperty.Register(
        nameof(Rotation), typeof(Rotation), typeof(SourceCropControl), new FrameworkPropertyMetadata(Rotation.Rotate0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ScaleMinProperty = DependencyProperty.Register(
        nameof(ScaleMin), typeof(double), typeof(SourceCropControl), new FrameworkPropertyMetadata(0.0));

    public static readonly DependencyProperty ScaleMaxProperty = DependencyProperty.Register(
        nameof(ScaleMax), typeof(double), typeof(SourceCropControl), new FrameworkPropertyMetadata(1.0));

    public static readonly DependencyProperty BackgroundProperty = DependencyProperty.Register(
        nameof(Background), typeof(BackgroundColor), typeof(SourceCropControl), new FrameworkPropertyMetadata(BackgroundColor.White, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ConfirmGestureProperty = DependencyProperty.Register(
        nameof(ConfirmGesture), typeof(Func<bool>), typeof(SourceCropControl));

    private BitmapSource? _bitmap;
    private ViewMap? _frozen;
    private bool _gesture;
    private bool _ending;
    private GestureKind _kind;
    private int _handle;
    private CropViewport _gestureCrop;
    private Point _gestureOrigin;
    private double _aspect = 1;
    private Point _anchor;

    public SourceCropControl()
    {
        Focusable = true;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    public RgbaImage? Source
    {
        get => (RgbaImage?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public CropViewport Crop
    {
        get => (CropViewport)GetValue(CropProperty);
        set => SetValue(CropProperty, value);
    }

    public int TargetWidth
    {
        get => (int)GetValue(TargetWidthProperty);
        set => SetValue(TargetWidthProperty, value);
    }

    public int TargetHeight
    {
        get => (int)GetValue(TargetHeightProperty);
        set => SetValue(TargetHeightProperty, value);
    }

    public Rotation Rotation
    {
        get => (Rotation)GetValue(RotationProperty);
        set => SetValue(RotationProperty, value);
    }

    public double ScaleMin
    {
        get => (double)GetValue(ScaleMinProperty);
        set => SetValue(ScaleMinProperty, value);
    }

    public double ScaleMax
    {
        get => (double)GetValue(ScaleMaxProperty);
        set => SetValue(ScaleMaxProperty, value);
    }

    public BackgroundColor Background
    {
        get => (BackgroundColor)GetValue(BackgroundProperty);
        set => SetValue(BackgroundProperty, value);
    }

    public Func<bool>? ConfirmGesture
    {
        get => (Func<bool>?)GetValue(ConfirmGestureProperty);
        set => SetValue(ConfirmGestureProperty, value);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        RgbaImage? source = Source;
        if (source is null || ActualWidth <= 1 || ActualHeight <= 1 || !TryMap(source, out ViewMap map, out SourceRect rect))
        {
            return;
        }

        Rect image = ScreenRect(map, 0, 0, source.Width, source.Height);
        Rect crop = ScreenRect(map, rect.X, rect.Y, rect.Width, rect.Height);
        if (_bitmap is not null)
        {
            drawingContext.DrawImage(_bitmap, image);
        }

        var cropGeometry = new RectangleGeometry(crop);
        var outsideImage = new CombinedGeometry(GeometryCombineMode.Exclude, cropGeometry, new RectangleGeometry(image));
        drawingContext.DrawGeometry(PaperBrush(), null, outsideImage);

        var full = new RectangleGeometry(new Rect(0, 0, ActualWidth, ActualHeight));
        var dim = new CombinedGeometry(GeometryCombineMode.Exclude, full, cropGeometry);
        drawingContext.DrawGeometry(new SolidColorBrush(Color.FromArgb(115, 0, 0, 0)), null, dim);

        double dpi = VisualTreeHelper.GetDpi(this).DpiScaleX;
        if (dpi <= 0)
        {
            dpi = 1;
        }

        double thickness = 1 / dpi;
        var guidelines = new GuidelineSet();
        guidelines.GuidelinesX.Add(crop.Left);
        guidelines.GuidelinesX.Add(crop.Right);
        guidelines.GuidelinesY.Add(crop.Top);
        guidelines.GuidelinesY.Add(crop.Bottom);
        drawingContext.PushGuidelineSet(guidelines);
        drawingContext.DrawRectangle(null, DashPen(Brushes.Black, thickness, 0), crop);
        drawingContext.DrawRectangle(null, DashPen(Brushes.White, thickness, 4), crop);
        drawingContext.Pop();

        double half = HandleDip / 2;
        foreach (Point corner in Corners(crop))
        {
            var handle = new Rect(corner.X - half, corner.Y - half, HandleDip, HandleDip);
            drawingContext.DrawRectangle(Brushes.White, new Pen(Brushes.Black, thickness), handle);
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        Focus();
        RgbaImage? source = Source;
        if (source is null || !TryMap(source, out ViewMap map, out SourceRect rect))
        {
            return;
        }

        if (e.ClickCount >= 2)
        {
            EndGesture(restore: false);
            if (Confirm())
            {
                WriteCrop(CropGeometry.Fit(source.Width, source.Height, TargetWidth, TargetHeight, Rotation));
            }

            e.Handled = true;
            return;
        }

        Point at = e.GetPosition(this);
        int handle = HitHandle(map, rect, at);
        bool inside = HitInside(map, rect, at);
        if (handle < 0 && !inside)
        {
            return;
        }

        if (!Confirm())
        {
            e.Handled = true;
            return;
        }

        if (e.LeftButton != MouseButtonState.Pressed)
        {
            e.Handled = true;
            return;
        }

        _gesture = true;
        _kind = handle >= 0 ? GestureKind.Resize : GestureKind.Move;
        _handle = handle;
        _gestureCrop = Crop;
        _gestureOrigin = at;
        _aspect = rect.Height <= 1e-9 ? 1 : rect.Width / rect.Height;
        _anchor = Opposite(rect, handle);
        _frozen = map;
        CaptureMouse();
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        Point at = e.GetPosition(this);
        RgbaImage? source = Source;
        if (source is not null && TryMap(source, out ViewMap map, out SourceRect rect))
        {
            int handle = _gesture && _kind == GestureKind.Resize ? _handle : HitHandle(map, rect, at);
            bool inside = HitInside(map, rect, at);
            Cursor = handle is 0 or 2 ? Cursors.SizeNWSE
                : handle is 1 or 3 ? Cursors.SizeNESW
                : inside || (_gesture && _kind == GestureKind.Move) ? Cursors.SizeAll
                : Cursors.Arrow;
            ToolTip = FindTip(handle >= 0 ? "Tip.CropResize" : "Tip.CropMove");
            if (_gesture)
            {
                if (_kind == GestureKind.Move)
                {
                    Point from = map.ToSource(_gestureOrigin);
                    Point to = map.ToSource(at);
                    WriteCrop(_gestureCrop with
                    {
                        CenterX = _gestureCrop.CenterX + (to.X - from.X),
                        CenterY = _gestureCrop.CenterY + (to.Y - from.Y),
                    });
                }
                else
                {
                    WriteCrop(ResizeTo(map.ToSource(at)));
                }
            }
        }

        e.Handled = _gesture;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_gesture)
        {
            EndGesture(restore: false);
            e.Handled = true;
        }
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        if (_gesture && !_ending)
        {
            EndGesture(restore: true);
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        RgbaImage? source = Source;
        if (source is null || !TryMap(source, out ViewMap map, out _))
        {
            return;
        }

        if (!Confirm())
        {
            e.Handled = true;
            return;
        }

        Point under = map.ToSource(e.GetPosition(this));
        double factor = e.Delta > 0 ? ScaleFormat.StepFactor : 1 / ScaleFormat.StepFactor;
        double next = Crop.Scale * factor;
        if (next <= 0)
        {
            return;
        }

        double ratio = Crop.Scale / next;
        WriteCrop(new CropViewport(
            under.X - (under.X - Crop.CenterX) * ratio,
            under.Y - (under.Y - Crop.CenterY) * ratio,
            next));
        e.Handled = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        RgbaImage? source = Source;
        if (source is null)
        {
            return;
        }

        if (e.Key == Key.Escape && _gesture)
        {
            EndGesture(restore: true);
            e.Handled = true;
            return;
        }

        int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        double dx = 0;
        double dy = 0;
        bool scale = false;
        double factor = 1;
        bool fit = false;
        switch (e.Key)
        {
            case Key.Left:
                dx = -step;
                break;
            case Key.Right:
                dx = step;
                break;
            case Key.Up:
                dy = -step;
                break;
            case Key.Down:
                dy = step;
                break;
            case Key.OemPlus:
            case Key.Add:
                scale = true;
                factor = ScaleFormat.StepFactor;
                break;
            case Key.OemMinus:
            case Key.Subtract:
                scale = true;
                factor = 1 / ScaleFormat.StepFactor;
                break;
            case Key.Home:
                fit = true;
                break;
            default:
                return;
        }

        if (!Confirm())
        {
            e.Handled = true;
            return;
        }

        if (fit)
        {
            WriteCrop(CropGeometry.Fit(source.Width, source.Height, TargetWidth, TargetHeight, Rotation));
        }
        else if (scale)
        {
            WriteCrop(Crop with { Scale = Crop.Scale * factor });
        }
        else
        {
            WriteCrop(Crop with { CenterX = Crop.CenterX + dx, CenterY = Crop.CenterY + dy });
        }

        e.Handled = true;
    }

    private bool Confirm() => ConfirmGesture?.Invoke() ?? true;

    private void EndGesture(bool restore)
    {
        if (!_gesture)
        {
            _frozen = null;
            InvalidateVisual();
            return;
        }

        _ending = true;
        _gesture = false;
        _frozen = null;
        if (restore)
        {
            WriteCrop(_gestureCrop);
        }

        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        _ending = false;
        InvalidateVisual();
    }

    private void WriteCrop(CropViewport next)
    {
        RgbaImage? source = Source;
        if (source is null || TargetWidth < 1 || TargetHeight < 1)
        {
            return;
        }

        CropViewport clamped = CropGeometry.Clamp(next, source.Width, source.Height, TargetWidth, TargetHeight, Rotation);
        if (ScaleMax > ScaleMin && ScaleMin > 0)
        {
            double scale = Math.Min(Math.Max(clamped.Scale, ScaleMin), ScaleMax);
            if (scale != clamped.Scale)
            {
                clamped = CropGeometry.Clamp(clamped with { Scale = scale }, source.Width, source.Height, TargetWidth, TargetHeight, Rotation);
            }
        }

        SetCurrentValue(CropProperty, clamped);
    }

    private CropViewport ResizeTo(Point mouse)
    {
        double alongX = mouse.X - _anchor.X;
        double alongY = mouse.Y - _anchor.Y;
        double dirX = _handle is 1 or 2 ? 1 : -1;
        double dirY = _handle is 2 or 3 ? 1 : -1;
        double reachX = Math.Max(0, alongX * dirX);
        double reachY = Math.Max(0, alongY * dirY);
        double width = reachX;
        double height = _aspect <= 0 ? reachY : width / _aspect;
        if (height < reachY)
        {
            height = reachY;
            width = height * _aspect;
        }

        if (width < 1e-6)
        {
            width = 1e-6;
        }

        double left = dirX > 0 ? _anchor.X : _anchor.X - width;
        double top = dirY > 0 ? _anchor.Y : _anchor.Y - height;
        bool swapped = Rotation is Rotation.Rotate90 or Rotation.Rotate270;
        double scale = (swapped ? TargetHeight : TargetWidth) / width;
        return new CropViewport(left + width / 2, top + height / 2, scale);
    }

    private bool TryMap(RgbaImage source, out ViewMap map, out SourceRect rect)
    {
        map = default;
        rect = default;
        if (TargetWidth < 1 || TargetHeight < 1 || Crop.Scale <= 0 || !double.IsFinite(Crop.Scale))
        {
            return false;
        }

        rect = CropGeometry.SourceRect(Crop, TargetWidth, TargetHeight, Rotation);
        if (rect.Width <= 0 || rect.Height <= 0 || !double.IsFinite(rect.Width) || !double.IsFinite(rect.Height))
        {
            return false;
        }

        if (_frozen is ViewMap frozen)
        {
            map = frozen;
            return true;
        }

        var union = Rect.Union(new Rect(0, 0, source.Width, source.Height), new Rect(rect.X, rect.Y, rect.Width, rect.Height));
        double availW = Math.Max(1, ActualWidth - 2 * MarginDip);
        double availH = Math.Max(1, ActualHeight - 2 * MarginDip);
        double scale = Math.Min(availW / union.Width, availH / union.Height);
        if (!double.IsFinite(scale) || scale <= 0)
        {
            return false;
        }

        map = new ViewMap(
            scale,
            MarginDip + (availW - union.Width * scale) / 2 - union.X * scale,
            MarginDip + (availH - union.Height * scale) / 2 - union.Y * scale);
        return true;
    }

    private int HitHandle(ViewMap map, SourceRect rect, Point at)
    {
        Rect crop = ScreenRect(map, rect.X, rect.Y, rect.Width, rect.Height);
        Point[] corners = Corners(crop);
        double half = HandleDip / 2;
        for (int i = 0; i < corners.Length; i++)
        {
            var box = new Rect(corners[i].X - half, corners[i].Y - half, HandleDip, HandleDip);
            if (box.Contains(at))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool HitInside(ViewMap map, SourceRect rect, Point at)
    {
        Rect crop = ScreenRect(map, rect.X, rect.Y, rect.Width, rect.Height);
        return crop.Contains(at);
    }

    private static Point Opposite(SourceRect rect, int handle) => handle switch
    {
        0 => new Point(rect.Right, rect.Bottom),
        1 => new Point(rect.X, rect.Bottom),
        2 => new Point(rect.X, rect.Y),
        _ => new Point(rect.Right, rect.Y),
    };

    private static Point[] Corners(Rect crop) =>
    [
        crop.TopLeft,
        crop.TopRight,
        crop.BottomRight,
        crop.BottomLeft,
    ];

    private static Rect ScreenRect(ViewMap map, double x, double y, double width, double height) =>
        new(map.OriginX + x * map.Scale, map.OriginY + y * map.Scale, width * map.Scale, height * map.Scale);

    private Brush PaperBrush() => Background == BackgroundColor.Black ? Brushes.Black : Brushes.White;

    private static Pen DashPen(Brush brush, double thickness, double offset)
    {
        var pen = new Pen(brush, thickness)
        {
            DashStyle = new DashStyle([4, 4], offset),
            DashCap = PenLineCap.Flat,
            StartLineCap = PenLineCap.Flat,
            EndLineCap = PenLineCap.Flat,
        };
        pen.Freeze();
        return pen;
    }

    private static string FindTip(string key) =>
        Application.Current?.TryFindResource(key) as string ?? string.Empty;

    private static void OnSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (SourceCropControl)sender;
        control._bitmap = args.NewValue is RgbaImage image ? CreateBitmap(image) : null;
        control.InvalidateVisual();
    }

    private static BitmapSource CreateBitmap(RgbaImage image)
    {
        int width = image.Width;
        int height = image.Height;
        var buffer = new byte[width * height * 4];
        ReadOnlySpan<byte> source = image.Pixels;
        for (int i = 0, o = 0; i < source.Length; i += RgbaImage.BytesPerPixel, o += 4)
        {
            buffer[o] = source[i + 2];
            buffer[o + 1] = source[i + 1];
            buffer[o + 2] = source[i];
            buffer[o + 3] = 255;
        }

        BitmapSource bitmap = BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, buffer, width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    private readonly record struct ViewMap(double Scale, double OriginX, double OriginY)
    {
        public Point ToSource(Point screen) => new((screen.X - OriginX) / Scale, (screen.Y - OriginY) / Scale);
    }

    private enum GestureKind
    {
        Move,
        Resize,
    }
}
