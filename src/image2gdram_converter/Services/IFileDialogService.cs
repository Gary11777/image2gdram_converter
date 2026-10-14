namespace image2gdram_converter.Services;

public interface IFileDialogService
{
    string? PickOpenImage(string? folder);

    string? PickOpenSheet(string? folder);

    string? PickOpenImport(string? folder);

    string? PickOpenProject(string? folder);

    string? PickSaveProject(string? folder);

    /// <summary>Окно сохранения кода. Возвращает выбранный путь файла или <c>null</c>.</summary>
    string? PickSaveOutput(string? folder, string fileName, string title, string filter, string defaultExt);
}
