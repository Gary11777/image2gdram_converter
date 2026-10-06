using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Persistence;
using Image2Gdram.Core.Presets;

namespace Image2Gdram.Core.Settings;

/// <summary>Результат чтения настроек. Повреждённый файл не мешает запуску: возвращаются умолчания и диагностика.</summary>
public sealed class SettingsLoadResult
{
    public SettingsLoadResult(AppSettings settings, Diagnostic? diagnostic)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Settings = settings;
        Diagnostic = diagnostic;
    }

    public AppSettings Settings { get; }

    /// <summary><see cref="DiagnosticCode.SettingsFileReset"/>, если файл был и его не удалось прочитать.</summary>
    public Diagnostic? Diagnostic { get; }
}

/// <summary>
/// Чтение и запись <c>settings.json</c> (п. 4.5 ТЗ, решение N-25). Запись атомарная.
/// Когда сохранять (при выходе и через секунду после изменения) решает приложение, не этот класс.
/// Исходные файлы не открываются.
/// </summary>
public sealed class SettingsService
{
    public SettingsService()
        : this(AppContext.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData))
    {
    }

    public SettingsService(string baseDirectory, string applicationDataRoot)
    {
        FilePath = SettingsPathResolver.Resolve(baseDirectory, applicationDataRoot);
    }

    public string FilePath { get; }

    public SettingsLoadResult Load()
    {
        if (!File.Exists(FilePath))
        {
            return new SettingsLoadResult(AppSettings.CreateDefault(), null);
        }

        try
        {
            AppSettings settings = SettingsMapping.Read(AtomicFile.ReadAllBytesShared(FilePath));
            return new SettingsLoadResult(settings, null);
        }
        catch (Exception ex) when (ex is FormatException or StoredDataException or PresetException)
        {
            return new SettingsLoadResult(
                AppSettings.CreateDefault(),
                new Diagnostic(DiagnosticCode.SettingsFileReset, DiagnosticSeverity.Warning, new[] { FilePath }));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SettingsException(Classify(ex), FilePath, ex);
        }
    }

    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        byte[] content = SettingsMapping.Write(settings);
        try
        {
            AtomicFile.Write(FilePath, content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SettingsException(Classify(ex), FilePath, ex);
        }
    }

    private static SettingsError Classify(Exception exception) =>
        exception is UnauthorizedAccessException ? SettingsError.AccessDenied : SettingsError.IoError;
}
