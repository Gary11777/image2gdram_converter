namespace Image2Gdram.Core.Imaging;

/// <summary>
/// Сведения о файле без копирования пикселей.
/// Ширина и высота — размер в файле, до поворота по EXIF.
/// </summary>
public sealed class ImageInfo
{
    public ImageInfo(ImageFileFormat format, int width, int height, int frameCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(frameCount, 1);
        Format = format;
        Width = width;
        Height = height;
        FrameCount = frameCount;
    }

    public ImageFileFormat Format { get; }

    public int Width { get; }

    public int Height { get; }

    public int FrameCount { get; }

    public bool IsAnimated => FrameCount > 1;
}
