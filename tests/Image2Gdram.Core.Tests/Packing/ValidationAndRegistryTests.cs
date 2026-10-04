using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Packing;

public class ValidationAndRegistryTests
{
    private readonly Mono1bppPacker _packer = new();

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(7)]
    [InlineData(16)]
    public void Bits_per_byte_other_than_8_or_6_are_rejected(int bits)
    {
        var options = new PackingOptions { Direction = PackDirection.Horizontal, BitsPerByte = bits };

        Assert.Throws<ArgumentException>(() => _packer.GetSize(8, 8, options));
        Assert.Throws<ArgumentException>(() => _packer.Pack(new MonoBitmap(8, 8), options));
    }

    [Fact]
    public void Undefined_enum_values_are_rejected()
    {
        var options = new PackingOptions { Direction = (PackDirection)42 };

        Assert.Throws<ArgumentException>(() => _packer.GetSize(8, 8, options));
    }

    [Fact]
    public void Color_format_is_rejected_by_monochrome_packer()
    {
        var options = new PackingOptions { PixelFormat = PixelFormat.Rgb565 };

        Assert.Throws<ArgumentException>(() => _packer.Pack(new MonoBitmap(8, 8), options));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 0)]
    [InlineData(-5, 8)]
    public void Non_positive_sizes_are_rejected(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _packer.GetSize(width, height, PackingOptions.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => new MonoBitmap(width, height));
    }

    [Fact]
    public void Default_registry_contains_only_monochrome_packer()
    {
        PackerRegistry registry = PackerRegistry.CreateDefault();

        Assert.Equal(new[] { PixelFormat.Mono1bpp }, registry.Formats);
        Assert.IsType<Mono1bppPacker>(registry.Get(PixelFormat.Mono1bpp));
        Assert.IsType<Mono1bppPacker>(registry.Get(PackingOptions.Default));
    }

    [Fact]
    public void Unregistered_format_gives_clear_error()
    {
        PackerRegistry registry = PackerRegistry.CreateDefault();

        var error = Assert.Throws<PackerNotRegisteredException>(() => registry.Get(PixelFormat.Rgb565));
        Assert.Equal(PixelFormat.Rgb565, error.Format);
        Assert.Contains("Rgb565", error.Message);
        Assert.False(registry.TryGet(PixelFormat.Rgb565, out _));
    }

    [Fact]
    public void New_packer_is_added_by_registration_only()
    {
        PackerRegistry registry = PackerRegistry.CreateDefault();
        var colorPacker = new FakeRgb565Packer();

        registry.Register(colorPacker);

        Assert.Same(colorPacker, registry.Get(new PackingOptions { PixelFormat = PixelFormat.Rgb565 }));
        Assert.IsType<Mono1bppPacker>(registry.Get(PixelFormat.Mono1bpp));
        Assert.Equal(new[] { PixelFormat.Mono1bpp, PixelFormat.Rgb565 }, registry.Formats);
    }

    [Fact]
    public void Duplicate_registration_is_rejected()
    {
        PackerRegistry registry = PackerRegistry.CreateDefault();

        Assert.Throws<InvalidOperationException>(() => registry.Register(new Mono1bppPacker()));
    }

    /// <summary>Заглушка цветного упаковщика: проверяет только механизм регистрации (п. 4.6 ТЗ).</summary>
    private sealed class FakeRgb565Packer : IPacker
    {
        public PixelFormat Format => PixelFormat.Rgb565;

        public int GetSize(int width, int height, PackingOptions options) => width * height * 2;

        public byte[] Pack(MonoBitmap bitmap, PackingOptions options) => new byte[GetSize(bitmap.Width, bitmap.Height, options)];

        public void Pack(MonoBitmap bitmap, PackingOptions options, Span<byte> destination) => destination.Clear();

        public MonoBitmap Unpack(ReadOnlySpan<byte> data, int width, int height, PackingOptions options) => new(width, height);

        public BitLocation Locate(int x, int y, int width, int height, PackingOptions options) => new((y * width + x) * 2, 0);
    }
}
