using System.Reflection;
using System.Text;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;

namespace Image2Gdram.Core.Tests.Presets;

public sealed class PresetCatalogTests
{
    private static readonly ExpectedPreset[] Expected =
    {
        new("WG240128A", "RA6963 / T6963C", 240, 128, PackDirection.Horizontal, BitOrder.MsbFirst, ColorScheme.Lcd, 3840),
        new("W0240128", "UC1608", 240, 128, PackDirection.Vertical, BitOrder.LsbFirst, ColorScheme.Lcd, 3840),
        new("RG12864F", "NT7108 (\u0441\u043e\u0432\u043c\u0435\u0441\u0442\u0438\u043c \u0441 KS0108)", 128, 64, PackDirection.Vertical, BitOrder.LsbFirst, ColorScheme.Lcd, 1024),
        new("RET012864DGPP3N", "SSD1305", 128, 64, PackDirection.Vertical, BitOrder.LsbFirst, ColorScheme.Oled, 1024),
        new("OLED128X64-0.96", "SSD1306", 128, 64, PackDirection.Vertical, BitOrder.LsbFirst, ColorScheme.Oled, 1024),
        new("HT1.3-OLED-BW / HR0161", "SH1106", 128, 64, PackDirection.Vertical, BitOrder.LsbFirst, ColorScheme.Oled, 1024),
    };

    [Fact]
    public void Built_in_presets_match_d01()
    {
        var packer = new Mono1bppPacker();
        Assert.Equal(Expected.Length, PresetCatalog.Shared.BuiltIn.Count);
        for (int i = 0; i < Expected.Length; i++)
        {
            Preset preset = PresetCatalog.Shared.BuiltIn[i];
            ExpectedPreset expected = Expected[i];
            Assert.Equal(expected.Name, preset.Name);
            Assert.Equal(expected.Controller, preset.Controller);
            Assert.Equal(expected.Width, preset.Width);
            Assert.Equal(expected.Height, preset.Height);
            Assert.Equal(expected.Direction, preset.Packing.Direction);
            Assert.Equal(expected.BitOrder, preset.Packing.BitOrder);
            Assert.Equal(8, preset.Packing.BitsPerByte);
            Assert.Equal(PageTraversal.ByPages, preset.Packing.PageTraversal);
            Assert.False(preset.Packing.Invert);
            Assert.Equal(PixelFormat.Mono1bpp, preset.Packing.PixelFormat);
            Assert.Equal(ByteOrder.BigEndian, preset.Packing.ByteOrder);
            Assert.Equal(expected.Scheme, preset.ColorScheme);
            Assert.True(preset.IsBuiltIn);
            Assert.Equal(expected.Bytes, packer.GetSize(preset.Width, preset.Height, preset.Packing));
        }
    }

    [Fact]
    public void Find_is_case_insensitive_and_custom_is_not_a_preset()
    {
        Assert.Equal("OLED128X64-0.96", PresetCatalog.Shared.Find("oled128x64-0.96")!.Name);
        Assert.Null(PresetCatalog.Shared.Find(PresetCatalog.CustomName));
        Assert.Null(PresetCatalog.Shared.Find("  "));
        Assert.True(PresetCatalog.Shared.IsReservedName("\u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0441\u043a\u0438\u0439"));
        Assert.False(PresetCatalog.Shared.IsBuiltInName(PresetCatalog.CustomName));
    }

    [Fact]
    public void Catalog_comes_from_the_embedded_json()
    {
        using Stream stream = typeof(PresetCatalog).Assembly.GetManifestResourceStream(PresetCatalog.ResourceName)!;
        using var reader = new StreamReader(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true));
        string json = reader.ReadToEnd();
        Assert.Contains("\"name\": \"WG240128A\"", json, StringComparison.Ordinal);
        Assert.Contains("\u0441\u043e\u0432\u043c\u0435\u0441\u0442\u0438\u043c \u0441 KS0108", json, StringComparison.Ordinal);
        Assert.DoesNotContain(PresetCatalog.CustomName, json, StringComparison.Ordinal);
        Assert.Equal(PresetCatalog.ResourceName, typeof(PresetCatalog).Assembly.GetManifestResourceNames().Single(name => name.EndsWith("presets.json", StringComparison.Ordinal)));
    }

    [Fact]
    public void Named_preset_info_includes_the_controller()
    {
        Preset preset = PresetCatalog.Shared.Find("OLED128X64-0.96")!;
        PresetInfo info = PresetBinding.ForPreset(preset).ToInfo(PresetCatalog.Shared.Find);
        Assert.Equal("OLED128X64-0.96", info.Name);
        Assert.Equal("SSD1306", info.Controller);
        Assert.False(info.IsCustom);
    }

    [Fact]
    public void Image_preset_sets_size_packing_and_scheme_and_custom_does_not()
    {
        Preset preset = PresetCatalog.Shared.Find("WG240128A")!;
        var current = ImageTabParameters.CreateDefault() with
        {
            Processing = ProcessingOptions.Default with { Threshold = 200, Rotation = Rotation.Rotate90 },
        };

        ImageTabParameters applied = PresetApplication.Apply(current, preset);

        Assert.Equal(240, applied.Processing.TargetWidth);
        Assert.Equal(128, applied.Processing.TargetHeight);
        Assert.Equal(TargetSizeMode.Manual, applied.Processing.SizeMode);
        Assert.Equal(200, applied.Processing.Threshold);
        Assert.Equal(Rotation.Rotate90, applied.Processing.Rotation);
        Assert.Equal(preset.Packing, applied.Packing);
        Assert.Equal(ColorScheme.Lcd, applied.ColorScheme);
        Assert.Equal("WG240128A", applied.Preset.Name);
        Assert.Equal(preset.Packing, PresetCatalog.Shared.Find("WG240128A")!.Packing);

        ImageTabParameters edited = applied with
        {
            Preset = applied.Preset.Edited(),
            Packing = applied.Packing with { Invert = true },
        };
        Assert.Null(edited.Preset.Name);
        Assert.Equal("WG240128A", edited.Preset.BasedOn);
        PresetInfo info = edited.Preset.ToInfo();
        Assert.True(info.IsCustom);
        Assert.Equal("WG240128A", info.BasedOn);
        Assert.Equal(edited.Preset, edited.Preset.Edited());

        ImageTabParameters custom = PresetApplication.Apply(edited, null);
        Assert.Equal(edited.Packing, custom.Packing);
        Assert.Equal(edited.Processing, custom.Processing);
        Assert.Equal(edited.ColorScheme, custom.ColorScheme);
        Assert.Equal(PresetBinding.Custom, custom.Preset);
    }

    [Fact]
    public void Font_preset_does_not_change_the_cell()
    {
        var current = FontTabParameters.CreateDefault() with { CellWidth = 12, CellHeight = 16, PreviewText = "abc" };

        FontTabParameters applied = PresetApplication.Apply(current, PresetCatalog.Shared.Find("OLED128X64-0.96"));

        Assert.Equal(12, applied.CellWidth);
        Assert.Equal(16, applied.CellHeight);
        Assert.Equal("abc", applied.PreviewText);
        Assert.Equal(PackDirection.Vertical, applied.Packing.Direction);
        Assert.Equal(BitOrder.LsbFirst, applied.Packing.BitOrder);
        Assert.Equal(ColorScheme.Oled, applied.ColorScheme);
        Assert.Equal("OLED128X64-0.96", applied.Preset.Name);

        FontTabParameters custom = PresetApplication.Apply(applied, null);
        Assert.Equal(applied.Packing, custom.Packing);
        Assert.Equal(12, custom.CellWidth);
        Assert.Equal(PresetBinding.Custom, custom.Preset);
    }

    private readonly record struct ExpectedPreset(
        string Name,
        string Controller,
        int Width,
        int Height,
        PackDirection Direction,
        BitOrder BitOrder,
        ColorScheme Scheme,
        int Bytes);
}
