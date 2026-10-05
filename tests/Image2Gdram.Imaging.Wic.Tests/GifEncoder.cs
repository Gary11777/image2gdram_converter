using System.IO;

namespace Image2Gdram.Imaging.Wic.Tests;

/// <summary>Минимальный GIF89a для проверки компоновки кадров. Палитра из четырёх цветов, LZW с порядком бит GIF.</summary>
internal static class GifEncoder
{
    internal readonly record struct Frame(int Left, int Top, int Width, int Height, byte[] Indexes, int Disposal, bool Transparent);

    public static byte[] Encode(int width, int height, IReadOnlyList<Frame> frames, int backgroundIndex = 0)
    {
        byte[] palette =
        [
            0, 0, 0,
            255, 0, 0,
            0, 255, 0,
            0, 0, 255,
        ];

        using var output = new MemoryStream();
        output.Write("GIF89a"u8);
        WriteU16(output, width);
        WriteU16(output, height);
        output.WriteByte(0xF1);
        output.WriteByte((byte)backgroundIndex);
        output.WriteByte(0);
        output.Write(palette);

        foreach (Frame frame in frames)
        {
            output.WriteByte(0x21);
            output.WriteByte(0xF9);
            output.WriteByte(0x04);
            output.WriteByte((byte)((frame.Disposal << 2) | (frame.Transparent ? 1 : 0)));
            WriteU16(output, 0);
            output.WriteByte(0);
            output.WriteByte(0);

            output.WriteByte(0x2C);
            WriteU16(output, frame.Left);
            WriteU16(output, frame.Top);
            WriteU16(output, frame.Width);
            WriteU16(output, frame.Height);
            output.WriteByte(0);
            byte[] compressed = Lzw.Compress(frame.Indexes, minCodeSize: 2);
            output.WriteByte(2);
            int offset = 0;
            while (offset < compressed.Length)
            {
                int count = Math.Min(255, compressed.Length - offset);
                output.WriteByte((byte)count);
                output.Write(compressed, offset, count);
                offset += count;
            }

            output.WriteByte(0);
        }

        output.WriteByte(0x3B);
        return output.ToArray();
    }

    private static void WriteU16(Stream stream, int value)
    {
        stream.WriteByte((byte)value);
        stream.WriteByte((byte)(value >> 8));
    }

    private static class Lzw
    {
        public static byte[] Compress(byte[] indexes, int minCodeSize)
        {
            int clear = 1 << minCodeSize;
            int eoi = clear + 1;
            int codeSize = minCodeSize + 1;
            int nextCode = eoi + 1;
            int maxCode = 1 << codeSize;
            var dictionary = new Dictionary<(int Prefix, byte Suffix), int>();
            var bits = new BitWriter();
            bits.Write(clear, codeSize);

            int prefix = indexes[0];
            for (int i = 1; i < indexes.Length; i++)
            {
                byte suffix = indexes[i];
                var key = (prefix, suffix);
                if (dictionary.TryGetValue(key, out int existing))
                {
                    prefix = existing;
                    continue;
                }

                bits.Write(prefix, codeSize);
                if (nextCode < 4096)
                {
                    dictionary[key] = nextCode++;
                    if (nextCode == maxCode && codeSize < 12)
                    {
                        codeSize++;
                        maxCode <<= 1;
                    }
                }
                else
                {
                    bits.Write(clear, codeSize);
                    dictionary.Clear();
                    codeSize = minCodeSize + 1;
                    nextCode = eoi + 1;
                    maxCode = 1 << codeSize;
                }

                prefix = suffix;
            }

            bits.Write(prefix, codeSize);
            bits.Write(eoi, codeSize);
            return bits.ToArray();
        }
    }

    private sealed class BitWriter
    {
        private readonly List<byte> _bytes = new();
        private int _accumulator;
        private int _count;

        public void Write(int code, int length)
        {
            _accumulator |= code << _count;
            _count += length;
            while (_count >= 8)
            {
                _bytes.Add((byte)_accumulator);
                _accumulator >>= 8;
                _count -= 8;
            }
        }

        public byte[] ToArray()
        {
            if (_count > 0)
            {
                _bytes.Add((byte)_accumulator);
            }

            return _bytes.ToArray();
        }
    }
}
