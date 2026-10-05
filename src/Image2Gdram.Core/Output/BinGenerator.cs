using Image2Gdram.Core.Diagnostics;

namespace Image2Gdram.Core.Output;

/// <summary>
/// Двоичный файл: только байты массива, без заголовков; кадры GIF подряд (п. 4.4.1 ТЗ, решение D-11).
/// Для окна кода — шестнадцатеричный дамп: смещение и 16 байт в строке (решение N-17).
/// </summary>
public sealed class BinGenerator : GeneratorBase
{
    public const int DumpBytesPerLine = 16;

    public override OutputFormat Format => OutputFormat.Bin;

    protected override bool TargetsC51 => false;

    protected override OutputDocument Generate(OutputData data, OutputOptions options, List<Diagnostic> diagnostics)
    {
        byte[] bytes = data.GetAllBytes();
        int[] starts = new int[bytes.Length];
        var dump = new TextBuilder(bytes.Length * 3 + (bytes.Length / DumpBytesPerLine + 1) * 12);
        for (int offset = 0; offset < bytes.Length; offset += DumpBytesPerLine)
        {
            dump.Append(offset.ToString("X8", System.Globalization.CultureInfo.InvariantCulture)).Append(" ");
            int end = Math.Min(offset + DumpBytesPerLine, bytes.Length);
            for (int i = offset; i < end; i++)
            {
                dump.Append(" ");
                starts[i] = dump.Position;
                dump.Append(bytes[i].ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            }

            dump.EndLine();
        }

        var file = new OutputFile(options.ArrayName + ".bin", OutputFileKind.Binary, dump.ToString(), bytes);
        return new OutputDocument(new[] { file }, new ByteSpanMap(0, starts, 2), diagnostics);
    }
}
