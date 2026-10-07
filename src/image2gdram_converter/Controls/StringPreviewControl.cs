using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using image2gdram_converter;

namespace image2gdram_converter.Controls;

/// <summary>
/// ?????? ?????????????: ??????? ????????, ? ???????? 1?8.
/// ???? ??? CP1251 ? ?????? ?????? ? ??????? ?????? (??????? N-30).
/// </summary>
public sealed class StringPreviewControl : FrameworkElement
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(StringPreviewControl), new PropertyMetadata(string.Empty, Invalidate));

    public static readonly DependencyProperty TableProperty = DependencyProperty.Register(
        nameof(Table), typeof(FontTable), typeof(StringPreviewControl), new PropertyMetadata(null, Invalidate));

    public static readonly DependencyProperty RevisionProperty = DependencyProperty.Register(
        nameof(Revision), typeof(int), typeof(StringPreviewControl), new PropertyMetadata(0, Invalidate));

    public static readonly DependencyProperty ScaleProperty = DependencyProperty.Register(
        nameof(Scale), typeof(int), typeof(StringPreviewControl), new PropertyMetadata(2, Invalidate));

    public static readonly DependencyProperty InvertProperty = DependencyProperty.Register(
        nameof(Invert), typeof(bool), typeof(StringPreviewControl), new PropertyMetadata(false, Invalidate));

    public static readonly DependencyProperty SchemeProperty = DependencyProperty.Register(
        nameof(Scheme), typeof(ColorScheme), typeof(StringPreviewControl), new PropertyMetadata(ColorScheme.Oled, Invalidate));

    public StringPreviewControl()
    {
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
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

    public int Scale
    {
        get => (int)GetValue(ScaleProperty);
        set => SetValue(ScaleProperty, value);
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
        if (table is null)
        {
            return new Size(0, 0);
        }

        int scale = Math.Clamp(Scale, 1, 8);
        int count = Math.Max(1, StringPreviewMap.Map(Text).Count);
        return new Size(count * table.Cell.Width * scale, table.Cell.Height * scale);
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        FontTable? table = Table;
        if (table is null)
        {
            return;
        }

        IReadOnlyList<PreviewGlyph> cells = StringPreviewMap.Map(Text);
        int scale = Math.Clamp(Scale, 1, 8);
        int cellW = table.Cell.Width;
        int cellH = table.Cell.Height;
        int count = Math.Max(1, cells.Count);
        int width = count * cellW * scale;
        int height = cellH * scale;
        var bitmap = new WriteableBitmap(Math.Max(1, width), Math.Max(1, height), 96, 96, PixelFormats.Bgra32, null);
        var buffer = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bool invert = Invert;
        ColorScheme scheme = Scheme;
        if (cells.Count == 0)
        {
            FillCell(buffer, bitmap.PixelWidth, 0, cellW, cellH, scale, null, false, invert, scheme);
        }

        for (int i = 0; i < cells.Count; i++)
        {
            PreviewGlyph item = cells[i];
            var glyph = item.Code is int code ? table.GetGlyph(code) : null;
            FillCell(buffer, bitmap.PixelWidth, i * cellW * scale, cellW, cellH, scale, glyph, item.OutsideEncoding, invert, scheme);
        }

        bitmap.WritePixels(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight), buffer, bitmap.PixelWidth * 4, 0);
        drawingContext.DrawImage(bitmap, new Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
    }

    private static void FillCell(
        byte[] buffer,
        int stride,
        int originX,
        int cellW,
        int cellH,
        int scale,
        MonoBitmap? glyph,
        bool outside,
        bool invert,
        ColorScheme scheme)
    {
        for (int y = 0; y < cellH; y++)
        {
            for (int x = 0; x < cellW; x++)
            {
                bool on = glyph is not null && PixelPaint.Displayed(glyph[x, y], invert);
                for (int dy = 0; dy < scale; dy++)
                {
                    for (int dx = 0; dx < scale; dx++)
                    {
                        int pixelX = originX + (x * scale) + dx;
                        int pixelY = (y * scale) + dy;
                        SchemeColors.Write(buffer, ((pixelY * stride) + pixelX) * 4, on, scheme);
                    }
                }
            }
        }

        if (!outside)
        {
            return;
        }

        byte r = SchemeColors.OutsideFrame.R;
        byte g = SchemeColors.OutsideFrame.G;
        byte b = SchemeColors.OutsideFrame.B;
        int width = cellW * scale;
        int height = cellH * scale;
        for (int i = 0; i < width; i++)
        {
            Plot(buffer, stride, originX + i, 0, b, g, r);
            Plot(buffer, stride, originX + i, height - 1, b, g, r);
        }

        for (int i = 0; i < height; i++)
        {
            Plot(buffer, stride, originX, i, b, g, r);
            Plot(buffer, stride, originX + width - 1, i, b, g, r);
        }
    }

    private static void Plot(byte[] buffer, int stride, int x, int y, byte b, byte g, byte r)
    {
        int offset = ((y * stride) + x) * 4;
        if ((uint)offset < (uint)buffer.Length)
        {
            SchemeColors.WriteRgb(buffer, offset, b, g, r);
        }
    }

    private static void Invalidate(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        var control = (StringPreviewControl)sender;
        control.InvalidateMeasure();
        control.InvalidateVisual();
    }
}
