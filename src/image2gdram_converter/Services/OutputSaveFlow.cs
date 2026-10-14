using System.IO;
using Image2Gdram.Core.Output;
using image2gdram_converter.Controls;

namespace image2gdram_converter.Services;

/// <summary>
/// Сохранение кода: окно сохранения файла с проверкой имени массива и перегенерацией документа (решение N-71).
/// </summary>
internal static class OutputSaveFlow
{
    private static readonly HashSet<string> KnownExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".c",
        ".h",
        ".a51",
        ".asm",
        ".bin",
    };

    public static void Run(
        ILocalizationService loc,
        IDialogService dialogs,
        IFileDialogService files,
        Func<OutputDocument?> getDocument,
        Func<bool> canSave,
        OutputFormat format,
        string? initialFolder,
        Func<string> getArrayName,
        Action<string> setArrayName,
        Func<OutputDocument?> regenerateNow,
        Func<string> getCodeText,
        IEnumerable<string> protectedPaths,
        Action<string> onSaved)
    {
        TextBoxCommit.FlushFocused();
        OutputDocument? currentDocument = getDocument();
        if (currentDocument is null || !canSave())
        {
            return;
        }

        string? folder = initialFolder;
        string name = getArrayName();
        string title = loc.Get("File.SaveOutputTitle");
        (string filter, string defaultExt) = BuildFilter(loc, currentDocument);

        string? directory;
        while (true)
        {
            string? path = files.PickSaveOutput(folder, name, title, filter, defaultExt);
            if (path is null)
            {
                return;
            }

            (string? pickedDirectory, string parsedName) = ParsePath(path);
            directory = pickedDirectory ?? folder;
            NameValidationResult validation = NameValidator.Validate(parsedName, format);
            if (validation.IsValid)
            {
                name = parsedName;
                break;
            }

            dialogs.Alert(UserText.Name(loc, validation));
            folder = directory;
            name = parsedName;
        }

        OutputDocument? document;
        if (!string.Equals(name, getArrayName(), StringComparison.Ordinal))
        {
            setArrayName(name);
            document = regenerateNow();
        }
        else if (!MatchesDocumentName(currentDocument, name))
        {
            document = regenerateNow();
        }
        else
        {
            document = currentDocument;
        }

        if (document is null)
        {
            string message = getCodeText();
            if (!string.IsNullOrEmpty(message))
            {
                dialogs.Alert(message);
            }

            return;
        }

        if (string.IsNullOrWhiteSpace(directory))
        {
            return;
        }

        try
        {
            OutputSaveResult result = OutputWriter.Save(
                document,
                directory,
                existing => dialogs.Confirm(loc.Format("Dialog.Overwrite", string.Join(Environment.NewLine, existing))),
                protectedPaths);
            if (result == OutputSaveResult.Saved)
            {
                onSaved(directory);
            }
        }
        catch (OutputWriteException ex)
        {
            dialogs.Alert(UserText.OutputWrite(loc, ex));
        }
    }

    public static (string Filter, string DefaultExt) BuildFilter(ILocalizationService loc, OutputDocument document)
    {
        ArgumentNullException.ThrowIfNull(loc);
        ArgumentNullException.ThrowIfNull(document);
        string[] extensions = document.Files
            .Select(file => Path.GetExtension(file.FileName))
            .Where(ext => !string.IsNullOrEmpty(ext))
            .ToArray();
        string defaultExt = extensions.Length > 0 ? extensions[0] : string.Empty;
        string[] masks = extensions.Select(ext => "*" + ext).ToArray();
        string display = string.Join(", ", masks);
        string pattern = masks.Length > 0 ? masks[0] : "*.*";
        return (loc.Format("File.SaveOutputFilter", display, pattern), defaultExt);
    }

    public static (string? Directory, string Name) ParsePath(string path)
    {
        string? directory = Path.GetDirectoryName(path);
        string file = Path.GetFileName(path);
        string ext = Path.GetExtension(file);
        string name = KnownExtensions.Contains(ext)
            ? Path.GetFileNameWithoutExtension(file)
            : file;
        return (NullIfEmpty(directory), name);
    }

    public static string? FolderOf(string? filePath) =>
        string.IsNullOrWhiteSpace(filePath) ? null : NullIfEmpty(Path.GetDirectoryName(filePath));

    public static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private static bool MatchesDocumentName(OutputDocument document, string name) =>
        document.Files.Count > 0
        && string.Equals(Path.GetFileNameWithoutExtension(document.Files[0].FileName), name, StringComparison.Ordinal);
}
