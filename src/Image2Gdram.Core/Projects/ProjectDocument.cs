using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Processing;
using Image2Gdram.Core.Settings;

namespace Image2Gdram.Core.Projects;

/// <summary>   <c>.iiu</c> ( N-16).    <see cref="FormatVersion"/>.</summary>
public sealed class ProjectDocument
{
    public const int FormatVersion = 1;

    public ProjectDocument(ImageProjectTab image, FontProjectTab font)
    {
        ArgumentNullException.ThrowIfNull(image);
        ArgumentNullException.ThrowIfNull(font);
        Image = image;
        Font = font;
    }

    public ImageProjectTab Image { get; }

    public FontProjectTab Font { get; }

    public static ProjectDocument CreateDefault() =>
        new(ImageProjectTab.CreateDefault(), FontProjectTab.CreateDefault());
}

/// <summary>   : ,       .</summary>
public sealed class ImageProjectTab
{
    public ImageProjectTab(ImageTabParameters parameters, string? sourcePath = null, FramePixelOverrides? edits = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Parameters = parameters;
        SourcePath = Clean(sourcePath);
        Edits = edits ?? new FramePixelOverrides();
    }

    public ImageTabParameters Parameters { get; }

    public string? SourcePath { get; }

    public FramePixelOverrides Edits { get; }

    public static ImageProjectTab CreateDefault() => new(ImageTabParameters.CreateDefault());

    private static string? Clean(string? path) => string.IsNullOrWhiteSpace(path) ? null : path.Trim();
}

/// <summary>   : ,       256 .</summary>
public sealed class FontProjectTab
{
    public FontProjectTab(FontTabParameters parameters, FontTable table, string? sheetPath = null, string? importPath = null)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        ArgumentNullException.ThrowIfNull(table);
        Parameters = parameters;
        Table = table;
        SheetPath = Clean(sheetPath);
        ImportPath = Clean(importPath);
    }

    public FontTabParameters Parameters { get; }

    public FontTable Table { get; }

    public string? SheetPath { get; }

    public string? ImportPath { get; }

    public static FontProjectTab CreateDefault() =>
        new(FontTabParameters.CreateDefault(), new FontTable(FontCellSize.Cell6x8));

    private static string? Clean(string? path) => string.IsNullOrWhiteSpace(path) ? null : path.Trim();
}

/// <summary>Роль пути в диагностике <see cref="DiagnosticCode.ProjectSourceNotFound"/>.</summary>
public static class ProjectSources
{
    public const string Image = "image";
    public const string Sheet = "sheet";
    public const string Import = "import";
}

/// <summary>Открытый проект. Предупреждения не мешают пользоваться параметрами и правками.</summary>
public sealed class ProjectLoadResult
{
    public ProjectLoadResult(ProjectDocument project, IReadOnlyList<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(diagnostics);
        Project = project;
        Diagnostics = diagnostics;
    }

    public ProjectDocument Project { get; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; }
}
