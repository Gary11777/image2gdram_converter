namespace Image2Gdram.Core.Packing;

/// <summary>
/// Монохромная упаковка по разделам 3 и 4.3 ТЗ (решения F-01…F-04).
/// До инверсии активный пиксель — бит 1, фон и биты дополнения — 0; инверсия — <c>b ^= 0xFF</c> для всего байта.
/// </summary>
public sealed class Mono1bppPacker : IPacker
{
    private const int PageHeight = 8;

    public PixelFormat Format => PixelFormat.Mono1bpp;

    public int GetSize(int width, int height, PackingOptions options)
    {
        ValidateSize(width, height);
        ValidateOptions(options);
        return GetSizeCore(width, height, options);
    }

    public byte[] Pack(MonoBitmap bitmap, PackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        var result = new byte[GetSize(bitmap.Width, bitmap.Height, options)];
        PackCore(bitmap, options, result);
        return result;
    }

    public void Pack(MonoBitmap bitmap, PackingOptions options, Span<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        int size = GetSize(bitmap.Width, bitmap.Height, options);
        if (destination.Length != size)
        {
            throw new ArgumentException(
                $"Destination must be exactly {size} bytes long, got {destination.Length}.", nameof(destination));
        }

        PackCore(bitmap, options, destination);
    }

    public MonoBitmap Unpack(ReadOnlySpan<byte> data, int width, int height, PackingOptions options)
    {
        int size = GetSize(width, height, options);
        if (data.Length != size)
        {
            throw new ArgumentException($"Expected exactly {size} bytes, got {data.Length}.", nameof(data));
        }

        int fill = options.Invert ? 0xFF : 0x00;
        var bitmap = new MonoBitmap(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                BitLocation location = LocateCore(x, y, width, height, options);
                bitmap[x, y] = (((data[location.ByteIndex] ^ fill) >> location.Bit) & 1) != 0;
            }
        }

        return bitmap;
    }

    public BitLocation Locate(int x, int y, int width, int height, PackingOptions options)
    {
        ValidateSize(width, height);
        ValidateOptions(options);
        if ((uint)x >= (uint)width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), x, $"Column must be in 0..{width - 1}.");
        }

        if ((uint)y >= (uint)height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), y, $"Row must be in 0..{height - 1}.");
        }

        return LocateCore(x, y, width, height, options);
    }

    private static int GetSizeCore(int width, int height, PackingOptions options) =>
        options.Direction == PackDirection.Horizontal
            ? checked(height * CeilDiv(width, options.BitsPerByte))
            : checked(width * CeilDiv(height, PageHeight));

    private static BitLocation LocateCore(int x, int y, int width, int height, PackingOptions options)
    {
        bool msbFirst = options.BitOrder == BitOrder.MsbFirst;
        if (options.Direction == PackDirection.Horizontal)
        {
            int bits = options.BitsPerByte;
            int k = x % bits;
            return new BitLocation(y * CeilDiv(width, bits) + x / bits, msbFirst ? bits - 1 - k : k);
        }

        int page = y / PageHeight;
        int row = y % PageHeight;
        int index = options.PageTraversal == PageTraversal.ByColumns
            ? x * CeilDiv(height, PageHeight) + page
            : page * width + x;
        return new BitLocation(index, msbFirst ? PageHeight - 1 - row : row);
    }

    private static void PackCore(MonoBitmap bitmap, PackingOptions options, Span<byte> destination)
    {
        if (options.Direction == PackDirection.Horizontal)
        {
            PackHorizontal(bitmap, options, destination);
        }
        else
        {
            PackVertical(bitmap, options, destination);
        }

        if (options.Invert)
        {
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] ^= 0xFF;
            }
        }
    }

    private static void PackHorizontal(MonoBitmap bitmap, PackingOptions options, Span<byte> destination)
    {
        int width = bitmap.Width;
        int bits = options.BitsPerByte;
        bool msbFirst = options.BitOrder == BitOrder.MsbFirst;
        int bytesPerRow = CeilDiv(width, bits);

        for (int y = 0; y < bitmap.Height; y++)
        {
            ReadOnlySpan<bool> row = bitmap.GetRow(y);
            int rowStart = y * bytesPerRow;
            for (int j = 0; j < bytesPerRow; j++)
            {
                int x0 = j * bits;
                int count = Math.Min(bits, width - x0);
                int value = 0;
                for (int k = 0; k < count; k++)
                {
                    if (row[x0 + k])
                    {
                        value |= 1 << (msbFirst ? bits - 1 - k : k);
                    }
                }

                destination[rowStart + j] = (byte)value;
            }
        }
    }

    private static void PackVertical(MonoBitmap bitmap, PackingOptions options, Span<byte> destination)
    {
        int width = bitmap.Width;
        int pages = CeilDiv(bitmap.Height, PageHeight);
        bool msbFirst = options.BitOrder == BitOrder.MsbFirst;
        bool byColumns = options.PageTraversal == PageTraversal.ByColumns;

        destination.Clear();
        for (int y = 0; y < bitmap.Height; y++)
        {
            ReadOnlySpan<bool> row = bitmap.GetRow(y);
            int page = y / PageHeight;
            int k = y % PageHeight;
            byte mask = (byte)(1 << (msbFirst ? PageHeight - 1 - k : k));
            for (int x = 0; x < width; x++)
            {
                if (row[x])
                {
                    destination[byColumns ? x * pages + page : page * width + x] |= mask;
                }
            }
        }
    }

    private static void ValidateSize(int width, int height)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);
    }

    private void ValidateOptions(PackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (options.PixelFormat != Format)
        {
            throw new ArgumentException(
                $"Pixel format {options.PixelFormat} is not supported by the {Format} packer.", nameof(options));
        }

        if (!Enum.IsDefined(options.Direction) || !Enum.IsDefined(options.BitOrder)
            || !Enum.IsDefined(options.PageTraversal) || !Enum.IsDefined(options.ByteOrder))
        {
            throw new ArgumentException("Packing options contain an undefined enum value.", nameof(options));
        }

        if (options.BitsPerByte is not (8 or 6))
        {
            throw new ArgumentException(
                $"Bits per byte must be 8 or 6, got {options.BitsPerByte}.", nameof(options));
        }
    }

    private static int CeilDiv(int value, int divisor) => (value + divisor - 1) / divisor;
}
