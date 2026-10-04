using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Packing;

public class SizeTests
{
    private readonly Mono1bppPacker _packer = new();

    private static PackingOptions Vertical => new() { Direction = PackDirection.Vertical };

    private static PackingOptions Horizontal(int bits) => new() { Direction = PackDirection.Horizontal, BitsPerByte = bits };

    // Таблица п. 4.2.1 ТЗ: байт на символ.
    [Theory]
    [InlineData(6, 8, 6, 8, 8)]
    [InlineData(8, 8, 8, 8, 16)]
    [InlineData(12, 16, 24, 32, 32)]
    public void Bytes_per_char_match_table_4_2_1(int width, int height, int vertical, int horizontal8, int horizontal6)
    {
        Assert.Equal(vertical, _packer.GetSize(width, height, Vertical));
        Assert.Equal(horizontal8, _packer.GetSize(width, height, Horizontal(8)));
        Assert.Equal(horizontal6, _packer.GetSize(width, height, Horizontal(6)));
    }

    [Theory]
    [InlineData(6, 8, 1536)]
    [InlineData(12, 16, 6144)]
    public void Font_array_size_is_256_times_bytes_per_char(int width, int height, int expected)
    {
        Assert.Equal(expected, 256 * _packer.GetSize(width, height, Vertical));
    }

    [Fact]
    public void Row_of_240_pixels_takes_40_bytes_with_6_bits_and_30_with_8()
    {
        Assert.Equal(40, _packer.GetSize(240, 1, Horizontal(6)));
        Assert.Equal(30, _packer.GetSize(240, 1, Horizontal(8)));
        Assert.Equal(40, _packer.Pack(new MonoBitmap(240, 1), Horizontal(6)).Length);
    }

    // Размеры буферов приложения А.
    [Theory]
    [InlineData(240, 128, PackDirection.Horizontal, 3840)]
    [InlineData(240, 128, PackDirection.Vertical, 3840)]
    [InlineData(128, 64, PackDirection.Vertical, 1024)]
    public void Preset_screen_buffers_have_expected_size(int width, int height, PackDirection direction, int expected)
    {
        var options = new PackingOptions { Direction = direction };

        Assert.Equal(expected, _packer.GetSize(width, height, options));
        Assert.Equal(expected, _packer.Pack(new MonoBitmap(width, height), options).Length);
    }

    [Fact]
    public void Sprite_13x11_sizes_round_up_on_both_axes()
    {
        Assert.Equal(11 * 2, _packer.GetSize(13, 11, Horizontal(8)));
        Assert.Equal(11 * 3, _packer.GetSize(13, 11, Horizontal(6)));
        Assert.Equal(13 * 2, _packer.GetSize(13, 11, Vertical));
        Assert.Equal(13 * 2, _packer.GetSize(13, 11, Vertical with { PageTraversal = PageTraversal.ByColumns }));
    }

    [Fact]
    public void Max_size_1024x1024_is_131072_bytes()
    {
        Assert.Equal(131072, _packer.GetSize(1024, 1024, Vertical));
        Assert.Equal(131072, _packer.GetSize(1024, 1024, Horizontal(8)));
    }

    [Theory]
    [InlineData(PackDirection.Horizontal)]
    [InlineData(PackDirection.Vertical)]
    public void Inversion_and_bit_order_do_not_change_size(PackDirection direction)
    {
        var plain = new PackingOptions { Direction = direction };
        var changed = plain with { Invert = true, BitOrder = BitOrder.MsbFirst, PageTraversal = PageTraversal.ByColumns };

        Assert.Equal(_packer.GetSize(17, 23, plain), _packer.GetSize(17, 23, changed));
    }

    [Fact]
    public void Bits_per_byte_is_ignored_in_vertical_mode()
    {
        Assert.Equal(_packer.GetSize(12, 16, Vertical), _packer.GetSize(12, 16, Vertical with { BitsPerByte = 6 }));
    }
}
