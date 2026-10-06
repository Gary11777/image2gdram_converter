using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image2Gdram.Core.Imaging;

namespace image2gdram_converter.Controls;

/// <summary>Исходник после наложения на фон для панели рядом с сеткой (решение N-29).</summary>
public sealed class RgbaImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not RgbaImage image)
        {
            return null;
        }

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

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
