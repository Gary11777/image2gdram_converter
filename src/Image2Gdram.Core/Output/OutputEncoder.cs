using System.Text;
using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Output;

/// <summary>Кодирование текста файлов: CP1251 или UTF-8 без BOM (п. 4.4.8 ТЗ).</summary>
public static class OutputEncoder
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    public static byte[] Encode(string text, OutputEncoding encoding)
    {
        ArgumentNullException.ThrowIfNull(text);
        return encoding switch
        {
            OutputEncoding.Cp1251 => Cp1251.GetBytes(text),
            OutputEncoding.Utf8NoBom => Utf8NoBom.GetBytes(text),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unknown output encoding."),
        };
    }
}
