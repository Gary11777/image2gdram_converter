namespace image2gdram_converter.Services;

/// <summary>Запоминает запрос записи и не пишет сам. Тесты вызывают действие, когда нужно.</summary>
public sealed class ManualSettingsAutosave : ISettingsAutosave
{
    public int Scheduled { get; private set; }

    public Action? Pending { get; private set; }

    public void Schedule(Action save)
    {
        ArgumentNullException.ThrowIfNull(save);
        Scheduled++;
        Pending = save;
    }

    public void Cancel()
    {
    }

    public void RunPending() => Pending?.Invoke();
}
