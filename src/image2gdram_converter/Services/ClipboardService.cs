using System.Windows;
using Image2Gdram.Core.Packing;

namespace image2gdram_converter.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text) => Clipboard.SetText(text);

    public void SetGlyph(MonoBitmap glyph, bool invert)
    {
        var data = new DataObject();
        data.SetData(GlyphClipboard.Format, GlyphClipboard.ToPayload(glyph));
        data.SetText(GlyphClipboard.ToText(glyph, invert));
        Clipboard.SetDataObject(data);
    }

    public bool TryGetGlyph(bool invert, out MonoBitmap? glyph)
    {
        glyph = null;
        try
        {
            if (Clipboard.ContainsData(GlyphClipboard.Format)
                && Clipboard.GetData(GlyphClipboard.Format) is string payload
                && GlyphClipboard.TryParsePayload(payload, out glyph))
            {
                return true;
            }

            if (Clipboard.ContainsText())
            {
                return GlyphClipboard.TryParseText(Clipboard.GetText(), invert, out glyph);
            }
        }
        catch (System.Runtime.InteropServices.COMException)
        {
            return false;
        }

        return false;
    }
}
