namespace image2gdram_converter.ViewModels;

public sealed class CodeFileItem
{
    public CodeFileItem(string caption, string text)
    {
        Caption = caption;
        Text = text;
    }

    public string Caption { get; }

    public string Text { get; }
}
