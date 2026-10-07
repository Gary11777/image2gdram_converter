using System.ComponentModel;

namespace image2gdram_converter.ViewModels;

/// <summary>Текст окна кода и подсветка байта. Общий контракт вкладок картинки и шрифта.</summary>
public interface ICodeSurface : INotifyPropertyChanged
{
    string CodeText { get; }

    int HighlightStart { get; }

    int HighlightLength { get; }
}
