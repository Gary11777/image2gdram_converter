namespace image2gdram_converter.Services;

public enum SaveChoice
{
    Save,
    Discard,
    Cancel,
}

public interface IDialogService
{
    bool Confirm(string message);

    SaveChoice AskSave(string message);

    /// <summary><c>null</c> — отказ. Пустая строка возможна: её отклоняет вызывающий код.</summary>
    string? AskText(string message, string initial);

    void Alert(string message);
}
