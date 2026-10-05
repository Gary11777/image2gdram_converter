namespace Image2Gdram.Core.Output;

public enum OutputSaveResult
{
    Saved,

    /// <summary>Пользователь отказался перезаписывать существующие файлы; ничего не записано.</summary>
    Cancelled,
}

public enum OutputWriteError
{
    /// <summary>Путь совпадает с исходным файлом изображения, листа или импортированного массива (решение N-18).</summary>
    WouldOverwriteSource,

    /// <summary>Нет доступа к файлу или папке.</summary>
    AccessDenied,

    /// <summary>Прочая ошибка ввода-вывода: нет папки, диск заполнен, файл занят.</summary>
    IoError,
}

/// <summary>Ошибка сохранения вывода; текст для пользователя интерфейс берёт по <see cref="Error"/>.</summary>
public sealed class OutputWriteException : Exception
{
    public OutputWriteException(OutputWriteError error, string path, Exception? innerException = null)
        : base($"Cannot write '{path}': {error}.", innerException)
    {
        Error = error;
        Path = path;
    }

    public OutputWriteError Error { get; }

    public string Path { get; }
}

/// <summary>
/// Сохранение файлов документа в папку (п. 4.4.1 ТЗ). Имена файлов берутся из документа (<c>&lt;имя массива&gt;.c</c> и т. д.),
/// поэтому <c>#include</c> в <c>.c</c> всегда совпадает с именем <c>.h</c> (решение N-43).
/// </summary>
public static class OutputWriter
{
    /// <summary>Полные пути, по которым будут записаны файлы документа.</summary>
    public static IReadOnlyList<string> GetTargetPaths(OutputDocument document, string directory)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        string root = Path.GetFullPath(directory);
        return document.Files.Select(f => Path.Combine(root, f.FileName)).ToArray();
    }

    /// <summary>
    /// Записывает все файлы документа. Если какие-то файлы уже существуют, вызывает <paramref name="confirmOverwrite"/>
    /// со списком их путей; отказ — <see cref="OutputSaveResult.Cancelled"/>, ничего не записывается.
    /// Запись в путь из <paramref name="protectedPaths"/> запрещена (исходные файлы, раздел 6 ТЗ).
    /// Каждый файл пишется во временный файл рядом и затем заменяет целевой.
    /// </summary>
    public static OutputSaveResult Save(
        OutputDocument document,
        string directory,
        Func<IReadOnlyList<string>, bool> confirmOverwrite,
        IEnumerable<string>? protectedPaths = null)
    {
        ArgumentNullException.ThrowIfNull(confirmOverwrite);
        IReadOnlyList<string> targets = GetTargetPaths(document, directory);

        var protectedSet = new HashSet<string>(
            (protectedPaths ?? Array.Empty<string>()).Where(p => !string.IsNullOrWhiteSpace(p)).Select(Path.GetFullPath),
            StringComparer.OrdinalIgnoreCase);
        foreach (string target in targets)
        {
            if (protectedSet.Contains(target))
            {
                throw new OutputWriteException(OutputWriteError.WouldOverwriteSource, target);
            }
        }

        string[] existing = targets.Where(File.Exists).ToArray();
        if (existing.Length > 0 && !confirmOverwrite(existing))
        {
            return OutputSaveResult.Cancelled;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            Write(targets[i], document.Files[i].Content);
        }

        return OutputSaveResult.Saved;
    }

    private static void Write(string path, byte[] content)
    {
        string temp = path + ".tmp";
        try
        {
            File.WriteAllBytes(temp, content);
            File.Move(temp, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Security.SecurityException)
        {
            TryDelete(temp);
            OutputWriteError error = ex is UnauthorizedAccessException or System.Security.SecurityException
                ? OutputWriteError.AccessDenied
                : OutputWriteError.IoError;
            throw new OutputWriteException(error, path, ex);
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Временный файл остаётся; основная ошибка уже сообщается вызывающему.
        }
    }
}
