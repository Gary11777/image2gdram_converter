using Image2Gdram.Core.Diagnostics;

namespace Image2Gdram.Core.Output;

/// <summary>Один файл вывода.</summary>
public sealed class OutputFile
{
    public OutputFile(string fileName, OutputFileKind kind, string text, byte[] content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(content);
        FileName = fileName;
        Kind = kind;
        Text = text;
        Content = content;
    }

    /// <summary>Имя файла без пути, например <c>logo_128x64.c</c>.</summary>
    public string FileName { get; }

    public OutputFileKind Kind { get; }

    /// <summary>
    /// Текст для окна кода и буфера обмена (CRLF). Для двоичного файла — шестнадцатеричный дамп
    /// (решение N-17), который не сохраняется.
    /// </summary>
    public string Text { get; }

    /// <summary>Байты, которые записываются в файл: закодированный текст или двоичные данные.</summary>
    public byte[] Content { get; }
}

/// <summary>Позиция значения байта массива в тексте файла.</summary>
public readonly record struct ByteSpan(int FileIndex, int Start, int Length);

/// <summary>
/// Карта «индекс байта массива → позиция его литерала в тексте» для подсветки байта в окне кода (п. 5.2 ТЗ).
/// Все байты массива лежат в одном файле, литералы одного документа имеют одинаковую длину.
/// </summary>
public sealed class ByteSpanMap
{
    private readonly int[] _starts;

    public ByteSpanMap(int fileIndex, int[] starts, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fileIndex);
        ArgumentNullException.ThrowIfNull(starts);
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        FileIndex = fileIndex;
        _starts = starts;
        Length = length;
    }

    public int FileIndex { get; }

    /// <summary>Длина литерала байта в символах: 4 для <c>0x7C</c> и <c>07Ch</c>, 9 для <c>01111100b</c>, 2 в дампе.</summary>
    public int Length { get; }

    public int Count => _starts.Length;

    public ByteSpan this[int byteIndex] => new(FileIndex, _starts[byteIndex], Length);
}

/// <summary>Результат генерации: файлы, карта позиций байтов и предупреждения.</summary>
public sealed class OutputDocument
{
    public OutputDocument(IReadOnlyList<OutputFile> files, ByteSpanMap byteMap, IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(byteMap);
        ArgumentNullException.ThrowIfNull(diagnostics);
        if (files.Count == 0 || byteMap.FileIndex >= files.Count)
        {
            throw new ArgumentException("The byte map must refer to one of the files.", nameof(byteMap));
        }

        Files = files;
        ByteMap = byteMap;
        Diagnostics = diagnostics;
    }

    /// <summary>Файлы; первым идёт файл с данными массива (<c>.c</c>, ассемблер или BIN).</summary>
    public IReadOnlyList<OutputFile> Files { get; }

    public ByteSpanMap ByteMap { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
