using Microsoft.Win32;

namespace image2gdram_converter.Services;

public sealed class FileDialogService : IFileDialogService
{
    private readonly ILocalizationService _text;

    public FileDialogService(ILocalizationService text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _text = text;
    }

    public string? PickOpenImage(string? folder)
    {
        var dialog = new OpenFileDialog
        {
            Filter = _text.Get("File.ImageFilter"),
            InitialDirectory = folder ?? string.Empty,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickOpenSheet(string? folder)
    {
        var dialog = new OpenFileDialog
        {
            Filter = _text.Get("File.SheetFilter"),
            InitialDirectory = folder ?? string.Empty,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickOpenImport(string? folder)
    {
        var dialog = new OpenFileDialog
        {
            Filter = _text.Get("File.ImportFilter"),
            InitialDirectory = folder ?? string.Empty,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickOpenProject(string? folder)
    {
        var dialog = new OpenFileDialog
        {
            Filter = _text.Get("File.ProjectFilter"),
            InitialDirectory = folder ?? string.Empty,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickSaveProject(string? folder)
    {
        var dialog = new SaveFileDialog
        {
            Filter = _text.Get("File.ProjectFilter"),
            DefaultExt = ".iiu",
            AddExtension = true,
            InitialDirectory = folder ?? string.Empty,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    public string? PickFolder(string? folder, string title)
    {
        var dialog = new OpenFolderDialog
        {
            Title = title,
            InitialDirectory = folder ?? string.Empty,
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
