namespace image2gdram_converter.Services;

/// <summary>Отложенная запись настроек. Выход из программы сохраняет их сразу.</summary>
public interface ISettingsAutosave
{
    void Schedule(Action save);

    void Cancel();
}
