using Image2Gdram.Core.Packing;

namespace image2gdram_converter.Services;

public interface IClipboardService
{
    void SetText(string text);

    void SetGlyph(MonoBitmap glyph, bool invert);

    bool TryGetGlyph(bool invert, out MonoBitmap? glyph);
}
