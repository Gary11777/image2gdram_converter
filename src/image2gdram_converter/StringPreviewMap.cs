using Image2Gdram.Core.Text;

namespace image2gdram_converter;

/// <summary>Один знак строки предпросмотра: код CP1251 или ячейка вне кодировки (решение N-30).</summary>
public readonly record struct PreviewGlyph(int? Code, bool OutsideEncoding);

/// <summary>Перевод строки предпросмотра в коды CP1251. Каждый символ — отдельная ячейка.</summary>
public static class StringPreviewMap
{
    public static IReadOnlyList<PreviewGlyph> Map(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<PreviewGlyph>();
        }

        var list = new List<PreviewGlyph>(text.Length);
        foreach (char c in text)
        {
            if (Cp1251.TryFromDecoded(c, out byte code))
            {
                list.Add(new PreviewGlyph(code, false));
            }
            else
            {
                list.Add(new PreviewGlyph(null, true));
            }
        }

        return list;
    }
}
