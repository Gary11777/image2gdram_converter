using System.Globalization;
using Image2Gdram.Core.Diagnostics;

namespace Image2Gdram.Core.Output;

/// <summary>
/// Пара файлов <c>.c</c> и <c>.h</c> по образцу приложения В (п. 4.4.4, 4.4.7 ТЗ, решения D-08, D-11, N-20, N-31).
/// Только блочные комментарии и шестнадцатеричные литералы, синтаксис C89. Файл <c>.c</c> подключает свой <c>.h</c>.
/// </summary>
public abstract class CGeneratorBase : GeneratorBase
{
    private const string Indent = "    ";
    private const string FrameIndent = "        ";
    private const string GlyphContinuationIndent = "      ";

    /// <summary>Объявление массива без <c>extern</c> и инициализатора, например <c>const uint8_t logo[LOGO_SIZE]</c>.</summary>
    protected abstract string Declare(string declarator, OutputOptions options);

    /// <summary>Нужен ли <c>#include &lt;stdint.h&gt;</c> в <c>.h</c>.</summary>
    protected abstract bool NeedsStdint(OutputOptions options);

    protected sealed override OutputDocument Generate(OutputData data, OutputOptions options, List<Diagnostic> diagnostics)
    {
        IReadOnlyList<string> header = HeaderCommentBuilder.Build(data, options, diagnostics);
        string name = options.ArrayName;
        string macro = name.ToUpperInvariant();
        int[] starts = new int[data.TotalBytes];

        var c = new TextBuilder(data.TotalBytes * 6 + 2048);
        WriteComment(c, header);
        c.Line($"#include \"{name}.h\"");
        c.Line();

        string declarator;
        List<(string Name, int Value)> macros;
        switch (data)
        {
            case ImageOutputData image when image.AllFrames:
                declarator = $"{name}[{macro}_FRAMES][{macro}_FRAME_SIZE]";
                macros = new()
                {
                    ($"{macro}_WIDTH", image.Width),
                    ($"{macro}_HEIGHT", image.Height),
                    ($"{macro}_FRAMES", image.FrameCount),
                    ($"{macro}_FRAME_SIZE", image.FrameSize),
                    ($"{macro}_SIZE", image.TotalBytes),
                };
                c.Line(Declare(declarator, options) + " = {");
                WriteFrames(c, image, options.BytesPerLine, starts);
                break;

            case ImageOutputData image:
                declarator = $"{name}[{macro}_SIZE]";
                macros = new()
                {
                    ($"{macro}_WIDTH", image.Width),
                    ($"{macro}_HEIGHT", image.Height),
                    ($"{macro}_SIZE", image.TotalBytes),
                };
                c.Line(Declare(declarator, options) + " = {");
                WriteValues(c, image.GetFrame(0), 0, options.BytesPerLine, Indent, starts);
                break;

            case FontOutputData font:
                declarator = $"{name}[{FontOutputData.CharCount.ToString(CultureInfo.InvariantCulture)}][{macro}_BYTES_PER_CHAR]";
                macros = new()
                {
                    ($"{macro}_CHAR_WIDTH", font.CellWidth),
                    ($"{macro}_CHAR_HEIGHT", font.CellHeight),
                    ($"{macro}_BYTES_PER_CHAR", font.BytesPerChar),
                };
                c.Line(Declare(declarator, options) + " = {");
                WriteGlyphs(c, font, starts);
                break;

            default:
                throw new ArgumentException($"Unsupported output data {data.GetType().Name}.", nameof(data));
        }

        c.Line("};");

        var h = new TextBuilder();
        WriteComment(h, header);
        h.Line();
        h.Line($"#ifndef {macro}_H");
        h.Line($"#define {macro}_H");
        h.Line();
        if (NeedsStdint(options))
        {
            h.Line("#include <stdint.h>");
            h.Line();
        }

        int width = macros.Max(m => m.Name.Length) + 2;
        foreach ((string macroName, int value) in macros)
        {
            h.Line("#define " + macroName.PadRight(width) + value.ToString(CultureInfo.InvariantCulture));
        }

        h.Line();
        h.Line("extern " + Declare(declarator, options) + ";");
        h.Line();
        h.Line("#endif");

        string cText = c.ToString();
        string hText = h.ToString();
        var files = new[]
        {
            new OutputFile(name + ".c", OutputFileKind.CSource, cText, OutputEncoder.Encode(cText, options.Encoding)),
            new OutputFile(name + ".h", OutputFileKind.CHeader, hText, OutputEncoder.Encode(hText, options.Encoding)),
        };
        return new OutputDocument(files, new ByteSpanMap(0, starts, 4), diagnostics);
    }

    private static void WriteComment(TextBuilder text, IReadOnlyList<string> lines)
    {
        text.Line("/*");
        foreach (string line in lines)
        {
            text.Line(" * " + line);
        }

        text.Line(" */");
    }

    private static void WriteFrames(TextBuilder text, ImageOutputData image, int bytesPerLine, int[] starts)
    {
        for (int f = 0; f < image.FrameCount; f++)
        {
            text.Line($"{Indent}{{ /* кадр {(f + 1).ToString(CultureInfo.InvariantCulture)} */");
            WriteValues(text, image.GetFrame(f), f * image.FrameSize, bytesPerLine, FrameIndent, starts);
            text.Line(f < image.FrameCount - 1 ? Indent + "}," : Indent + "}");
        }
    }

    /// <summary>Строки по <paramref name="bytesPerLine"/> значений; запятая после последнего значения не ставится.</summary>
    private static void WriteValues(TextBuilder text, ReadOnlySpan<byte> values, int firstIndex, int bytesPerLine, string indent, int[] starts)
    {
        int index = 0;
        foreach (int length in DataLayout.SplitImage(values.Length, bytesPerLine))
        {
            text.Append(indent);
            for (int i = 0; i < length; i++, index++)
            {
                if (i > 0)
                {
                    text.Append(", ");
                }

                starts[firstIndex + index] = text.Position;
                text.Append(NumberFormatter.C(values[index]));
            }

            text.Line(index < values.Length ? "," : string.Empty);
        }
    }

    private static void WriteGlyphs(TextBuilder text, FontOutputData font, int[] starts)
    {
        int[] lineLengths = DataLayout.SplitGlyph(font.BytesPerChar);
        for (int code = 0; code < FontOutputData.CharCount; code++)
        {
            ReadOnlySpan<byte> glyph = font.GetGlyph(code);
            int index = 0;
            for (int line = 0; line < lineLengths.Length; line++)
            {
                text.Append(line == 0 ? Indent + "{ " : GlyphContinuationIndent);
                for (int i = 0; i < lineLengths[line]; i++, index++)
                {
                    if (i > 0)
                    {
                        text.Append(", ");
                    }

                    starts[code * font.BytesPerChar + index] = text.Position;
                    text.Append(NumberFormatter.C(glyph[index]));
                }

                if (line < lineLengths.Length - 1)
                {
                    text.Line(",");
                }
            }

            string comma = code < FontOutputData.CharCount - 1 ? "," : string.Empty;
            text.Line($" }}{comma} {GlyphCommentFormatter.C((byte)code)}");
        }
    }
}
