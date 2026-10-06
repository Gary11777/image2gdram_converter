namespace Image2Gdram.Core.Settings;

/// <summary>
/// Куда писать <c>settings.json</c> (п. 4.5 ТЗ, решение N-25): рядом с программой, а если туда записать нельзя —
/// в <c>%APPDATA%\ImageIU</c>. Проверка — реальная попытка создать и удалить файл, не флаг «только чтение».
/// Путь выбирается один раз при создании <see cref="SettingsService"/>.
/// </summary>
public static class SettingsPathResolver
{
    public const string FileName = "settings.json";
    public const string AppDataFolderName = "ImageIU";

    public static string Resolve() =>
        Resolve(AppContext.BaseDirectory, Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    public static string Resolve(string baseDirectory, string applicationDataRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(baseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationDataRoot);

        string primary = Path.GetFullPath(baseDirectory);
        if (CanWrite(primary))
        {
            return Path.Combine(primary, FileName);
        }

        string fallbackDir = Path.Combine(Path.GetFullPath(applicationDataRoot), AppDataFolderName);
        try
        {
            Directory.CreateDirectory(fallbackDir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SettingsException(SettingsError.AccessDenied, Path.Combine(fallbackDir, FileName), ex);
        }

        if (!CanWrite(fallbackDir))
        {
            throw new SettingsException(SettingsError.AccessDenied, Path.Combine(fallbackDir, FileName));
        }

        return Path.Combine(fallbackDir, FileName);
    }

    public static bool CanWrite(string directory)
    {
        string probe = Path.Combine(directory, ".imageiu-write-probe");
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllBytes(probe, new byte[] { 0 });
            File.Delete(probe);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            TryDelete(probe);
            return false;
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
