using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Image2Gdram.Core.Persistence;

/// <summary>Файл JSON не является строгим UTF-8 или не разбирается как JSON.</summary>
internal sealed class StoredDataException : Exception
{
    public StoredDataException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

internal static class JsonFormat
{
    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly byte[] Bom = { 0xEF, 0xBB, 0xBF };

    public static JsonSerializerOptions Options { get; } = CreateOptions();

    public static byte[] Write<T>(T value) => JsonSerializer.SerializeToUtf8Bytes(value, Options);

    public static T Parse<T>(byte[] content)
    {
        try
        {
            string text = Utf8.GetString(SkipBom(content));
            T? value = JsonSerializer.Deserialize<T>(text, Options);
            if (value is null)
            {
                throw new StoredDataException("JSON value is empty.");
            }

            return value;
        }
        catch (Exception ex) when (ex is ArgumentException or JsonException or DecoderFallbackException)
        {
            throw new FormatException("The file is not strict UTF-8 JSON.", ex);
        }
    }

    private static ReadOnlySpan<byte> SkipBom(byte[] content)
    {
        ReadOnlySpan<byte> bytes = content;
        return bytes.StartsWith(Bom) ? bytes[Bom.Length..] : bytes;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        options.Converters.Add(new JsonStringEnumConverter(namingPolicy: null, allowIntegerValues: false));
        return options;
    }
}

internal static class AtomicFile
{
    public static byte[] ReadAllBytesShared(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static void Write(string path, byte[] content)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllBytes(temp, content);
            File.Move(temp, path, overwrite: true);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
        }
    }
}
