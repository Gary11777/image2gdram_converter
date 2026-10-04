using Image2Gdram.Core.Packing;
using Image2Gdram.Reference;

namespace Image2Gdram.Core.Tests.Packing;

public class LocateAndUnpackTests
{
    private readonly Mono1bppPacker _packer = new();

    /// <summary>
    /// Locate согласован с Pack: бит, на который указывает Locate, равен «активен XOR инверсия»;
    /// разные пиксели не делят бит; все биты, не занятые пикселями, — биты дополнения со значением фона.
    /// </summary>
    [Theory]
    [MemberData(nameof(TestBitmaps.SizeAndCombination), MemberType = typeof(TestBitmaps))]
    public void Locate_is_consistent_with_pack(int width, int height, int combination)
    {
        PackingOptions options = TestBitmaps.ToCore(RefOptions.AllCombinations[combination]);
        MonoBitmap bitmap = TestBitmaps.Random(width, height, TestBitmaps.SeedFor(width, height));
        byte[] bytes = _packer.Pack(bitmap, options);
        var covered = new bool[bytes.Length * 8];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                BitLocation location = _packer.Locate(x, y, width, height, options);
                Assert.InRange(location.ByteIndex, 0, bytes.Length - 1);
                Assert.InRange(location.Bit, 0, 7);

                int slot = location.ByteIndex * 8 + location.Bit;
                Assert.False(covered[slot], $"Pixel ({x}, {y}) shares a bit with another pixel.");
                covered[slot] = true;

                bool bit = ((bytes[location.ByteIndex] >> location.Bit) & 1) != 0;
                Assert.Equal(bitmap[x, y] ^ options.Invert, bit);
            }
        }

        for (int slot = 0; slot < covered.Length; slot++)
        {
            if (!covered[slot])
            {
                bool bit = ((bytes[slot / 8] >> (slot % 8)) & 1) != 0;
                Assert.Equal(options.Invert, bit);
            }
        }
    }

    [Theory]
    [MemberData(nameof(TestBitmaps.SizeAndCombination), MemberType = typeof(TestBitmaps))]
    public void Unpack_restores_packed_bitmap(int width, int height, int combination)
    {
        PackingOptions options = TestBitmaps.ToCore(RefOptions.AllCombinations[combination]);
        MonoBitmap bitmap = TestBitmaps.Random(width, height, TestBitmaps.SeedFor(width, height));

        MonoBitmap restored = _packer.Unpack(_packer.Pack(bitmap, options), width, height, options);

        Assert.True(bitmap.ContentEquals(restored));
    }

    [Fact]
    public void Unpack_restores_1024x1024_for_all_combinations()
    {
        MonoBitmap bitmap = TestBitmaps.Random(1024, 1024, 1024);

        foreach (RefOptions reference in RefOptions.AllCombinations)
        {
            PackingOptions options = TestBitmaps.ToCore(reference);
            MonoBitmap restored = _packer.Unpack(_packer.Pack(bitmap, options), 1024, 1024, options);

            Assert.True(bitmap.ContentEquals(restored), $"Mismatch for {reference.Name}");
        }
    }

    [Theory]
    [MemberData(nameof(TestBitmaps.SizeAndCombination), MemberType = typeof(TestBitmaps))]
    public void Unpack_ignores_padding_bits(int width, int height, int combination)
    {
        PackingOptions options = TestBitmaps.ToCore(RefOptions.AllCombinations[combination]);
        MonoBitmap bitmap = TestBitmaps.Random(width, height, TestBitmaps.SeedFor(width, height));
        byte[] bytes = _packer.Pack(bitmap, options);

        // Все биты, не принадлежащие пикселям, выставляются в противоположное фону значение.
        var paddingMask = Enumerable.Repeat((byte)0xFF, bytes.Length).ToArray();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                BitLocation location = _packer.Locate(x, y, width, height, options);
                paddingMask[location.ByteIndex] &= (byte)~(1 << location.Bit);
            }
        }

        for (int i = 0; i < bytes.Length; i++)
        {
            bytes[i] ^= paddingMask[i];
        }

        Assert.True(bitmap.ContentEquals(_packer.Unpack(bytes, width, height, options)));
    }

    [Fact]
    public void Unpack_of_inverted_background_gives_empty_bitmap()
    {
        var options = new PackingOptions { Direction = PackDirection.Horizontal, BitsPerByte = 6, Invert = true };
        byte[] bytes = Enumerable.Repeat((byte)0xFF, _packer.GetSize(6, 8, options)).ToArray();

        MonoBitmap glyph = _packer.Unpack(bytes, 6, 8, options);

        Assert.True(glyph.ContentEquals(new MonoBitmap(6, 8)));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(7)]
    public void Unpack_rejects_wrong_length(int length)
    {
        Assert.Throws<ArgumentException>(() => _packer.Unpack(new byte[length], 6, 8, PackingOptions.Default));
    }

    [Fact]
    public void Pack_into_span_matches_pack_and_rejects_wrong_length()
    {
        MonoBitmap bitmap = TestBitmaps.Random(13, 11, 13);
        var options = new PackingOptions { Direction = PackDirection.Horizontal, BitsPerByte = 6, Invert = true };
        var buffer = new byte[_packer.GetSize(13, 11, options)];

        _packer.Pack(bitmap, options, buffer);

        Assert.Equal(_packer.Pack(bitmap, options), buffer);
        Assert.Throws<ArgumentException>(() => _packer.Pack(bitmap, options, new byte[buffer.Length + 1]));
    }

    [Fact]
    public void Locate_gives_tooltip_values_for_t6963c_6_bit_mode()
    {
        // 240 пикселей в 6-битном режиме: 40 байт в строке; пиксель (7, 2) — байт 2·40 + 1, бит 5 − 1.
        var options = new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst, BitsPerByte = 6 };

        Assert.Equal(new BitLocation(81, 4), _packer.Locate(7, 2, 240, 128, options));
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(13, 0)]
    [InlineData(0, 11)]
    public void Locate_rejects_pixels_outside_bitmap(int x, int y)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _packer.Locate(x, y, 13, 11, PackingOptions.Default));
    }
}
