using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Processing;
using Rotation = Image2Gdram.Core.Processing.Rotation;

namespace Image2Gdram.Imaging.Wic;

/// <summary>
/// Декодер BMP, PNG, JPEG и GIF через WIC (решение N-39).
/// Цветовой профиль не применяется. Ориентация EXIF поворачивается своим кодом (N-19).
/// Кадры GIF собираются в полный холст с учётом disposal (N-20). Файл открывается только для чтения.
/// </summary>
public sealed class WicImageDecoder : IImageDecoder
{
    private const BitmapCreateOptions CreateOptions =
        BitmapCreateOptions.PreservePixelFormat
        | BitmapCreateOptions.IgnoreColorProfile
        | BitmapCreateOptions.IgnoreImageCache;

    public ImageInfo Identify(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ImageHeaderReader.Header header = ReadHeader(path);
        EnsureLimits(path, header);
        return new ImageInfo(header.Format, header.Width, header.Height, header.FrameCount);
    }

    public DecodedImage Decode(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        ImageHeaderReader.Header header = ReadHeader(path);
        EnsureLimits(path, header);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            using FileStream stream = Open(path);
            BitmapDecoder decoder = BitmapDecoder.Create(stream, CreateOptions, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count < 1)
            {
                throw Corrupted(path);
            }

            if (header.Format == ImageFileFormat.Gif)
            {
                if (ImageLimits.MemoryExceedsLimit(header.Width, header.Height, decoder.Frames.Count))
                {
                    throw Memory(path, header.Width, header.Height, decoder.Frames.Count);
                }

                IReadOnlyList<RgbaImage> frames = ComposeGif(decoder, header, path, cancellationToken);
                return new DecodedImage(ImageFileFormat.Gif, frames);
            }

            BitmapFrame frame = decoder.Frames[0];
            RgbaImage image = CopyFrame(frame, path);
            image = ApplyOrientation(image, ReadOrientation(frame.Metadata as BitmapMetadata));
            return new DecodedImage(header.Format, new[] { image });
        }
        catch (ImageLoadException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (FileFormatException ex)
        {
            throw Corrupted(path, ex);
        }
        catch (NotSupportedException ex)
        {
            throw Unsupported(path, ex);
        }
        catch (IOException ex)
        {
            throw Io(path, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Io(path, ex);
        }
        catch (ArgumentException ex)
        {
            throw Corrupted(path, ex);
        }
    }

    private static IReadOnlyList<RgbaImage> ComposeGif(
        BitmapDecoder decoder,
        ImageHeaderReader.Header header,
        string path,
        CancellationToken cancellationToken)
    {
        int width = header.Width;
        int height = header.Height;
        if (ImageLimits.SideExceedsLimit(width, height))
        {
            throw TooLarge(path, width, height);
        }

        var canvas = new byte[width * height * RgbaImage.BytesPerPixel];
        byte[]? snapshot = null;
        var frames = new List<RgbaImage>(decoder.Frames.Count);
        BitmapPalette? palette = decoder.Palette;

        for (int i = 0; i < decoder.Frames.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BitmapFrame frame = decoder.Frames[i];
            var metadata = frame.Metadata as BitmapMetadata;
            int left = QueryInt(metadata, "/imgdesc/Left");
            int top = QueryInt(metadata, "/imgdesc/Top");
            int disposal = QueryInt(metadata, "/grctlext/Disposal");
            bool hasTransparency = QueryInt(metadata, "/grctlext/TransparencyFlag") != 0;
            int transparentIndex = QueryInt(metadata, "/grctlext/TransparentColorIndex", -1);

            if (disposal == 3)
            {
                snapshot = (byte[])canvas.Clone();
            }

            RgbaImage pixels = CopyFrame(frame, path, hasTransparency ? transparentIndex : -1);
            Blit(canvas, width, height, pixels, left, top);
            frames.Add(new RgbaImage(width, height, (byte[])canvas.Clone()));

            if (disposal == 2)
            {
                ClearRect(canvas, width, height, left, top, pixels.Width, pixels.Height, ClearColor(palette, header.GifBackgroundIndex, hasTransparency, transparentIndex));
            }
            else if (disposal == 3)
            {
                canvas = snapshot ?? new byte[canvas.Length];
                snapshot = null;
            }
        }

        return frames;
    }

    private static byte[] ClearColor(BitmapPalette? palette, int backgroundIndex, bool hasTransparency, int transparentIndex)
    {
        if (hasTransparency && transparentIndex == backgroundIndex)
        {
            return new byte[4];
        }

        if (palette is not null && (uint)backgroundIndex < (uint)palette.Colors.Count)
        {
            Color color = palette.Colors[backgroundIndex];
            return new[] { color.R, color.G, color.B, (byte)255 };
        }

        return new byte[4];
    }

    private static void Blit(byte[] canvas, int canvasWidth, int canvasHeight, RgbaImage frame, int left, int top)
    {
        ReadOnlySpan<byte> src = frame.Pixels;
        for (int y = 0; y < frame.Height; y++)
        {
            int cy = top + y;
            if ((uint)cy >= (uint)canvasHeight)
            {
                continue;
            }

            for (int x = 0; x < frame.Width; x++)
            {
                int cx = left + x;
                if ((uint)cx >= (uint)canvasWidth)
                {
                    continue;
                }

                int from = (y * frame.Width + x) * RgbaImage.BytesPerPixel;
                if (src[from + 3] == 0)
                {
                    continue;
                }

                int to = (cy * canvasWidth + cx) * RgbaImage.BytesPerPixel;
                canvas[to] = src[from];
                canvas[to + 1] = src[from + 1];
                canvas[to + 2] = src[from + 2];
                canvas[to + 3] = src[from + 3];
            }
        }
    }

    private static void ClearRect(byte[] canvas, int canvasWidth, int canvasHeight, int left, int top, int width, int height, byte[] color)
    {
        for (int y = 0; y < height; y++)
        {
            int cy = top + y;
            if ((uint)cy >= (uint)canvasHeight)
            {
                continue;
            }

            for (int x = 0; x < width; x++)
            {
                int cx = left + x;
                if ((uint)cx >= (uint)canvasWidth)
                {
                    continue;
                }

                int i = (cy * canvasWidth + cx) * RgbaImage.BytesPerPixel;
                canvas[i] = color[0];
                canvas[i + 1] = color[1];
                canvas[i + 2] = color[2];
                canvas[i + 3] = color[3];
            }
        }
    }

    private static RgbaImage CopyFrame(BitmapSource frame, string path, int transparentIndex = -1)
    {
        if (frame.Palette is not null && frame.Format == PixelFormats.Indexed8)
        {
            return CopyIndexed8(frame, path, transparentIndex);
        }

        if (frame.Palette is not null
            && frame.Format.BitsPerPixel is 1 or 2 or 4
            && (frame.Format == PixelFormats.Indexed1 || frame.Format == PixelFormats.Indexed2 || frame.Format == PixelFormats.Indexed4))
        {
            var indexed = new FormatConvertedBitmap(frame, PixelFormats.Indexed8, frame.Palette, 0);
            return CopyIndexed8(indexed, path, transparentIndex);
        }

        BitmapSource source = frame.Format == PixelFormats.Bgra32
            ? frame
            : new FormatConvertedBitmap(frame, PixelFormats.Bgra32, null, 0);
        return CopyBgra(source, path);
    }

    private static RgbaImage CopyIndexed8(BitmapSource frame, string path, int transparentIndex)
    {
        int width = frame.PixelWidth;
        int height = frame.PixelHeight;
        EnsureSide(path, width, height);
        int stride = (width + 3) & ~3;
        var indexes = new byte[stride * height];
        frame.CopyPixels(indexes, stride, 0);
        BitmapPalette palette = frame.Palette ?? throw Corrupted(path);
        var pixels = new byte[width * height * RgbaImage.BytesPerPixel];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = indexes[y * stride + x];
                int o = (y * width + x) * RgbaImage.BytesPerPixel;
                if (index == transparentIndex || (uint)index >= (uint)palette.Colors.Count)
                {
                    continue;
                }

                Color color = palette.Colors[index];
                pixels[o] = color.R;
                pixels[o + 1] = color.G;
                pixels[o + 2] = color.B;
                pixels[o + 3] = 255;
            }
        }

        return new RgbaImage(width, height, pixels);
    }

    private static RgbaImage CopyBgra(BitmapSource source, string path)
    {
        int width = source.PixelWidth;
        int height = source.PixelHeight;
        EnsureSide(path, width, height);
        int stride = width * RgbaImage.BytesPerPixel;
        var bgra = new byte[stride * height];
        source.CopyPixels(bgra, stride, 0);
        var pixels = new byte[bgra.Length];
        for (int i = 0; i < bgra.Length; i += RgbaImage.BytesPerPixel)
        {
            pixels[i] = bgra[i + 2];
            pixels[i + 1] = bgra[i + 1];
            pixels[i + 2] = bgra[i];
            pixels[i + 3] = bgra[i + 3];
        }

        return new RgbaImage(width, height, pixels);
    }

    private static RgbaImage ApplyOrientation(RgbaImage image, int orientation) => orientation switch
    {
        2 => RotateFlipStep.Flip(image, horizontal: true, vertical: false),
        3 => RotateFlipStep.Rotate(image, Rotation.Rotate180),
        4 => RotateFlipStep.Flip(image, horizontal: false, vertical: true),
        5 => RotateFlipStep.Apply(image, Rotation.Rotate90, flipHorizontal: true, flipVertical: false),
        6 => RotateFlipStep.Rotate(image, Rotation.Rotate90),
        7 => RotateFlipStep.Apply(image, Rotation.Rotate270, flipHorizontal: true, flipVertical: false),
        8 => RotateFlipStep.Rotate(image, Rotation.Rotate270),
        _ => image,
    };

    private static int ReadOrientation(BitmapMetadata? metadata)
    {
        if (metadata is null)
        {
            return 1;
        }

        string[] queries = ["/app1/ifd/{ushort=274}", "/app1/ifd/exif/{ushort=274}"];
        foreach (string query in queries)
        {
            int value = QueryInt(metadata, query, fallback: 0);
            if (value is >= 1 and <= 8)
            {
                return value;
            }
        }

        return 1;
    }

    private static int QueryInt(BitmapMetadata? metadata, string query, int fallback = 0)
    {
        if (metadata is null)
        {
            return fallback;
        }

        try
        {
            return metadata.GetQuery(query) switch
            {
                byte value => value,
                sbyte value => value,
                short value => value,
                ushort value => value,
                int value => value,
                uint value => (int)value,
                bool value => value ? 1 : 0,
                _ => fallback,
            };
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or InvalidOperationException or IOException)
        {
            return fallback;
        }
    }

    private static ImageHeaderReader.Header ReadHeader(string path)
    {
        try
        {
            using FileStream stream = Open(path);
            return ImageHeaderReader.Read(stream, path);
        }
        catch (ImageLoadException)
        {
            throw;
        }
        catch (IOException ex)
        {
            throw Io(path, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Io(path, ex);
        }
    }

    private static FileStream Open(string path)
    {
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (FileNotFoundException ex)
        {
            throw Io(path, ex);
        }
        catch (DirectoryNotFoundException ex)
        {
            throw Io(path, ex);
        }
        catch (UnauthorizedAccessException ex)
        {
            throw Io(path, ex);
        }
        catch (IOException ex)
        {
            throw Io(path, ex);
        }
    }

    private static void EnsureLimits(string path, ImageHeaderReader.Header header)
    {
        if (ImageLimits.SideExceedsLimit(header.Width, header.Height))
        {
            throw TooLarge(path, header.Width, header.Height);
        }

        if (ImageLimits.MemoryExceedsLimit(header.Width, header.Height, header.FrameCount))
        {
            throw Memory(path, header.Width, header.Height, header.FrameCount);
        }
    }

    private static void EnsureSide(string path, int width, int height)
    {
        if (width < 1 || height < 1)
        {
            throw Corrupted(path);
        }

        if (ImageLimits.SideExceedsLimit(width, height))
        {
            throw TooLarge(path, width, height);
        }
    }

    private static ImageLoadException TooLarge(string path, int width, int height) =>
        new(ImageLoadError.TooLarge, $"Image {width}x{height} exceeds {ImageLimits.MaxSide}x{ImageLimits.MaxSide}.", path)
        {
            Width = width,
            Height = height,
        };

    private static ImageLoadException Memory(string path, int width, int height, int frames) =>
        new(ImageLoadError.MemoryLimit, $"{frames} frames of {width}x{height} exceed 512 MB.", path)
        {
            Width = width,
            Height = height,
        };

    private static ImageLoadException Corrupted(string path, Exception? inner = null) =>
        new(ImageLoadError.Corrupted, "The image file is corrupted or truncated.", path, inner);

    private static ImageLoadException Unsupported(string path, Exception inner) =>
        new(ImageLoadError.Unsupported, "The file is not a BMP, PNG, JPEG or GIF image.", path, inner);

    private static ImageLoadException Io(string path, Exception inner) =>
        new(ImageLoadError.IoError, inner.Message, path, inner);
}
