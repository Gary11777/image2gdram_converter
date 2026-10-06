using System.Windows;

namespace image2gdram_converter.Services;

public sealed class ClipboardService : IClipboardService
{
    public void SetText(string text) => Clipboard.SetText(text);
}
