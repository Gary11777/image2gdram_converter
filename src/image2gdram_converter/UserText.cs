using System.Globalization;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts.Import;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Output;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter.Services;

namespace image2gdram_converter;

/// <summary>Тексты сообщений по кодам ядра. Сами фразы лежат в словаре.</summary>
public static class UserText
{
    public static string ImageLoad(ILocalizationService text, ImageLoadException error)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(error);
        return error.Error switch
        {
            ImageLoadError.TooLarge => text.Format(
                "Error.Image.TooLarge",
                error.Width ?? 0,
                error.Height ?? 0,
                ImageLimits.MaxSide),
            ImageLoadError.MemoryLimit => text.Get("Error.Image.MemoryLimit"),
            ImageLoadError.Unsupported => text.Get("Error.Image.Unsupported"),
            ImageLoadError.Corrupted => text.Get("Error.Image.Corrupted"),
            _ => text.Format("Error.Image.IoError", error.Message),
        };
    }

    public static string Name(ILocalizationService text, NameValidationResult result) => result.Error switch
    {
        NameValidationError.Empty => text.Get("Error.Name.Empty"),
        NameValidationError.InvalidCharacter => text.Get("Error.Name.InvalidCharacter"),
        NameValidationError.StartsWithDigit => text.Get("Error.Name.StartsWithDigit"),
        NameValidationError.TooLong => text.Format("Error.Name.TooLong", result.MaxLength),
        NameValidationError.CKeyword => text.Get("Error.Name.CKeyword"),
        NameValidationError.KeilC51Keyword => text.Get("Error.Name.Keil"),
        NameValidationError.ReservedCIdentifier => text.Get("Error.Name.Reserved"),
        NameValidationError.StdintTypeName => text.Get("Error.Name.Stdint"),
        NameValidationError.A51ReservedWord => text.Get("Error.Name.A51"),
        _ => string.Empty,
    };

    public static string Preset(ILocalizationService text, PresetException error) => error.Error switch
    {
        PresetError.EmptyName => text.Get("Error.Preset.EmptyName"),
        PresetError.InvalidName => text.Get("Error.Preset.InvalidName"),
        PresetError.InvalidController => text.Get("Error.Preset.InvalidController"),
        PresetError.InvalidSize => text.Get("Error.Preset.InvalidSize"),
        PresetError.InvalidPacking => text.Get("Error.Preset.InvalidPacking"),
        PresetError.ReservedName => text.Get("Error.Preset.ReservedName"),
        PresetError.DuplicateName => text.Get("Error.Preset.DuplicateName"),
        PresetError.BuiltIn => text.Get("Error.Preset.BuiltIn"),
        PresetError.NotFound => text.Get("Error.Preset.NotFound"),
        _ => text.Get("Error.Preset.InvalidName"),
    };

    public static string OutputWrite(ILocalizationService text, OutputWriteException error) => error.Error switch
    {
        OutputWriteError.WouldOverwriteSource => text.Format("Error.Output.Source", error.Path),
        OutputWriteError.AccessDenied => text.Format("Error.Output.Access", error.Path),
        _ => text.Format("Error.Output.Io", error.Path),
    };

    public static string Project(ILocalizationService text, ProjectException error) => error.Error switch
    {
        ProjectError.UnsupportedVersion => text.Format("Error.Project.Version", error.Path),
        ProjectError.Corrupt => text.Format("Error.Project.Corrupt", error.Path),
        ProjectError.AccessDenied => text.Format("Error.Project.Access", error.Path),
        _ => text.Format("Error.Project.Io", error.Path),
    };

    public static string Settings(ILocalizationService text, SettingsException error) => error.Error switch
    {
        SettingsError.AccessDenied => text.Format("Error.Settings.Access", error.Path),
        _ => text.Format("Error.Settings.Io", error.Path),
    };

    public static string Import(ILocalizationService text, ArrayImportException error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error.Kind switch
        {
            ArrayImportErrorKind.FileTooLarge => text.Get("Error.Import.Large"),
            ArrayImportErrorKind.IoError => text.Format("Error.Import.Io", error.Token ?? string.Empty),
            ArrayImportErrorKind.UnterminatedComment => text.Format("Error.Import.Comment", error.Line),
            ArrayImportErrorKind.UnterminatedString => text.Format("Error.Import.String", error.Line),
            ArrayImportErrorKind.UnbalancedBraces => text.Format("Error.Import.Braces", error.Line),
            _ => text.Format(IssueKey(error.Kind), error.Line, error.Token ?? string.Empty),
        };
    }

    public static string ImportIssue(ILocalizationService text, ArrayImportIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        return issue.Kind switch
        {
            ArrayImportErrorKind.FileTooLarge => text.Get("Error.Import.Large"),
            ArrayImportErrorKind.IoError => text.Format("Error.Import.Io", issue.Token ?? string.Empty),
            ArrayImportErrorKind.UnterminatedComment => text.Format("Error.Import.Comment", issue.Line),
            ArrayImportErrorKind.UnterminatedString => text.Format("Error.Import.String", issue.Line),
            ArrayImportErrorKind.UnbalancedBraces => text.Format("Error.Import.Braces", issue.Line),
            _ => text.Format(IssueKey(issue.Kind), issue.Line, issue.Token),
        };
    }

    private static string IssueKey(ArrayImportErrorKind kind) => kind switch
    {
        ArrayImportErrorKind.CharacterNotInCp1251 => "Error.Import.Char",
        ArrayImportErrorKind.InvalidNumber => "Error.Import.Number",
        _ => "Error.Import.Value",
    };

    public static string Diagnostic(ILocalizationService text, Diagnostic diagnostic)
    {
        ArgumentNullException.ThrowIfNull(diagnostic);
        string At(int index) => index < diagnostic.Arguments.Count ? diagnostic.Arguments[index] : string.Empty;
        return diagnostic.Code switch
        {
            DiagnosticCode.ArrayExceeds64KForC51 => text.Format("Diag.ArrayExceeds64K", At(0)),
            DiagnosticCode.NonCp1251CharactersReplaced => text.Format("Diag.NonCp1251", Field(text, At(0)), At(1)),
            DiagnosticCode.FontNotFound => text.Format("Diag.FontNotFound", At(0)),
            DiagnosticCode.GlyphsMissingInFont => text.Format("Diag.GlyphsMissing", string.Join(", ", diagnostic.Arguments)),
            DiagnosticCode.SheetTooSmall => text.Format("Diag.SheetTooSmall", At(0), At(1)),
            DiagnosticCode.ImportValueCountMismatch => text.Format("Diag.ImportCount", At(0), At(1)),
            DiagnosticCode.SettingsFileReset => text.Format("Diag.SettingsReset", At(0)),
            DiagnosticCode.ProjectSourceNotFound => text.Format("Diag.SourceMissing", Role(text, At(0)), At(1)),
            _ => diagnostic.Code.ToString(),
        };
    }

    public static string Hover(
        ILocalizationService text,
        int x,
        int y,
        int byteIndex,
        int bit,
        byte value,
        bool active,
        bool allFrames,
        int frameNumber,
        int indexInFrame)
    {
        string line = text.Format(
            "Hover.Cell",
            IntText.Format(x),
            IntText.Format(y),
            IntText.Format(byteIndex),
            byteIndex.ToString("X", CultureInfo.InvariantCulture),
            IntText.Format(bit),
            value.ToString("X2", CultureInfo.InvariantCulture),
            Convert.ToString(value, 2).PadLeft(8, '0'),
            text.Get(active ? "Hover.Yes" : "Hover.No"));
        if (!allFrames)
        {
            return line;
        }

        return text.Format(
            "Hover.Frame",
            line,
            IntText.Format(frameNumber),
            IntText.Format(indexInFrame),
            indexInFrame.ToString("X", CultureInfo.InvariantCulture));
    }

    private static string Field(ILocalizationService text, string field) => field switch
    {
        nameof(HeaderField.SourceFile) => text.Get("Header.SourceFile"),
        nameof(HeaderField.FontFamily) => text.Get("Header.FontFamily"),
        nameof(HeaderField.ImportedArrayName) => text.Get("Header.ArrayName"),
        nameof(HeaderField.Preset) => text.Get("Header.Preset"),
        _ => field,
    };

    private static string Role(ILocalizationService text, string role) => role switch
    {
        ProjectSources.Image => text.Get("Role.Image"),
        ProjectSources.Sheet => text.Get("Role.Sheet"),
        ProjectSources.Import => text.Get("Role.Import"),
        _ => role,
    };
}
