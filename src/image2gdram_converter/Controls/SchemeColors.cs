using System.Windows.Media;
using Image2Gdram.Core.Presets;

namespace image2gdram_converter.Controls;

/// <summary>Цвета точки и фона схем «ЖКИ» и «OLED». Совпадают с сеткой предпросмотра.</summary>
public static class SchemeColors
{
    public static void Write(byte[] buffer, int offset, bool on, ColorScheme scheme)
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

    public static void WriteRgb(byte[] buffer, int offset, byte b, byte g, byte r)
    {
        buffer[offset] = b;
        buffer[offset + 1] = g;
        buffer[offset + 2] = r;
        buffer[offset + 3] = 0xFF;
    }

    public static Color Selection => Color.FromRgb(0x1E, 0x6F, 0xD9);

    public static Color OutsideFrame => Color.FromRgb(0xE0, 0x6A, 0x00);
}
