using System.Globalization;
using System.Text;

namespace Image2Gdram.Core.Output;

public enum ArrayNameKind
{
    Image,
    Font,
}

/// <summary>
/// Имя массива по умолчанию (п. 4.4.2 ТЗ, решение D-13): латиница в нижнем регистре, прочие символы → <c>_</c>,
/// повторы <c>_</c> схлопываются, крайние удаляются; пустой результат — <c>image</c> или <c>font</c>;
/// начинается с цифры — префикс <c>img_</c>; затем суффикс размера; обрезка с сохранением суффикса.
/// </summary>
public static class DefaultNameBuilder
{
    /// <param name="baseName">Имя файла без расширения или гарнитура; <c>null</c> — нет источника.</param>
    public static string Build(string? baseName, int width, int height, ArrayNameKind kind, int maxLength = NameValidator.MaxLength)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 1);

        string suffix = "_" + width.ToString(CultureInfo.InvariantCulture) + "x" + height.ToString(CultureInfo.InvariantCulture);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLength, suffix.Length + 1);

        string stem = Normalize(baseName ?? string.Empty);
        if (stem.Length == 0)
        {
            stem = kind == ArrayNameKind.Font ? "font" : "image";
        }

        if (stem[0] is >= '0' and <= '9')
        {
            stem = "img_" + stem;
        }

        int room = maxLength - suffix.Length;
        if (stem.Length > room)
        {
            stem = stem[..room].TrimEnd('_');
        }

        return stem + suffix;
    }

    /// <summary>Имя по пути к файлу: каталог и расширение отбрасываются.</summary>
    public static string FromFile(string path, int width, int height, ArrayNameKind kind, int maxLength = NameValidator.MaxLength) =>
        Build(Path.GetFileNameWithoutExtension(path), width, height, kind, maxLength);

    private static string Normalize(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            char mapped = c switch
            {
                >= 'a' and <= 'z' or >= '0' and <= '9' => c,
                >= 'A' and <= 'Z' => (char)(c + ('a' - 'A')),
                _ => '_',
            };
            if (mapped == '_' && (sb.Length == 0 || sb[^1] == '_'))
            {
                continue;
            }

            sb.Append(mapped);
        }

        return sb.ToString().TrimEnd('_');
    }
}
