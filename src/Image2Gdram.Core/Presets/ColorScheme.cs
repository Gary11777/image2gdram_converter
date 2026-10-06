namespace Image2Gdram.Core.Presets;

/// <summary>Цветовая схема сетки (п. 5.2 ТЗ): «ЖКИ» или «OLED». В версии 1.0 на упаковку не влияет.</summary>
public enum ColorScheme
{
    /// <summary>Тёмные пиксели на светлом фоне.</summary>
    Lcd,

    /// <summary>Светлые пиксели на тёмном фоне.</summary>
    Oled,
}
