using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Presets;

namespace Image2Gdram.Core.Tests.Presets;

public sealed class UserPresetStoreTests
{
    private readonly UserPresetStore _store = new(PresetCatalog.Shared);

    [Fact]
    public void Add_rename_and_remove_keep_insertion_order()
    {
        _store.Add(User("Panel", width: 10));
        _store.Add(User("Other"));
        _store.Rename("panel", "Panel 2");

        Assert.Equal(new[] { "Panel 2", "Other" }, _store.Presets.Select(preset => preset.Name));
        Assert.Equal(10, _store.Find("PANEL 2")!.Width);
        Assert.Equal("demo", _store.Find("panel 2")!.Controller);
        Assert.False(_store.Find("panel 2")!.IsBuiltIn);

        _store.Remove("OTHER");
        Assert.Single(_store.Presets);
        Assert.Equal("Panel 2", _store.Presets[0].Name);
    }

    [Fact]
    public void Case_only_rename_is_allowed()
    {
        _store.Add(User("Panel"));
        _store.Rename("Panel", "panel");
        Assert.Equal("panel", _store.Presets[0].Name);
    }

    [Fact]
    public void Stored_name_is_trimmed_and_controller_may_be_empty()
    {
        _store.Add(new Preset("  Panel  ", "  ", 8, 8, PackingOptions.Default, ColorScheme.Oled, isBuiltIn: false));
        Assert.Equal("Panel", _store.Presets[0].Name);
        Assert.Null(_store.Presets[0].Controller);
    }

    [Theory]
    [InlineData("WG240128A", PresetError.BuiltIn)]
    [InlineData("oled128x64-0.96", PresetError.BuiltIn)]
    [InlineData("\u041f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0441\u043a\u0438\u0439", PresetError.ReservedName)]
    [InlineData("  \u043f\u043e\u043b\u044c\u0437\u043e\u0432\u0430\u0442\u0435\u043b\u044c\u0441\u043a\u0438\u0439  ", PresetError.ReservedName)]
    public void Reserved_names_cannot_be_added(string name, PresetError error)
    {
        PresetException exception = Assert.Throws<PresetException>(() => _store.Add(User(name)));
        Assert.Equal(error, exception.Error);
        Assert.Empty(_store.Presets);
    }

    [Fact]
    public void Duplicate_name_does_not_overwrite()
    {
        _store.Add(User("Panel", width: 10));
        PresetException exception = Assert.Throws<PresetException>(() => _store.Add(User("PANEL", width: 20)));
        Assert.Equal(PresetError.DuplicateName, exception.Error);
        Assert.Equal(10, _store.Find("panel")!.Width);
    }

    [Fact]
    public void Built_in_presets_cannot_be_renamed_or_removed()
    {
        _store.Add(User("Panel"));
        Assert.Equal(PresetError.BuiltIn, Assert.Throws<PresetException>(() => _store.Rename("WG240128A", "X")).Error);
        Assert.Equal(PresetError.BuiltIn, Assert.Throws<PresetException>(() => _store.Remove("RG12864F")).Error);
        Assert.Equal(PresetError.BuiltIn, Assert.Throws<PresetException>(() => _store.Rename("Panel", "rg12864f")).Error);
        Assert.Equal(PresetError.ReservedName, Assert.Throws<PresetException>(() => _store.Rename("Panel", PresetCatalog.CustomName)).Error);
        Assert.Equal(PresetError.NotFound, Assert.Throws<PresetException>(() => _store.Remove("missing")).Error);
        Assert.Equal("Panel", _store.Presets[0].Name);
        Assert.Equal(6, PresetCatalog.Shared.BuiltIn.Count);
    }

    [Fact]
    public void Built_in_instance_cannot_be_stored()
    {
        PresetException exception = Assert.Throws<PresetException>(() => _store.Add(PresetCatalog.Shared.BuiltIn[0]));
        Assert.Equal(PresetError.BuiltIn, exception.Error);
    }

    [Fact]
    public void Size_name_and_packing_are_checked()
    {
        Assert.Equal(PresetError.EmptyName, Assert.Throws<PresetException>(() => User("  ")).Error);
        Assert.Equal(PresetError.InvalidName, Assert.Throws<PresetException>(() => User(new string('a', 81))).Error);
        Assert.Equal(PresetError.InvalidName, Assert.Throws<PresetException>(() => User("bad\nname")).Error);
        _store.Add(User(new string('a', 80), width: 1024, height: 1));
        Assert.Equal(1024, _store.Presets[0].Width);

        Assert.Equal(PresetError.InvalidSize, Assert.Throws<PresetException>(() => User("wide", width: 0)).Error);
        Assert.Equal(PresetError.InvalidSize, Assert.Throws<PresetException>(() => User("wide", height: 1025)).Error);
        Assert.Equal(PresetError.InvalidPacking, Assert.Throws<PresetException>(() => User("bits", bits: 7)).Error);
        Assert.Equal(
            PresetError.InvalidController,
            Assert.Throws<PresetException>(() => new Preset("ok", new string('c', 81), 8, 8, PackingOptions.Default, ColorScheme.Lcd, false)).Error);
    }

    private static Preset User(string name, int width = 16, int height = 16, int bits = 8) =>
        new(name, "demo", width, height, PackingOptions.Default with { BitsPerByte = bits }, ColorScheme.Lcd, isBuiltIn: false);
}
