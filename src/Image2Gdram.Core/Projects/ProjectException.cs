namespace Image2Gdram.Core.Projects;

public enum ProjectError
{
    /// <summary>В файле другая <c>formatVersion</c>. Файл не заменяется умолчаниями, чтобы следующее сохранение его не затёрло.</summary>
    UnsupportedVersion,

    Corrupt,
    AccessDenied,
    IoError,
}

/// <summary>Ошибка открытия или сохранения проекта <c>.iiu</c>.</summary>
public sealed class ProjectException : Exception
{
    public ProjectException(ProjectError error, string path, Exception? innerException = null)
        : base($"Cannot use project file '{path}': {error}.", innerException)
    {
        Error = error;
        Path = path;
    }

    public ProjectError Error { get; }

    public string Path { get; }
}
