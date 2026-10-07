using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Presets;
using image2gdram_converter;

namespace image2gdram_converter.Controls;

/// <summary>
/// ???????? ??????? 16?16: ?????? ? ??????? ????????, ??????? ? ???????.
/// ?????? ?????? ????????, ?????? ?????? ???????? ???????. ???? ????? ?? ??? ???????.
/// </summary>
public sealed class GlyphTableControl : FrameworkElement
{
    public static readonly DependencyProperty TableProperty = DependencyProperty.Register(
        nameof(Table), typeof(FontTable), typeof(GlyphTableControl), new PropertyMetadata(null, MarkDirty));

    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(
        nameof(Revision), typeof(int), typeof(GlyphTableControl), new PropertyMetadata(0, MarkDirty));

    public static readonly DependencyProperty SelectedCodeProperty = DependencyProperty.Register(
        nameof(SelectedCode),
        typeof(int),
        typeof(GlyphTableControl),
        new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelection));

    public static readonly DependencyProperty InvertProperty = DependencyProperty.Register(
        nameof(Invert), typeof(bool), typeof(GlyphTableControl), new PropertyMetadata(false, MarkDirty));

    public static readonly DependencyProperty SchemeProperty = DependencyProperty.Register(
        nameof(Scheme), typeof(ColorScheme), typeof(GlyphTableControl), new PropertyMetadata(ColorScheme.Oled, MarkDirty));

    private const int Scale = 2;
    private const double Label = 22;

    private WriteableBitmap? _bitmap;
    private bool _dirty = true;
    private int _cellWidth = 6;
    private int _cellHeight = 8;

    public GlyphTableControl()
    {
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        Focusable = true;
    }

    public FontTable? Table
    {
        get => (FontTable?)GetValue(TableProperty);
        set => SetValue(TableProperty, value);
    }

    public int Revision
    {
        get => (int)GetValue(RevisionProperty);
        set => SetValue(RevisionProperty, value);
    }

    public int SelectedCode
    {
        get => (int)GetValue(SelectedCodeProperty);
        set => SetValue(SelectedCodeProperty, value);
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

    protected override Size MeasureOverride(Size available)
    {
        FontTable? table = Table;
        int width = table?.Cell.Width ?? 6;
        int height = table?.Cell.Height ?? 8;
        return new Size(Label + (16 * width * Scale), Label + (16 * height * Scale));
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        FontTable? table = Table;
        if (table is null)
        {
            return;
        }

        Point point = e.GetPosition(this);
        double cellW = table.Cell.Width * Scale;
        double cellH = table.Cell.Height * Scale;
        int column = (int)((point.X - Label) / cellW);
        int row = (int)((point.Y - Label) / cellH);
        if ((uint)column < 16 && (uint)row < 16)
        {
            SelectedCode = (row << 4) | column;
            e.Handled = true;
        }
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters) =>
        new PointHitTestResult(this, hitTestParameters.HitPoint);

    protected override void OnRender(DrawingContext drawingContext)
    {
        FontTable? table = Table;
        if (table is null)
        {
            return;
        }

        if (_dirty || _bitmap is null || _cellWidth != table.Cell.Width || _cellHeight != table.Cell.Height)
        {
            Rebuild(table);
        }

        DrawHeaders(drawingContext, table);
        if (_bitmap is not null)
        {
            drawingContext.DrawImage(_bitmap, new Rect(Label, Label, _bitmap.PixelWidth, _bitmap.PixelHeight));
        }

        int code = Math.Clamp(SelectedCode, 0, 255);
        double left = Label + ((code & 0x0F) * table.Cell.Width * Scale);
        double top = Label + ((code >> 4) * table.Cell.Height * Scale);
        var pen = new Pen(new SolidColorBrush(SchemeColors.Selection), 2);
        pen.Freeze();
        drawingContext.DrawRectangle(null, pen, new Rect(left, top, table.Cell.Width * Scale, table.Cell.Height * Scale));
    }

    private void Rebuild(FontTable table)
    {
        _cellWidth = table.Cell.Width;
        _cellHeight = table.Cell.Height;
        int width = 16 * _cellWidth * Scale;
        int height = 16 * _cellHeight * Scale;
        _bitmap = new WriteableBitmap(width, height, 96, 96, PixelFormats.Bgra32, null);
        var buffer = new byte[width * height * 4];
        bool invert = Invert;
        ColorScheme scheme = Scheme;
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            int column = code & 0x0F;
            int row = code >> 4;
            int originX = column * _cellWidth * Scale;
            int originY = row * _cellHeight * Scale;
            var glyph = table.GetGlyph(code);
            bool empty = table.IsEmpty(code);
            for (int y = 0; y < _cellHeight; y++)
            {
                for (int x = 0; x < _cellWidth; x++)
                {
                    bool on = PixelPaint.Displayed(glyph[x, y], invert);
                    for (int dy = 0; dy < Scale; dy++)
                    {
                        int pixelY = originY + (y * Scale) + dy;
                        for (int dx = 0; dx < Scale; dx++)
                        {
                            int pixelX = originX + (x * Scale) + dx;
                            SchemeColors.Write(buffer, ((pixelY * width) + pixelX) * 4, on, scheme);
                        }
                    }
                }
            }

            if (empty)
            {
                Frame(buffer, width, originX, originY, _cellWidth * Scale, _cellHeight * Scale, scheme);
            }

            if (table.IsManual(code))
            {
                Mark(buffer, width, originX, originY, _cellWidth * Scale);
            }
        }

        _bitmap.WritePixels(new Int32Rect(0, 0, width, height), buffer, width * 4, 0);
        _dirty = false;
    }

    private static void Frame(byte[] buffer, int stride, int x, int y, int width, int height, ColorScheme scheme)
    {
        byte b = scheme == ColorScheme.Lcd ? (byte)0x70 : (byte)0x66;
        byte g = scheme == ColorScheme.Lcd ? (byte)0x90 : (byte)0x66;
        byte r = scheme == ColorScheme.Lcd ? (byte)0x80 : (byte)0x66;
        for (int i = 0; i < width; i++)
        {
            Plot(buffer, stride, x + i, y, b, g, r);
            Plot(buffer, stride, x + i, y + height - 1, b, g, r);
        }

        for (int i = 0; i < height; i++)
        {
            Plot(buffer, stride, x, y + i, b, g, r);
            Plot(buffer, stride, x + width - 1, y + i, b, g, r);
        }
    }

    private static void Mark(byte[] buffer, int stride, int x, int y, int cellWidth)
    {
        int size = Math.Min(6, cellWidth);
        for (int dy = 0; dy < size; dy++)
        {
            for (int dx = 0; dx < size - dy; dx++)
            {
                Plot(buffer, stride, x + cellWidth - 1 - dx, y + dy, 0x00, 0x80, 0xF0);
            }
        }
    }

    private static void Plot(byte[] buffer, int stride, int x, int y, byte b, byte g, byte r)
    {
        int offset = ((y * stride) + x) * 4;
        if ((uint)offset >= (uint)buffer.Length)
        {
            return;
        }

        SchemeColors.WriteRgb(buffer, offset, b, g, r);
    }

    private void DrawHeaders(DrawingContext drawingContext, FontTable table)
    {
        double dip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var brush = Scheme == ColorScheme.Lcd ? Brushes.Black : Brushes.White;
        double cellW = table.Cell.Width * Scale;
        double cellH = table.Cell.Height * Scale;
        for (int i = 0; i < 16; i++)
        {
            string label = i.ToString("X", CultureInfo.InvariantCulture);
            var text = new FormattedText(
                label,
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                new Typeface("Consolas"),
                12,
                brush,
                dip);
            drawingContext.DrawText(text, new Point(Label + (i * cellW) + 2, 2));
            drawingContext.DrawText(text, new Point(4, Label + (i * cellH) + 2));
        }
    }

    private static void MarkDirty(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (GlyphTableControl)sender;
        control._dirty = true;
        control.InvalidateMeasure();
        control.InvalidateVisual();
    }

    private static void OnSelection(DependencyObject sender, DependencyPropertyChangedEventArgs args) =>
        ((GlyphTableControl)sender).InvalidateVisual();
}
