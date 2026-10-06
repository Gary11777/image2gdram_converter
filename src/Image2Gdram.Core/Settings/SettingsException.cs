namespace Image2Gdram.Core.Settings;

public enum SettingsError
{
    AccessDenied,
    IoError,
}

/// <summary>Ошибка чтения или записи <c>settings.json</c>. Повреждённый файл сюда не попадает: он заменяется умолчаниями.</summary>
public sealed class SettingsException : Exception
{
    public SettingsException(SettingsError error, string path, Exception? innerException = null)
        : base($"Cannot use settings file '{path}': {error}.", innerException)
    {
        Error = error;
        Path = path;
    }

    public SettingsError Error { get; }

    public string Path { get; }
}
