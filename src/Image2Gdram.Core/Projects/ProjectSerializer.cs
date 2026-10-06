using Image2Gdram.Core.Persistence;

namespace Image2Gdram.Core.Projects;

/// <summary>
///  <c>.iiu</c>: JSON, UTF-8  BOM,   (. 4.5  8 ,  N-16).
///          ,   .
///   ,      .
/// </summary>
public static class ProjectSerializer
{
    public static void Save(ProjectDocument project, string path)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string full = Path.GetFullPath(path);
        byte[] content = JsonFormat.Write(ProjectMapping.ToDto(project, full));
        try
        {
            AtomicFile.Write(full, content);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ProjectException(Classify(ex), full, ex);
        }
    }

    public static ProjectLoadResult Load(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string full = Path.GetFullPath(path);
        byte[] content;
        try
        {
            content = AtomicFile.ReadAllBytesShared(full);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new ProjectException(Classify(ex), full, ex);
        }

        ProjectFileDto dto;
        try
        {
            dto = JsonFormat.Parse<ProjectFileDto>(content);
        }
        catch (Exception ex) when (ex is FormatException or StoredDataException)
        {
            throw new ProjectException(ProjectError.Corrupt, full, ex);
        }

        if (dto.FormatVersion != ProjectDocument.FormatVersion)
        {
            throw new ProjectException(ProjectError.UnsupportedVersion, full);
        }

        try
        {
            return ProjectMapping.ToDocument(full, dto);
        }
        catch (Exception ex) when (ex is StoredDataException or ArgumentException)
        {
            throw new ProjectException(ProjectError.Corrupt, full, ex);
        }
    }

    private static ProjectError Classify(Exception exception) =>
        exception is UnauthorizedAccessException ? ProjectError.AccessDenied : ProjectError.IoError;
}
