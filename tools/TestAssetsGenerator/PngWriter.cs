using System.IO.Compression;
using System.Text;

namespace Image2Gdram.TestAssets;

/// <summary>PNG с оттенками серого, 8 бит, без сторонних библиотек (решение N-39).</summary>
public static class PngWriter
{
    private static readonly byte[] Signature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly uint[] CrcTable = BuildCrcTable();

    public static void Write(string path, PatternBitmap image)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(image);
        using var stream = File.Create(path);
        Write(stream, image);
    }

    public static void Write(Stream stream, PatternBitmap image)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(image);
        Write(stream, image.Width, image.Height, image.IsActive);
    }

    /// <summary>
    /// Тот же кодировщик, что пишет изображения приложения Б: активный пиксель — чёрный (0), фон — белый (255).
    /// </summary>
    public static void Write(string path, int width, int height, Func<int, int, bool> isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using FileStream stream = File.Create(path);
        Write(stream, width, height, isActive);
    }

    /// <summary>
    /// Тот же кодировщик, что пишет изображения приложения Б: активный пиксель — чёрный (0), фон — белый (255).
    /// </summary>
    public static void Write(Stream stream, int width, int height, Func<int, int, bool> isActive)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(isActive);
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
        stream.Write(Signature);
        WriteChunk(stream, "IHDR", Header(width, height));
        WriteChunk(stream, "IDAT", Compress(Pixels(width, height, isActive)));
        WriteChunk(stream, "IEND", ReadOnlySpan<byte>.Empty);
    }

    private static byte[] Header(int width, int height)
    {
        var data = new byte[13];
        WriteInt32(data, 0, width);
        WriteInt32(data, 4, height);
        data[8] = 8;
        return data;
    }

    private static byte[] Pixels(int width, int height, Func<int, int, bool> isActive)
    {
        int stride = width + 1;
        var raw = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            int row = y * stride;
            raw[row] = 0;
            for (int x = 0; x < width; x++)
            {
                raw[row + 1 + x] = isActive(x, y) ? (byte)0 : (byte)255;
            }
        }

        return raw;
    }

    private static byte[] Compress(byte[] raw)
    {
        using var output = new MemoryStream();
        using (var zlib = new ZLibStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            zlib.Write(raw);
        }

        return output.ToArray();
    }

    private static void WriteChunk(Stream stream, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> length = stackalloc byte[4];
        WriteInt32(length, 0, data.Length);
        stream.Write(length);
        byte[] typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);
        uint crc = Crc(typeBytes, data);
        Span<byte> crcBytes = stackalloc byte[4];
        WriteInt32(crcBytes, 0, (int)crc);
        stream.Write(crcBytes);
    }

    private static uint Crc(byte[] type, ReadOnlySpan<byte> data)
    {
        uint crc = 0xFFFF_FFFF;
        foreach (byte value in type)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        foreach (byte value in data)
        {
            crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFF_FFFF;
    }

    private static uint[] BuildCrcTable()
    {
        var table = new uint[256];
        for (uint i = 0; i < table.Length; i++)
        {
            uint value = i;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB8_8320u ^ (value >> 1) : value >> 1;
            }

            table[i] = value;
        }

        return table;
    }

    private static void WriteInt32(Span<byte> buffer, int offset, int value)
    {
        buffer[offset] = (byte)(value >> 24);
        buffer[offset + 1] = (byte)(value >> 16);
        buffer[offset + 2] = (byte)(value >> 8);
        buffer[offset + 3] = (byte)value;
    }
}
