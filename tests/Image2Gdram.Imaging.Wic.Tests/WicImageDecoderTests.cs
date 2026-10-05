using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Image2Gdram.Core.Imaging;
using Image2Gdram.TestAssets;

namespace Image2Gdram.Imaging.Wic.Tests;

public sealed class WicImageDecoderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-" + Guid.NewGuid().ToString("N"));
    private readonly WicImageDecoder _decoder = new();

    public WicImageDecoderTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void Decodes_png_bmp_jpeg_and_static_gif()
    {
        string png = SaveEncoded("red.png", Solid(2, 2, Colors.Red), new PngBitmapEncoder());
        string bmp = SaveEncoded("red.bmp", Solid(2, 2, Colors.Red), new BmpBitmapEncoder());
        string jpeg = SaveEncoded("red.jpg", Solid(8, 8, Colors.Red), new JpegBitmapEncoder { QualityLevel = 100 });
        string gif = Write("red.gif", GifEncoder.Encode(1, 1, new[] { new GifEncoder.Frame(0, 0, 1, 1, new byte[] { 1 }, 1, false) }));

        AssertPixel(png, ImageFileFormat.Png, 255, 0, 0, 255);
        AssertPixel(bmp, ImageFileFormat.Bmp, 255, 0, 0, 255);
        AssertPixel(jpeg, ImageFileFormat.Jpeg, 255, 0, 0, 255, tolerance: 8);
        AssertPixel(gif, ImageFileFormat.Gif, 255, 0, 0, 255);
        Assert.False(_decoder.Decode(gif).IsAnimated);
    }

    [Fact]
    public void Png_keeps_straight_alpha()
    {
        var pixels = new byte[4];
        pixels[2] = 255;
        pixels[3] = 128;
        string path = SaveEncoded("alpha.png", BitmapSource.Create(1, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 4), new PngBitmapEncoder());

        RgbaImage image = _decoder.Decode(path).Frames[0];
        image.GetPixel(0, 0, out byte r, out byte g, out byte b, out byte a);
        Assert.Equal((255, 0, 0, 128), (r, g, b, a));
    }

    [Fact]
    public void Bmp32_keeps_alpha()
    {
        string path = Write("alpha.bmp", Bmp32(10, 20, 30, 128));
        RgbaImage image = _decoder.Decode(path).Frames[0];
        image.GetPixel(0, 0, out byte r, out byte g, out byte b, out byte a);
        Assert.Equal((10, 20, 30, 128), (r, g, b, a));
    }

    [Fact]
    public void Sixteen_bit_gray_and_palette_become_8_bit_rgba()
    {
        byte[] gray16 = [0x00, 0x80];
        string grayPath = SaveEncoded("gray16.png", BitmapSource.Create(1, 1, 96, 96, PixelFormats.Gray16, null, gray16, 2), new PngBitmapEncoder());
        RgbaImage gray = _decoder.Decode(grayPath).Frames[0];
        gray.GetPixel(0, 0, out byte r, out _, out _, out byte a);
        Assert.InRange(r, 127, 129);
        Assert.Equal(255, a);

        var palette = new BitmapPalette(new List<Color> { Colors.Black, Colors.White, Colors.Red });
        string indexed = SaveEncoded("indexed.png", BitmapSource.Create(1, 1, 96, 96, PixelFormats.Indexed8, palette, new byte[] { 2 }, 1), new PngBitmapEncoder());
        RgbaImage color = _decoder.Decode(indexed).Frames[0];
        color.GetPixel(0, 0, out r, out byte g, out byte b, out a);
        Assert.Equal((255, 0, 0, 255), (r, g, b, a));
    }

    [Fact]
    public void Jpeg_orientation_6_rotates_clockwise()
    {
        var pixels = new byte[16 * 8 * 4];
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 16; x++)
            {
                byte value = x < 8 ? (byte)0 : (byte)255;
                int i = (y * 16 + x) * 4;
                pixels[i] = value;
                pixels[i + 1] = value;
                pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
        }

        var metadata = new BitmapMetadata("jpg");
        metadata.SetQuery("/app1/ifd/{ushort=274}", (ushort)6);
        var source = BitmapSource.Create(16, 8, 96, 96, PixelFormats.Bgra32, null, pixels, 16 * 4);
        string path = SaveEncoded("turned.jpg", source, new JpegBitmapEncoder { QualityLevel = 100 }, metadata);

        RgbaImage image = _decoder.Decode(path).Frames[0];
        Assert.Equal(8, image.Width);
        Assert.Equal(16, image.Height);
        image.GetPixel(0, 1, out byte top, out _, out _, out _);
        image.GetPixel(0, 14, out byte bottom, out _, out _, out _);
        Assert.InRange(top, 0, 20);
        Assert.InRange(bottom, 235, 255);
    }

    [Fact]
    public void Side_8192_is_accepted_and_8193_is_too_large_before_pixel_copy()
    {
        string wide = SaveEncoded("wide.png", Solid(8192, 1, Colors.Black), new PngBitmapEncoder());
        DecodedImage decoded = _decoder.Decode(wide);
        Assert.Equal(8192, decoded.Width);
        decoded.Frames[0].GetPixel(0, 0, out byte r, out _, out _, out _);
        Assert.Equal(0, r);

        string tooWide = Write("huge.png", PngHeader(8193, 1));
        ImageLoadException identified = Assert.Throws<ImageLoadException>(() => _decoder.Identify(tooWide));
        ImageLoadException decodedError = Assert.Throws<ImageLoadException>(() => _decoder.Decode(tooWide));
        Assert.Equal(ImageLoadError.TooLarge, identified.Error);
        Assert.Equal(ImageLoadError.TooLarge, decodedError.Error);
        Assert.Equal(8193, identified.Width);
    }

    [Fact]
    public void Huge_gif_is_rejected_by_the_memory_limit()
    {
        var frame = new GifEncoder.Frame(0, 0, 1, 1, new byte[] { 1 }, 0, false);
        string three = Write("three.gif", GifEncoder.Encode(8192, 8192, new[] { frame, frame, frame }));
        string two = Write("two.gif", GifEncoder.Encode(8192, 8192, new[] { frame, frame }));

        ImageInfo info = _decoder.Identify(two);
        Assert.Equal(2, info.FrameCount);
        Assert.Equal(8192, info.Width);

        ImageLoadException error = Assert.Throws<ImageLoadException>(() => _decoder.Decode(three));
        Assert.Equal(ImageLoadError.MemoryLimit, error.Error);
    }

    [Fact]
    public void Animated_gif_composites_disposal()
    {
        byte[] leftRed = new byte[8];
        leftRed[0] = 1;
        leftRed[1] = 1;
        leftRed[4] = 1;
        leftRed[5] = 1;
        var frames = new[]
        {
            new GifEncoder.Frame(0, 0, 4, 2, leftRed, Disposal: 1, Transparent: true),
            new GifEncoder.Frame(3, 0, 1, 1, new byte[] { 3 }, Disposal: 2, Transparent: true),
            new GifEncoder.Frame(3, 1, 1, 1, new byte[] { 2 }, Disposal: 1, Transparent: true),
        };
        string path = Write("anim.gif", GifEncoder.Encode(4, 2, frames));

        DecodedImage image = _decoder.Decode(path);
        Assert.True(image.IsAnimated);
        Assert.Equal(3, image.Frames.Count);
        AssertColor(image.Frames[0], 0, 0, 255, 0, 0, 255);
        AssertColor(image.Frames[0], 3, 0, 0, 0, 0, 0);
        AssertColor(image.Frames[1], 0, 0, 255, 0, 0, 255);
        AssertColor(image.Frames[1], 3, 0, 0, 0, 255, 255);
        AssertColor(image.Frames[2], 0, 0, 255, 0, 0, 255);
        AssertColor(image.Frames[2], 3, 0, 0, 0, 0, 0);
        AssertColor(image.Frames[2], 3, 1, 0, 255, 0, 255);
    }

    [Fact]
    public void Disposal_restore_previous_drops_the_frame_after_display()
    {
        var frames = new[]
        {
            new GifEncoder.Frame(0, 0, 3, 1, new byte[] { 1, 1, 1 }, Disposal: 3, Transparent: false),
            new GifEncoder.Frame(0, 0, 1, 1, new byte[] { 3 }, Disposal: 1, Transparent: true),
            new GifEncoder.Frame(1, 0, 1, 1, new byte[] { 2 }, Disposal: 1, Transparent: true),
        };
        string path = Write("restore.gif", GifEncoder.Encode(3, 1, frames));

        IReadOnlyList<RgbaImage> decoded = _decoder.Decode(path).Frames;
        AssertColor(decoded[0], 2, 0, 255, 0, 0, 255);
        AssertColor(decoded[2], 0, 0, 0, 0, 255, 255);
        AssertColor(decoded[2], 1, 0, 0, 255, 0, 255);
        AssertColor(decoded[2], 2, 0, 0, 0, 0, 0);
    }

    [Fact]
    public void Broken_and_foreign_files_do_not_crash()
    {
        string text = Write("notes.png", "не картинка"u8.ToArray());
        string truncated = Write("cut.png", PngHeader(2, 2));
        string missing = Path.Combine(_directory, "missing.png");

        Assert.Equal(ImageLoadError.Unsupported, Assert.Throws<ImageLoadException>(() => _decoder.Decode(text)).Error);
        ImageInfo info = _decoder.Identify(truncated);
        Assert.Equal(2, info.Width);
        Assert.Equal(ImageLoadError.Corrupted, Assert.Throws<ImageLoadException>(() => _decoder.Decode(truncated)).Error);
        Assert.Equal(ImageLoadError.IoError, Assert.Throws<ImageLoadException>(() => _decoder.Decode(missing)).Error);
    }

    [Fact]
    public void Decode_does_not_modify_or_lock_the_file()
    {
        string path = SaveEncoded("stable.png", Solid(2, 2, Colors.Blue), new PngBitmapEncoder());
        byte[] before = File.ReadAllBytes(path);
        _decoder.Decode(path);
        Assert.Equal(before, File.ReadAllBytes(path));

        using var rewrite = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        Assert.True(rewrite.CanWrite);
    }

    [Fact]
    public void Cancelled_decode_stops()
    {
        string path = SaveEncoded("cancel.png", Solid(2, 2, Colors.White), new PngBitmapEncoder());
        using var source = new CancellationTokenSource();
        source.Cancel();
        Assert.Throws<OperationCanceledException>(() => _decoder.Decode(path, source.Token));
    }

    [Fact]
    public void Generated_png_matches_the_test_pattern()
    {
        PatternBitmap pattern = TestPattern.CreateSprite();
        string path = Path.Combine(_directory, "sprite.png");
        PngWriter.Write(path, pattern);

        RgbaImage image = _decoder.Decode(path).Frames[0];
        Assert.Equal(13, image.Width);
        Assert.Equal(11, image.Height);
        for (int y = 0; y < pattern.Height; y++)
        {
            for (int x = 0; x < pattern.Width; x++)
            {
                image.GetPixel(x, y, out byte r, out _, out _, out byte a);
                Assert.Equal(255, a);
                Assert.Equal(pattern.IsActive(x, y) ? 0 : 255, r);
            }
        }
    }

    private void AssertPixel(string path, ImageFileFormat format, byte r, byte g, byte b, byte a, int tolerance = 0)
    {
        ImageInfo info = _decoder.Identify(path);
        DecodedImage decoded = _decoder.Decode(path);
        Assert.Equal(format, info.Format);
        Assert.Equal(format, decoded.Format);
        Assert.Equal(info.Width, decoded.Width);
        Assert.Equal(info.Height, decoded.Height);
        decoded.Frames[0].GetPixel(0, 0, out byte pr, out byte pg, out byte pb, out byte pa);
        Assert.InRange(pr, r - tolerance, r + tolerance);
        Assert.InRange(pg, g - tolerance, g + tolerance);
        Assert.InRange(pb, b - tolerance, b + tolerance);
        Assert.Equal(a, pa);
    }

    private static void AssertColor(RgbaImage image, int x, int y, byte r, byte g, byte b, byte a)
    {
        image.GetPixel(x, y, out byte pr, out byte pg, out byte pb, out byte pa);
        Assert.Equal((r, g, b, a), (pr, pg, pb, pa));
    }

    private string SaveEncoded(string name, BitmapSource source, BitmapEncoder encoder, BitmapMetadata? metadata = null)
    {
        encoder.Frames.Add(BitmapFrame.Create(source, thumbnail: null, metadata, colorContexts: null));
        string path = Path.Combine(_directory, name);
        using var stream = File.Create(path);
        encoder.Save(stream);
        return path;
    }

    private string Write(string name, byte[] data)
    {
        string path = Path.Combine(_directory, name);
        File.WriteAllBytes(path, data);
        return path;
    }

    private static BitmapSource Solid(int width, int height, Color color)
    {
        var pixels = new byte[width * height * 4];
        for (int i = 0; i < pixels.Length; i += 4)
        {
            pixels[i] = color.B;
            pixels[i + 1] = color.G;
            pixels[i + 2] = color.R;
            pixels[i + 3] = color.A;
        }

        return BitmapSource.Create(width, height, 96, 96, PixelFormats.Bgra32, null, pixels, width * 4);
    }

    private static byte[] PngHeader(int width, int height)
    {
        var data = new byte[24];
        data[0] = 0x89;
        data[1] = 0x50;
        data[2] = 0x4E;
        data[3] = 0x47;
        data[4] = 0x0D;
        data[5] = 0x0A;
        data[6] = 0x1A;
        data[7] = 0x0A;
        data[12] = (byte)'I';
        data[13] = (byte)'H';
        data[14] = (byte)'D';
        data[15] = (byte)'R';
        data[16] = (byte)(width >> 24);
        data[17] = (byte)(width >> 16);
        data[18] = (byte)(width >> 8);
        data[19] = (byte)width;
        data[20] = (byte)(height >> 24);
        data[21] = (byte)(height >> 16);
        data[22] = (byte)(height >> 8);
        data[23] = (byte)height;
        return data;
    }

    private static byte[] Bmp32(byte r, byte g, byte b, byte a)
    {
        const int dib = 124;
        const int pixelOffset = 14 + dib;
        var data = new byte[pixelOffset + 4];
        data[0] = (byte)'B';
        data[1] = (byte)'M';
        WriteI32(data, 2, data.Length);
        WriteI32(data, 10, pixelOffset);
        WriteI32(data, 14, dib);
        WriteI32(data, 18, 1);
        WriteI32(data, 22, 1);
        WriteI16(data, 26, 1);
        WriteI16(data, 28, 32);
        WriteI32(data, 30, 3);
        WriteI32(data, 34, 4);
        WriteI32(data, 54, 0x00FF0000);
        WriteI32(data, 58, 0x0000FF00);
        WriteI32(data, 62, 0x000000FF);
        WriteI32(data, 66, unchecked((int)0xFF000000));
        WriteI32(data, 70, 0x73524742);
        data[pixelOffset] = b;
        data[pixelOffset + 1] = g;
        data[pixelOffset + 2] = r;
        data[pixelOffset + 3] = a;
        return data;
    }

    private static void WriteI32(byte[] data, int offset, int value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
        data[offset + 2] = (byte)(value >> 16);
        data[offset + 3] = (byte)(value >> 24);
    }

    private static void WriteI16(byte[] data, int offset, int value)
    {
        data[offset] = (byte)value;
        data[offset + 1] = (byte)(value >> 8);
    }
}
