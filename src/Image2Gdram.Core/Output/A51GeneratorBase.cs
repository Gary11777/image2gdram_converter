using System.Globalization;
using Image2Gdram.Core.Diagnostics;

namespace Image2Gdram.Core.Output;

/// <summary>
/// Ассемблер Keil A51 по образцу примеров В.2 и В.3 (п. 4.4.4, 4.4.5 ТЗ, решения D-09, N-07, N-20, N-31):
/// метка с первой позиции, директива с 17-й, операнды с 25-й, комментарий через два пробела после значений.
/// Строка <c>DB</c> не заканчивается запятой. Имя метки — в верхнем регистре.
/// </summary>
public abstract class A51GeneratorBase : GeneratorBase
{
    /// <summary>Отступ до директивы: 16 пробелов.</summary>
    protected const string DirectiveIndent = "                ";

    private const int DirectiveWidth = 8;
    private const int LabelWidth = 16;

    protected sealed override bool TargetsC51 => true;

    /// <summary>Строки между заголовком и меткой (<c>PUBLIC</c>, сегмент).</summary>
    private protected abstract void WritePrologue(TextBuilder text, string label);

    /// <summary>Строки после данных (<c>END</c>).</summary>
    private protected abstract void WriteEpilogue(TextBuilder text);

    /// <summary>Директива с операндами, выровненная по образцу: <c>                RSEG    ?CO?NAME</c>.</summary>
    protected static string Directive(string directive, string operands) =>
        DirectiveIndent + directive.PadRight(DirectiveWidth) + operands;

    /// <summary>Метка в поле шириной 16 символов и директива: <c>?CO?FONT_6X8    SEGMENT CODE</c>.</summary>
    protected static string Labeled(string label, string rest) =>
        (label.Length < LabelWidth ? label.PadRight(LabelWidth) : label + " ") + rest;

    protected sealed override OutputDocument Generate(OutputData data, OutputOptions options, List<Diagnostic> diagnostics)
    {
        IReadOnlyList<string> header = HeaderCommentBuilder.Build(data, options, diagnostics);
        string label = options.ArrayName.ToUpperInvariant();
        int[] starts = new int[data.TotalBytes];
        int literalLength = options.AsmNumberFormat == AsmNumberFormat.Binary ? 9 : 4;

        var text = new TextBuilder(data.TotalBytes * (literalLength + 2) + 2048);
        foreach (string line in header)
        {
            text.Line("; " + line);
        }

        text.Line();
        WritePrologue(text, label);
        text.Line(label + ":");

        switch (data)
        {
            case ImageOutputData image:
                for (int f = 0; f < image.FrameCount; f++)
                {
                    if (image.AllFrames)
                    {
                        text.Line($"{DirectiveIndent}; кадр {(f + 1).ToString(CultureInfo.InvariantCulture)}");
                    }

                    ReadOnlySpan<byte> frame = image.GetFrame(f);
                    int index = 0;
                    foreach (int length in DataLayout.SplitImage(frame.Length, options.BytesPerLine))
                    {
                        WriteDb(text, frame.Slice(index, length), f * image.FrameSize + index, options.AsmNumberFormat, starts);
                        text.EndLine();
                        index += length;
                    }
                }

                break;

            case FontOutputData font:
                int[] lineLengths = DataLayout.SplitGlyph(font.BytesPerChar);
                for (int code = 0; code < FontOutputData.CharCount; code++)
                {
                    ReadOnlySpan<byte> glyph = font.GetGlyph(code);
                    int index = 0;
                    for (int line = 0; line < lineLengths.Length; line++)
                    {
                        WriteDb(text, glyph.Slice(index, lineLengths[line]), code * font.BytesPerChar + index, options.AsmNumberFormat, starts);
                        if (line == 0)
                        {
                            text.Append("  " + GlyphCommentFormatter.Asm((byte)code));
                        }

                        text.EndLine();
                        index += lineLengths[line];
                    }
                }

                break;

            default:
                throw new ArgumentException($"Unsupported output data {data.GetType().Name}.", nameof(data));
        }

        WriteEpilogue(text);

        string content = text.ToString();
        string extension = options.AsmFileExtension switch
        {
            AsmFileExtension.A51 => ".a51",
            AsmFileExtension.Asm => ".asm",
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.AsmFileExtension, "Unknown assembler extension."),
        };
        var file = new OutputFile(options.ArrayName + extension, OutputFileKind.Assembly, content, OutputEncoder.Encode(content, options.Encoding));
        return new OutputDocument(new[] { file }, new ByteSpanMap(0, starts, literalLength), diagnostics);
    }

    private static void WriteDb(TextBuilder text, ReadOnlySpan<byte> values, int firstIndex, AsmNumberFormat format, int[] starts)
    {
        text.Append(DirectiveIndent + "DB".PadRight(DirectiveWidth));
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                text.Append(", ");
            }

            starts[firstIndex + i] = text.Position;
            text.Append(NumberFormatter.Asm(values[i], format));
        }
    }
}

/// <summary>Самостоятельный модуль A51: <c>PUBLIC</c>, <c>?CO?ИМЯ SEGMENT CODE</c>, <c>RSEG</c>, <c>END</c> (пример В.2).</summary>
public sealed class A51ModuleGenerator : A51GeneratorBase
{
    public override OutputFormat Format => OutputFormat.A51Module;

    private protected override void WritePrologue(TextBuilder text, string label)
    {
        string segment = "?CO?" + label;
        text.Line(Directive("PUBLIC", label));
        text.Line();
        text.Line(Labeled(segment, "SEGMENT CODE"));
        text.Line(Directive("RSEG", segment));
        text.Line();
    }

    private protected override void WriteEpilogue(TextBuilder text) => text.Line(DirectiveIndent + "END");
}

/// <summary>
/// Фрагмент для <c>$INCLUDE</c>: заголовок-комментарий, метка и строки <c>DB</c> — без <c>PUBLIC</c>,
/// сегмента и <c>END</c> (пример В.3, решение N-03).
/// </summary>
public sealed class A51IncludeGenerator : A51GeneratorBase
{
    public override OutputFormat Format => OutputFormat.A51Include;

    private protected override void WritePrologue(TextBuilder text, string label)
    {
    }

    private protected override void WriteEpilogue(TextBuilder text)
    {
    }
}
