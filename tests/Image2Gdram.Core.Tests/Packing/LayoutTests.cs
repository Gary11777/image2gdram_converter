using Image2Gdram.Core.Packing;
using Image2Gdram.Reference;

namespace Image2Gdram.Core.Tests.Packing;

/// <summary>Порядок обхода, биты дополнения, инверсия, раскладка шрифта.</summary>
public class LayoutTests
{
    private readonly Mono1bppPacker _packer = new();

    // Пример п. 4.3.4 ТЗ для символа 12×16.
    [Fact]
    public void Traversal_by_pages_for_12x16_puts_rows_8_to_15_into_bytes_12_to_23()
    {
        var options = new PackingOptions { Direction = PackDirection.Vertical, PageTraversal = PageTraversal.ByPages };

        for (int x = 0; x < 12; x++)
        {
            Assert.Equal(new BitLocation(x, 0), _packer.Locate(x, 0, 12, 16, options));
            Assert.Equal(new BitLocation(x, 7), _packer.Locate(x, 7, 12, 16, options));
            Assert.Equal(new BitLocation(12 + x, 0), _packer.Locate(x, 8, 12, 16, options));
            Assert.Equal(new BitLocation(12 + x, 7), _packer.Locate(x, 15, 12, 16, options));
        }
    }

    [Fact]
    public void Traversal_by_columns_for_12x16_puts_each_column_into_two_adjacent_bytes()
    {
        var options = new PackingOptions { Direction = PackDirection.Vertical, PageTraversal = PageTraversal.ByColumns };

        for (int x = 0; x < 12; x++)
        {
            Assert.Equal(2 * x, _packer.Locate(x, 3, 12, 16, options).ByteIndex);
            Assert.Equal(2 * x + 1, _packer.Locate(x, 12, 12, 16, options).ByteIndex);
        }

        // Столбец 0 целиком активен: байты 0 и 1 — 0xFF, остальные — фон.
        var column = new MonoBitmap(12, 16);
        for (int y = 0; y < 16; y++)
        {
            column[0, y] = true;
        }

        byte[] bytes = _packer.Pack(column, options);
        Assert.Equal(new byte[] { 0xFF, 0xFF }, bytes[..2]);
        Assert.All(bytes[2..], b => Assert.Equal(0, b));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(8)]
    public void Traversal_orders_coincide_when_height_is_at_most_8(int height)
    {
        MonoBitmap bitmap = TestBitmaps.Random(29, height, TestBitmaps.SeedFor(29, height));
        var byPages = new PackingOptions { Direction = PackDirection.Vertical, PageTraversal = PageTraversal.ByPages };
        var byColumns = byPages with { PageTraversal = PageTraversal.ByColumns };

        Assert.Equal(_packer.Pack(bitmap, byPages), _packer.Pack(bitmap, byColumns));
    }

    // Спрайт 13×11 полностью активен: биты дополнения справа (горизонтально) и снизу (вертикально).
    [Theory]
    [InlineData(BitOrder.MsbFirst, false, 0xF8)]
    [InlineData(BitOrder.LsbFirst, false, 0x1F)]
    [InlineData(BitOrder.MsbFirst, true, 0x07)]
    [InlineData(BitOrder.LsbFirst, true, 0xE0)]
    public void Horizontal_padding_bits_of_13_pixel_row_are_background(BitOrder bitOrder, bool invert, int lastByte)
    {
        var options = new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = bitOrder, Invert = invert };

        byte[] bytes = _packer.Pack(TestBitmaps.Filled(13, 11, true), options);

        byte full = invert ? (byte)0x00 : (byte)0xFF;
        for (int y = 0; y < 11; y++)
        {
            Assert.Equal(full, bytes[2 * y]);
            Assert.Equal(lastByte, bytes[2 * y + 1]);
        }
    }

    [Theory]
    [InlineData(BitOrder.LsbFirst, false, 0x07)]
    [InlineData(BitOrder.MsbFirst, false, 0xE0)]
    [InlineData(BitOrder.LsbFirst, true, 0xF8)]
    [InlineData(BitOrder.MsbFirst, true, 0x1F)]
    public void Vertical_padding_bits_of_last_page_are_background(BitOrder bitOrder, bool invert, int lastPageByte)
    {
        var options = new PackingOptions { Direction = PackDirection.Vertical, BitOrder = bitOrder, Invert = invert };

        byte[] bytes = _packer.Pack(TestBitmaps.Filled(13, 11, true), options);

        byte full = invert ? (byte)0x00 : (byte)0xFF;
        Assert.All(bytes[..13], b => Assert.Equal(full, b));
        Assert.All(bytes[13..], b => Assert.Equal(lastPageByte, b));
    }

    [Theory]
    [InlineData(BitOrder.MsbFirst, false, 0x3F)]
    [InlineData(BitOrder.LsbFirst, false, 0x3F)]
    [InlineData(BitOrder.MsbFirst, true, 0xC0)]
    [InlineData(BitOrder.LsbFirst, true, 0xC0)]
    public void Bits_7_and_6_are_padding_in_6_bit_mode(BitOrder bitOrder, bool invert, int expected)
    {
        var options = new PackingOptions
        {
            Direction = PackDirection.Horizontal,
            BitsPerByte = 6,
            BitOrder = bitOrder,
            Invert = invert,
        };

        byte[] bytes = _packer.Pack(TestBitmaps.Filled(12, 2, true), options);

        Assert.All(bytes, b => Assert.Equal(expected, b));
    }

    [Theory]
    [MemberData(nameof(TestBitmaps.SizeAndCombination), MemberType = typeof(TestBitmaps))]
    public void Empty_bitmap_packs_to_background_bytes(int width, int height, int combination)
    {
        PackingOptions options = TestBitmaps.ToCore(RefOptions.AllCombinations[combination]);

        byte[] bytes = _packer.Pack(new MonoBitmap(width, height), options);

        byte background = options.Invert ? (byte)0xFF : (byte)0x00;
        Assert.All(bytes, b => Assert.Equal(background, b));
    }

    [Theory]
    [MemberData(nameof(TestBitmaps.SizeAndCombination), MemberType = typeof(TestBitmaps))]
    public void Inversion_flips_every_byte(int width, int height, int combination)
    {
        PackingOptions options = TestBitmaps.ToCore(RefOptions.AllCombinations[combination]) with { Invert = false };
        MonoBitmap bitmap = TestBitmaps.Random(width, height, TestBitmaps.SeedFor(width, height));

        byte[] plain = _packer.Pack(bitmap, options);
        byte[] inverted = _packer.Pack(bitmap, options with { Invert = true });

        Assert.Equal(plain.Select(b => (byte)(b ^ 0xFF)), inverted);
    }

    // F-04: символ с кодом c занимает байты c·N … c·N + N − 1; пустой символ — фон.
    [Theory]
    [InlineData(6, 8, false)]
    [InlineData(12, 16, true)]
    public void Font_table_layout_indexes_glyph_by_code(int cellWidth, int cellHeight, bool invert)
    {
        var options = new PackingOptions { Direction = PackDirection.Vertical, Invert = invert };
        int bytesPerChar = _packer.GetSize(cellWidth, cellHeight, options);
        var table = new byte[256 * bytesPerChar];
        var glyphs = new MonoBitmap[256];
        for (int code = 0; code < 256; code++)
        {
            glyphs[code] = code % 3 == 0
                ? new MonoBitmap(cellWidth, cellHeight)
                : TestBitmaps.Random(cellWidth, cellHeight, code);
            _packer.Pack(glyphs[code], options, table.AsSpan(code * bytesPerChar, bytesPerChar));
        }

        byte background = invert ? (byte)0xFF : (byte)0x00;
        for (int code = 0; code < 256; code++)
        {
            byte[] slice = table[(code * bytesPerChar)..((code + 1) * bytesPerChar)];
            Assert.Equal(_packer.Pack(glyphs[code], options), slice);
            if (code % 3 == 0)
            {
                Assert.All(slice, b => Assert.Equal(background, b));
            }
        }
    }

    [Fact]
    public void Byte_order_does_not_affect_monochrome_output()
    {
        MonoBitmap bitmap = TestBitmaps.Random(37, 19, 42);
        var bigEndian = new PackingOptions { ByteOrder = ByteOrder.BigEndian };

        Assert.Equal(
            _packer.Pack(bitmap, bigEndian),
            _packer.Pack(bitmap, bigEndian with { ByteOrder = ByteOrder.LittleEndian }));
    }

    [Fact]
    public void Repeated_packing_is_byte_identical()
    {
        MonoBitmap bitmap = TestBitmaps.Random(240, 128, 7);
        var options = new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst };

        Assert.Equal(_packer.Pack(bitmap, options), _packer.Pack(bitmap.Clone(), options));
    }

    [Fact]
    public void Default_options_are_8_bits_by_pages_without_inversion()
    {
        PackingOptions options = PackingOptions.Default;

        Assert.Equal(PixelFormat.Mono1bpp, options.PixelFormat);
        Assert.Equal(PackDirection.Vertical, options.Direction);
        Assert.Equal(BitOrder.LsbFirst, options.BitOrder);
        Assert.Equal(8, options.BitsPerByte);
        Assert.Equal(PageTraversal.ByPages, options.PageTraversal);
        Assert.False(options.Invert);
    }
}
