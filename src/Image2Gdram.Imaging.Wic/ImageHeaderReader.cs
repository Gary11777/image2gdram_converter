using System.Buffers.Binary;
using System.IO;
using Image2Gdram.Core.Imaging;

namespace Image2Gdram.Imaging.Wic;

/// <summary>
/// Размер кадра из заголовка файла, до копирования пикселей (п. 4.1.1 ТЗ, решение N-39).
/// Для GIF заодно считает кадры, не распаковывая LZW.
/// </summary>
internal static class ImageHeaderReader
{
    internal readonly record struct Header(ImageFileFormat Format, int Width, int Height, int FrameCount, int GifBackgroundIndex);

    public static Header Read(Stream stream, string path)
    {
        byte[] start = new byte[32];
        int read = ReadSome(stream, start);
        if (read >= 8 && IsPng(start))
        {
            return ReadPng(stream, start, read, path);
        }

        if (read >= 2 && start[0] == (byte)'B' && start[1] == (byte)'M')
        {
            return ReadBmp(stream, start, read, path);
        }

        if (read >= 6 && IsGif(start))
        {
            return ReadGif(stream, start, read, path);
        }

        if (read >= 2 && start[0] == 0xFF && start[1] == 0xD8)
        {
            return ReadJpeg(stream, path);
        }

        throw Unsupported(path);
    }

    private static Header ReadPng(Stream stream, byte[] start, int read, string path)
    {
        if (read < 24)
        {
            read += ReadSome(stream, start.AsSpan(read));
        }

        if (read < 24 || start[12] != (byte)'I' || start[13] != (byte)'H' || start[14] != (byte)'D' || start[15] != (byte)'R')
        {
            throw Corrupted(path);
        }

        int width = BinaryPrimitives.ReadInt32BigEndian(start[16..]);
        int height = BinaryPrimitives.ReadInt32BigEndian(start[20..]);
        return Size(ImageFileFormat.Png, width, height, 1, 0, path);
    }

    private static Header ReadBmp(Stream stream, byte[] start, int read, string path)
    {
        if (read < 26)
        {
            byte[] extra = new byte[26];
            start.AsSpan(0, read).CopyTo(extra);
            read += ReadSome(stream, extra.AsSpan(read));
            start = extra;
        }

        if (read < 22)
        {
            throw Corrupted(path);
        }

        uint headerSize = BinaryPrimitives.ReadUInt32LittleEndian(start[14..]);
        int width;
        int height;
        if (headerSize == 12)
        {
            width = BinaryPrimitives.ReadUInt16LittleEndian(start[18..]);
            height = BinaryPrimitives.ReadUInt16LittleEndian(start[20..]);
        }
        else if (headerSize >= 16 && read >= 26)
        {
            width = BinaryPrimitives.ReadInt32LittleEndian(start[18..]);
            height = BinaryPrimitives.ReadInt32LittleEndian(start[22..]);
            if (height == int.MinValue)
            {
                throw Corrupted(path);
            }

            height = Math.Abs(height);
        }
        else
        {
            throw Corrupted(path);
        }

        return Size(ImageFileFormat.Bmp, width, height, 1, 0, path);
    }

    private static Header ReadGif(Stream stream, byte[] start, int read, string path)
    {
        if (read < 13)
        {
            byte[] extra = new byte[13];
            start.AsSpan(0, read).CopyTo(extra);
            read += ReadSome(stream, extra.AsSpan(read));
            if (read < 13)
            {
                throw Corrupted(path);
            }

            start = extra;
        }

        int width = BinaryPrimitives.ReadUInt16LittleEndian(start.AsSpan(6));
        int height = BinaryPrimitives.ReadUInt16LittleEndian(start.AsSpan(8));
        if (width < 1 || height < 1)
        {
            throw Corrupted(path);
        }

        byte packed = start[10];
        int background = start[11];
        int gctBytes = (packed & 0x80) != 0 ? 3 * (1 << ((packed & 7) + 1)) : 0;
        // Первое чтение уже сдвинуло поток дальше 13-го байта. Таблица цветов читается с его начала.
        stream.Position = 13;
        Skip(stream, gctBytes, path);
        int frames = CountGifFrames(stream, path);
        return new Header(ImageFileFormat.Gif, width, height, frames, background);
    }

    private static int CountGifFrames(Stream stream, string path)
    {
        int frames = 0;
        Span<byte> descriptor = stackalloc byte[9];
        while (true)
        {
            int block = stream.ReadByte();
            if (block < 0)
            {
                throw Corrupted(path);
            }

            if (block == 0x3B)
            {
                return frames > 0 ? frames : throw Corrupted(path);
            }

            if (block == 0x21)
            {
                if (stream.ReadByte() < 0)
                {
                    throw Corrupted(path);
                }

                SkipSubBlocks(stream, path);
                continue;
            }

            if (block == 0x2C)
            {
                frames++;
                ReadExactly(stream, descriptor, path);
                if ((descriptor[8] & 0x80) != 0)
                {
                    Skip(stream, 3 * (1 << ((descriptor[8] & 7) + 1)), path);
                }

                if (stream.ReadByte() < 0)
                {
                    throw Corrupted(path);
                }

                SkipSubBlocks(stream, path);
                continue;
            }

            throw Corrupted(path);
        }
    }

    private static Header ReadJpeg(Stream stream, string path)
    {
        stream.Position = 2;
        while (true)
        {
            int marker = ReadJpegMarker(stream, path);
            if (marker == 0xD9 || marker == 0xDA)
            {
                throw Corrupted(path);
            }

            if (IsSof(marker))
            {
                int length = ReadU16(stream, path);
                if (length < 8)
                {
                    throw Corrupted(path);
                }

                if (stream.ReadByte() < 0)
                {
                    throw Corrupted(path);
                }

                int height = ReadU16(stream, path);
                int width = ReadU16(stream, path);
                return Size(ImageFileFormat.Jpeg, width, height, 1, 0, path);
            }

            if (marker == 0x01 || marker is >= 0xD0 and <= 0xD7)
            {
                continue;
            }

            int segment = ReadU16(stream, path);
            if (segment < 2)
            {
                throw Corrupted(path);
            }

            Skip(stream, segment - 2, path);
        }
    }

    private static int ReadJpegMarker(Stream stream, string path)
    {
        int lead;
        do
        {
            lead = stream.ReadByte();
            if (lead < 0)
            {
                throw Corrupted(path);
            }
        }
        while (lead != 0xFF);

        int marker;
        do
        {
            marker = stream.ReadByte();
            if (marker < 0)
            {
                throw Corrupted(path);
            }
        }
        while (marker == 0xFF);

        return marker;
    }

    private static bool IsSof(int marker) => marker is 0xC0 or 0xC1 or 0xC2 or 0xC3 or 0xC5 or 0xC6 or 0xC7
        or 0xC9 or 0xCA or 0xCB or 0xCD or 0xCE or 0xCF;

    private static Header Size(ImageFileFormat format, int width, int height, int frames, int background, string path)
    {
        if (width < 1 || height < 1)
        {
            throw Corrupted(path);
        }

        return new Header(format, width, height, frames, background);
    }

    private static bool IsPng(byte[] start) =>
        start[0] == 0x89 && start[1] == 0x50 && start[2] == 0x4E && start[3] == 0x47
        && start[4] == 0x0D && start[5] == 0x0A && start[6] == 0x1A && start[7] == 0x0A;

    private static bool IsGif(byte[] start) =>
        start[0] == (byte)'G' && start[1] == (byte)'I' && start[2] == (byte)'F'
        && start[3] == (byte)'8' && (start[4] == (byte)'7' || start[4] == (byte)'9') && start[5] == (byte)'a';

    private static int ReadSome(Stream stream, Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int n = stream.Read(buffer[total..]);
            if (n == 0)
            {
                break;
            }

            total += n;
        }

        return total;
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer, string path)
    {
        if (ReadSome(stream, buffer) != buffer.Length)
        {
            throw Corrupted(path);
        }
    }

    private static int ReadU16(Stream stream, string path)
    {
        Span<byte> bytes = stackalloc byte[2];
        ReadExactly(stream, bytes, path);
        return BinaryPrimitives.ReadUInt16BigEndian(bytes);
    }

    private static void Skip(Stream stream, int count, string path)
    {
        Span<byte> junk = stackalloc byte[256];
        while (count > 0)
        {
            int n = stream.Read(junk[..Math.Min(junk.Length, count)]);
            if (n == 0)
            {
                throw Corrupted(path);
            }

            count -= n;
        }
    }

    private static void SkipSubBlocks(Stream stream, string path)
    {
        while (true)
        {
            int size = stream.ReadByte();
            if (size < 0)
            {
                throw Corrupted(path);
            }

            if (size == 0)
            {
                return;
            }

            Skip(stream, size, path);
        }
    }

    private static ImageLoadException Unsupported(string path) =>
        new(ImageLoadError.Unsupported, "Файл не является изображением BMP, PNG, JPEG или GIF.", path);

    private static ImageLoadException Corrupted(string path) =>
        new(ImageLoadError.Corrupted, "Файл изображения повреждён или обрезан.", path);
}
