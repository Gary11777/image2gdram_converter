using System.Globalization;
using System.Text;
using Image2Gdram.Core.Diagnostics;
using Image2Gdram.Core.Packing;
using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Output;

/// <summary>
/// Строки заголовка-комментария (п. 4.4.3 ТЗ, решения D-02, D-03, D-04, N-02, N-04) без синтаксиса комментария.
/// Тексты этого класса — часть формата вывода, а не строки интерфейса.
/// </summary>
public static class HeaderCommentBuilder
{
    public const string DateFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// Строки заголовка. Значения, заданные пользователем (имя файла, гарнитура, пресет), очищаются:
    /// символы вне CP1251, управляющие символы и «/» после «*» заменяются на <c>?</c> с предупреждением в
    /// <paramref name="diagnostics"/>. Поэтому текст заголовка одинаков в CP1251 и UTF-8 и не ломает комментарий C.
    /// </summary>
    public static IReadOnlyList<string> Build(OutputData data, OutputOptions options, ICollection<Diagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(diagnostics);

        var lines = new List<string> { ProductInfo.NameWithVersion };
        if (options.IncludeDate)
        {
            DateTime at = options.GeneratedAt ?? DateTime.Now;
            lines.Add("Дата: " + at.ToString(DateFormat, CultureInfo.InvariantCulture));
        }

        switch (data)
        {
            case ImageOutputData image:
                string source = DescribeSource(image, diagnostics);
                lines.Add($"Источник: {source}; пресет: {DescribePreset(data.Preset, diagnostics)}");
                lines.Add($"Размер: {Size(image.Width, image.Height)} (ШxВ); упаковка: {DescribePacking(image.Packing, image.Height)}");
                lines.Add("Размер массива: " + (image.AllFrames
                    ? $"{RussianPlural.Frames(image.FrameCount)} x {RussianPlural.Bytes(image.FrameSize)} = {RussianPlural.Bytes(image.TotalBytes)}"
                    : RussianPlural.Bytes(image.TotalBytes)));
                break;

            case FontOutputData font:
                lines.Add($"Шрифт: {DescribeSource(font.Source, diagnostics)}; ячейка {Size(font.CellWidth, font.CellHeight)} (ШxВ)");
                lines.Add($"Упаковка: {DescribePacking(font.Packing, font.CellHeight)}; пресет: {DescribePreset(data.Preset, diagnostics)}");
                lines.Add($"Размер: {RussianPlural.Symbols(FontOutputData.CharCount)} x {RussianPlural.Bytes(font.BytesPerChar)} = {RussianPlural.Bytes(font.TotalBytes)}");
                break;

            default:
                throw new ArgumentException($"Unsupported output data {data.GetType().Name}.", nameof(data));
        }

        return lines;
    }

    /// <summary>
    /// Параметры упаковки: «вертикальная, LSB first, по страницам, инверсия: нет». Число бит в байте — только
    /// в горизонтальном режиме, порядок обхода — только в вертикальном при высоте больше 8 (решение N-02).
    /// </summary>
    public static string DescribePacking(PackingOptions packing, int height)
    {
        ArgumentNullException.ThrowIfNull(packing);
        var parts = new List<string>
        {
            packing.Direction switch
            {
                PackDirection.Horizontal => "горизонтальная",
                PackDirection.Vertical => "вертикальная",
                _ => throw new ArgumentOutOfRangeException(nameof(packing), packing.Direction, "Unknown direction."),
            },
            packing.BitOrder switch
            {
                BitOrder.LsbFirst => "LSB first",
                BitOrder.MsbFirst => "MSB first",
                _ => throw new ArgumentOutOfRangeException(nameof(packing), packing.BitOrder, "Unknown bit order."),
            },
        };

        if (packing.Direction == PackDirection.Horizontal)
        {
            parts.Add(packing.BitsPerByte.ToString(CultureInfo.InvariantCulture) + " бит в байте");
        }
        else if (height > 8)
        {
            parts.Add(packing.PageTraversal switch
            {
                PageTraversal.ByPages => "по страницам",
                PageTraversal.ByColumns => "по столбцам",
                _ => throw new ArgumentOutOfRangeException(nameof(packing), packing.PageTraversal, "Unknown traversal."),
            });
        }

        parts.Add("инверсия: " + (packing.Invert ? "да" : "нет"));
        return string.Join(", ", parts);
    }

    /// <summary>
    /// Заменяет на <c>?</c> символы, которых нет в CP1251, управляющие символы и «/» сразу после «*».
    /// Возвращает <c>true</c>, если что-то заменено.
    /// </summary>
    public static bool TrySanitize(string value, out string sanitized)
    {
        ArgumentNullException.ThrowIfNull(value);
        var sb = new StringBuilder(value.Length);
        bool replaced = false;
        foreach (char c in value)
        {
            bool ok = !char.IsControl(c)
                && Cp1251.TryFromUnicode(c, out _)
                && !(c == '/' && sb.Length > 0 && sb[^1] == '*');
            sb.Append(ok ? c : '?');
            replaced |= !ok;
        }

        sanitized = sb.ToString();
        return replaced;
    }

    private static string DescribeSource(ImageOutputData image, ICollection<Diagnostic> diagnostics)
    {
        string name = Clean(image.Source.FileName, HeaderField.SourceFile, diagnostics);
        if (image.AllFrames)
        {
            return $"{name}, все кадры ({image.FrameCount.ToString(CultureInfo.InvariantCulture)})";
        }

        return image.Source.SourceFrameCount > 1
            ? $"{name}, кадр {image.Source.SelectedFrame.ToString(CultureInfo.InvariantCulture)}"
            : name;
    }

    private static string DescribeSource(FontSourceInfo source, ICollection<Diagnostic> diagnostics) => source.Kind switch
    {
        FontSourceKind.TrueType =>
            $"{Clean(source.Family!, HeaderField.FontFamily, diagnostics)}, {source.SizePx.ToString(CultureInfo.InvariantCulture)} px"
            + (source.Bold ? ", жирный" : string.Empty)
            + (source.Italic ? ", курсив" : string.Empty),
        FontSourceKind.Sheet => "лист " + Clean(source.FileName!, HeaderField.SourceFile, diagnostics),
        FontSourceKind.Import => "импорт из " + Clean(source.FileName!, HeaderField.SourceFile, diagnostics)
            + (source.ArrayName is null ? string.Empty : $" (массив {Clean(source.ArrayName, HeaderField.ImportedArrayName, diagnostics)})"),
        FontSourceKind.Manual => "ручное рисование",
        _ => throw new ArgumentOutOfRangeException(nameof(source), source.Kind, "Unknown font source."),
    };

    private static string DescribePreset(PresetInfo preset, ICollection<Diagnostic> diagnostics)
    {
        if (preset.Name is { } name)
        {
            string text = preset.Controller is null ? name : $"{name} ({preset.Controller})";
            return Clean(text, HeaderField.Preset, diagnostics);
        }

        return preset.BasedOn is { } basedOn
            ? $"Пользовательский (на основе {Clean(basedOn, HeaderField.Preset, diagnostics)})"
            : "Пользовательский";
    }

    private static string Clean(string value, HeaderField field, ICollection<Diagnostic> diagnostics)
    {
        if (!TrySanitize(value, out string sanitized))
        {
            return sanitized;
        }

        diagnostics.Add(new Diagnostic(
            DiagnosticCode.NonCp1251CharactersReplaced,
            DiagnosticSeverity.Warning,
            new[] { field.ToString(), value }));
        return sanitized;
    }

    private static string Size(int width, int height) =>
        width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);
}
