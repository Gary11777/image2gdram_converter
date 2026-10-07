namespace image2gdram_converter.Services;

public interface IFileDialogService
{
    string? PickOpenImage(string? folder);

    string? PickOpenSheet(string? folder);

    string? PickOpenImport(string? folder);

    string? PickOpenProject(string? folder);

    string? PickSaveProject(string? folder);

    string? PickFolder(string? folder, string title);
}
