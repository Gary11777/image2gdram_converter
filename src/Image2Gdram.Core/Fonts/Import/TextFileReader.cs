using System.Text;
using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Fonts.Import;

public enum ImportSyntax
{
    /// <summary>Инициализатор C: расширения <c>.c</c> и <c>.h</c>.</summary>
    C,

    /// <summary>Строки <c>DB</c>: расширения <c>.asm</c>, <c>.a51</c> и <c>.inc</c>.</summary>
    Asm,
}

public enum TextFileEncoding
{
    Utf8,
    Cp1251,
}

/// <summary>Текст файла импорта и кодировка, которой он прочитан (решение D-17).</summary>
public sealed record DecodedText(string Text, TextFileEncoding Encoding);

/// <summary>
/// Чтение текстового файла импорта (п. 4.2.2 ТЗ, источник 4; решения D-17, N-18, N-21).
/// Сначала строгий UTF-8, при ошибке — CP1251. Файл открывается только на чтение и не изменяется.
/// </summary>
public static class TextFileReader
{
    public const long MaxFileSize = 16L * 1024 * 1024;

    private static readonly UTF8Encoding Utf8Strict = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Синтаксис по расширению. Другое расширение — <see cref="ArgumentException"/>.</summary>
    public static ImportSyntax GetSyntax(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string extension = Path.GetExtension(path);
        if (extension.Equals(".c", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".h", StringComparison.OrdinalIgnoreCase))
        {
            return ImportSyntax.C;
        }

        if (extension.Equals(".asm", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".a51", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".inc", StringComparison.OrdinalIgnoreCase))
        {
            return ImportSyntax.Asm;
        }

        throw new ArgumentException($"Extension '{extension}' is not an import format (.c, .h, .asm, .a51, .inc).", nameof(path));
    }

    /// <summary>Читает файл целиком. Размер больше 16 МБ отвергается до чтения содержимого.</summary>
    public static DecodedText Read(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        FileInfo info;
        try
        {
            info = new FileInfo(path);
        }
        catch (Exception ex) when (IsIo(ex))
        {
            throw IoError(path, ex);
        }

        if (!info.Exists)
        {
            throw IoError(path, new FileNotFoundException("Import file was not found.", path));
        }

        long length = info.Length;
        if (length > MaxFileSize)
        {
            throw new ArrayImportException(ArrayImportErrorKind.FileTooLarge, 0, null);
        }

        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var bytes = new byte[length];
            stream.ReadExactly(bytes);
            return Decode(bytes);
        }
        catch (ArrayImportException)
        {
            throw;
        }
        catch (Exception ex) when (IsIo(ex))
        {
            throw IoError(path, ex);
        }
    }

    /// <summary>Декодирует байты: BOM <c>EF BB BF</c> отбрасывается, затем строгий UTF-8, иначе CP1251.</summary>
    public static DecodedText Decode(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> data = bytes;
        if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
        {
            data = data[3..];
        }

        if (TryUtf8(data, out string text))
        {
            return new DecodedText(text, TextFileEncoding.Utf8);
        }

        return new DecodedText(Cp1251.GetString(data), TextFileEncoding.Cp1251);
    }

    private static bool TryUtf8(ReadOnlySpan<byte> data, out string text)
    {
        try
        {
            text = Utf8Strict.GetString(data);
            return true;
        }
        catch (DecoderFallbackException)
        {
            text = string.Empty;
            return false;
        }
    }

    private static bool IsIo(Exception ex) =>
        ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException;

    private static ArrayImportException IoError(string path, Exception ex) =>
        new(ArrayImportErrorKind.IoError, 0, path, ex);
}
