namespace Image2Gdram.Core.Presets;

/// <summary>Почему операция с пресетом отклонена. Текст для пользователя берётся из словаря интерфейса по этому коду.</summary>
public enum PresetError
{
    EmptyName,
    InvalidName,
    InvalidController,
    InvalidSize,
    InvalidPacking,
    ReservedName,
    DuplicateName,
    BuiltIn,
    NotFound,
}

/// <summary>Ошибка каталога или пользовательского пресета (п. 4.5 ТЗ, решение N-26).</summary>
public sealed class PresetException : Exception
{
    public PresetException(PresetError error, string? name = null, Exception? innerException = null)
        : base(name is null ? $"Preset error {error}." : $"Preset error {error} for '{name}'.", innerException)
    {
        Error = error;
        Name = name;
    }

    public PresetError Error { get; }

    public string? Name { get; }
}
