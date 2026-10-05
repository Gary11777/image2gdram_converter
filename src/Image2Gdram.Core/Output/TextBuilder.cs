using System.Text;

namespace Image2Gdram.Core.Output;

/// <summary>Сборка текста с переводами строк CRLF (п. 4.4.8 ТЗ); позиция нужна для карты байтов.</summary>
internal sealed class TextBuilder
{
    public const string NewLine = "\r\n";

    private readonly StringBuilder _sb;

    public TextBuilder(int capacity = 1024)
    {
        _sb = new StringBuilder(capacity);
    }

    public int Position => _sb.Length;

    public TextBuilder Append(string text)
    {
        _sb.Append(text);
        return this;
    }

    public TextBuilder Line(string text = "")
    {
        _sb.Append(text).Append(NewLine);
        return this;
    }

    public TextBuilder EndLine()
    {
        _sb.Append(NewLine);
        return this;
    }

    public override string ToString() => _sb.ToString();
}
