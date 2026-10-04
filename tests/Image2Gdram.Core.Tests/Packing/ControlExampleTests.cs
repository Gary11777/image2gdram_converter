using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Packing;

/// <summary>Контрольные примеры F-05: один активный пиксель (0, 0) на фоне.</summary>
public class ControlExampleTests
{
    private readonly Mono1bppPacker _packer = new();

    [Theory]
    [InlineData(PackDirection.Vertical, BitOrder.LsbFirst, 8, false, 0x01)]
    [InlineData(PackDirection.Vertical, BitOrder.LsbFirst, 8, true, 0xFE)]
    [InlineData(PackDirection.Vertical, BitOrder.MsbFirst, 8, false, 0x80)]
    [InlineData(PackDirection.Vertical, BitOrder.MsbFirst, 8, true, 0x7F)]
    [InlineData(PackDirection.Horizontal, BitOrder.MsbFirst, 8, false, 0x80)]
    [InlineData(PackDirection.Horizontal, BitOrder.MsbFirst, 8, true, 0x7F)]
    [InlineData(PackDirection.Horizontal, BitOrder.MsbFirst, 6, false, 0x20)]
    [InlineData(PackDirection.Horizontal, BitOrder.MsbFirst, 6, true, 0xDF)]
    [InlineData(PackDirection.Horizontal, BitOrder.LsbFirst, 8, false, 0x01)]
    [InlineData(PackDirection.Horizontal, BitOrder.LsbFirst, 8, true, 0xFE)]
    [InlineData(PackDirection.Horizontal, BitOrder.LsbFirst, 6, false, 0x01)]
    [InlineData(PackDirection.Horizontal, BitOrder.LsbFirst, 6, true, 0xFE)]
    public void Single_pixel_at_origin_gives_expected_first_byte(
        PackDirection direction, BitOrder bitOrder, int bitsPerByte, bool invert, int expected)
    {
        var options = new PackingOptions
        {
            Direction = direction,
            BitOrder = bitOrder,
            BitsPerByte = bitsPerByte,
            Invert = invert,
        };

        byte[] bytes = _packer.Pack(TestBitmaps.SinglePixel(16, 16, 0, 0), options);

        Assert.Equal(expected, bytes[0]);
        byte background = invert ? (byte)0xFF : (byte)0x00;
        Assert.All(bytes.Skip(1), b => Assert.Equal(background, b));
    }

    [Theory]
    [InlineData(BitOrder.LsbFirst, 0x80)]
    [InlineData(BitOrder.MsbFirst, 0x01)]
    public void Last_pixel_of_vertical_page_is_opposite_bit(BitOrder bitOrder, int expected)
    {
        var options = new PackingOptions { Direction = PackDirection.Vertical, BitOrder = bitOrder };

        byte[] bytes = _packer.Pack(TestBitmaps.SinglePixel(4, 8, 0, 7), options);

        Assert.Equal(new[] { (byte)expected, (byte)0, (byte)0, (byte)0 }, bytes);
    }

    [Fact]
    public void Diagonal_in_horizontal_msb_first_gives_walking_bit()
    {
        // Приложение Б, п. 4: диагональ под 45° в группе из 8 пикселей не должна превращаться в зигзаг.
        var bitmap = new MonoBitmap(8, 8);
        for (int i = 0; i < 8; i++)
        {
            bitmap[i, i] = true;
        }

        byte[] bytes = _packer.Pack(bitmap, new PackingOptions { Direction = PackDirection.Horizontal, BitOrder = BitOrder.MsbFirst });

        Assert.Equal(new byte[] { 0x80, 0x40, 0x20, 0x10, 0x08, 0x04, 0x02, 0x01 }, bytes);
    }

    [Fact]
    public void Glyph_A_6x8_vertical_lsb_first_matches_appendix_V2()
    {
        // Символ 0xC0 'А' из примера В.2: DB 07Ch, 012h, 011h, 012h, 07Ch, 000h.
        MonoBitmap glyph = TestBitmaps.FromRows(
            "..#...",
            ".#.#..",
            "#...#.",
            "#...#.",
            "#####.",
            "#...#.",
            "#...#.",
            "......");

        byte[] bytes = _packer.Pack(glyph, PackingOptions.Default);

        Assert.Equal(new byte[] { 0x7C, 0x12, 0x11, 0x12, 0x7C, 0x00 }, bytes);
    }
}
