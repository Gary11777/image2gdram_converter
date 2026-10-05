namespace Image2Gdram.Core.Imaging;

/// <summary>
/// Результат декодирования. Кадры GIF уже собраны в полный холст (решение N-20).
/// Размер кадра — после ориентации EXIF (решение N-19).
/// </summary>
public sealed class DecodedImage
{
    public DecodedImage(ImageFileFormat format, IReadOnlyList<RgbaImage> frames)
    {
        ArgumentNullException.ThrowIfNull(frames);
        if (frames.Count == 0)
        {
            throw new ArgumentException("At least one frame is required.", nameof(frames));
        }

        int width = frames[0].Width;
        int height = frames[0].Height;
        for (int i = 1; i < frames.Count; i++)
        {
            if (frames[i].Width != width || frames[i].Height != height)
            {
                throw new ArgumentException("All frames must have the same size.", nameof(frames));
            }
        }

        Format = format;
        Frames = frames;
    }

    public ImageFileFormat Format { get; }

    public IReadOnlyList<RgbaImage> Frames { get; }

    public bool IsAnimated => Frames.Count > 1;

    public int Width => Frames[0].Width;

    public int Height => Frames[0].Height;
}
