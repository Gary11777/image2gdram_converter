namespace image2gdram_converter;

/// <summary>Значение списка и подпись из словаря строк.</summary>
public sealed class Labeled<T>
    where T : notnull
{
    public Labeled(T value, string caption)
    {
        Value = value;
        Caption = caption;
    }

    public T Value { get; }

    public string Caption { get; }
}
