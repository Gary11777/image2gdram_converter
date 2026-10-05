using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;

namespace Image2Gdram.Core.Tests.Output;

internal static class OutputTestData
{
    public const string Crlf = "\r\n";

    public static readonly PresetInfo Ssd1306 = PresetInfo.Named("OLED128X64-0.96", "SSD1306");

    public static readonly OutputFormat[] AllFormats =
    {
        OutputFormat.CKeilC51, OutputFormat.CStm32, OutputFormat.A51Module, OutputFormat.A51Include, OutputFormat.Bin,
    };

    public static readonly OutputFormat[] TextFormats =
    {
        OutputFormat.CKeilC51, OutputFormat.CStm32, OutputFormat.A51Module, OutputFormat.A51Include,
    };

    public static OutputGeneratorRegistry Registry { get; } = OutputGeneratorRegistry.CreateDefault();

    /// <summary>Детерминированные «случайные» байты: зависят только от длины и seed.</summary>
    public static byte[] Bytes(int length, int seed = 1)
    {
        var random = new Random(seed * 7919 + length);
        var bytes = new byte[length];
        random.NextBytes(bytes);
        return bytes;
    }

    /// <summary>Изображение 128?64 из примера В.4: первая строка — 0xFF и пятнадцать 0x01.</summary>
    public static ImageOutputData Logo128x64()
    {
        var bytes = new byte[1024];
        bytes[0] = 0xFF;
        for (int i = 1; i < 16; i++)
        {
            bytes[i] = 0x01;
        }

        return new ImageOutputData(128, 64, bytes, PackingOptions.Default, new ImageSourceInfo("logo.png"), Ssd1306);
    }

    public static ImageOutputData Image(int width, int height, PackingOptions? packing = null, int seed = 1)
    {
        PackingOptions options = packing ?? PackingOptions.Default;
        int size = new Mono1bppPacker().GetSize(width, height, options);
        return new ImageOutputData(width, height, Bytes(size, seed), options, new ImageSourceInfo("image.png"), Ssd1306);
    }

    public static ImageOutputData Frames(int count, int width = 16, int height = 16)
    {
        int size = new Mono1bppPacker().GetSize(width, height, PackingOptions.Default);
        var frames = Enumerable.Range(0, count).Select(f => Bytes(size, f + 10)).ToArray();
        return new ImageOutputData(width, height, frames, allFrames: true, PackingOptions.Default, new ImageSourceInfo("anim.gif", count), Ssd1306);
    }

    public static FontOutputData Font(int width, int height, PackingOptions? packing = null, byte[]? data = null, FontSourceInfo? source = null)
    {
        PackingOptions options = packing ?? PackingOptions.Default;
        int perChar = new Mono1bppPacker().GetSize(width, height, options);
        return new FontOutputData(
            width,
            height,
            data ?? Bytes(perChar * FontOutputData.CharCount, width * 100 + height),
            options,
            source ?? FontSourceInfo.Manual,
            Ssd1306);
    }

    public static OutputOptions Options(OutputFormat format, string name = "test_array") =>
        new() { Format = format, ArrayName = name, IncludeDate = false };

    public static OutputDocument Generate(OutputData data, OutputFormat format, string name = "test_array") =>
        Registry.Generate(data, Options(format, name));

    public static string Join(params string[] lines) => string.Join(Crlf, lines) + Crlf;
}
