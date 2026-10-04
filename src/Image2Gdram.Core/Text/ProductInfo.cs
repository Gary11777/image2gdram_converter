namespace Image2Gdram.Core.Text;

/// <summary>Наименование и версия программы (п. 1.1 ТЗ, решение D-02) — единственное место, где они заданы.</summary>
public static class ProductInfo
{
    public const string Name = "Image2GDRAM Converter";

    public const string Version = "1.0";

    /// <summary>Строка для заголовка-комментария генерируемых файлов.</summary>
    public const string NameWithVersion = Name + " " + Version;
}
