namespace image2gdram_converter.Services;

/// <summary>Строки интерфейса по ключу. Текст живёт в словаре, не в коде.</summary>
public interface ILocalizationService
{
    string Get(string key);

    string Format(string key, params object[] args);
}
