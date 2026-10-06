namespace Image2Gdram.Core.Presets;

/// <summary>
/// Пользовательские пресеты (п. 4.5 ТЗ, решение N-26). Встроенные сюда не входят: их нельзя изменить и удалить.
/// Имя уникально без учёта регистра и не совпадает со встроенными и со словом «Пользовательский».
/// Повторное сохранение под тем же именем не перезаписывает пресет — его сначала удаляют или переименовывают.
/// </summary>
public sealed class UserPresetStore
{
    private readonly PresetCatalog _catalog;
    private readonly List<Preset> _items = new();

    public UserPresetStore(PresetCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        _catalog = catalog;
    }

    public IReadOnlyList<Preset> Presets => _items;

    public Preset? Find(string? name)
    {
        int index = IndexOf(name);
        return index < 0 ? null : _items[index];
    }

    public void Add(Preset preset)
    {
        ArgumentNullException.ThrowIfNull(preset);
        if (preset.IsBuiltIn)
        {
            throw new PresetException(PresetError.BuiltIn, preset.Name);
        }

        EnsureAvailable(preset.Name, ignoreIndex: -1);
        _items.Add(preset);
    }

    public void Rename(string name, string newName)
    {
        int index = RequireIndex(name);
        Preset updated = _items[index].WithName(newName);
        EnsureAvailable(updated.Name, index);
        _items[index] = updated;
    }

    public void Remove(string name)
    {
        int index = RequireIndex(name);
        _items.RemoveAt(index);
    }

    private int RequireIndex(string name)
    {
        RejectReserved(name);
        int index = IndexOf(name);
        if (index < 0)
        {
            throw new PresetException(PresetError.NotFound, string.IsNullOrWhiteSpace(name) ? name : name.Trim());
        }

        return index;
    }

    private void EnsureAvailable(string name, int ignoreIndex)
    {
        RejectReserved(name);
        for (int i = 0; i < _items.Count; i++)
        {
            if (i != ignoreIndex && SameName(_items[i].Name, name))
            {
                throw new PresetException(PresetError.DuplicateName, name);
            }
        }
    }

    private void RejectReserved(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new PresetException(PresetError.EmptyName, name);
        }

        string trimmed = name.Trim();
        if (_catalog.IsBuiltInName(trimmed))
        {
            throw new PresetException(PresetError.BuiltIn, trimmed);
        }

        if (_catalog.IsReservedName(trimmed))
        {
            throw new PresetException(PresetError.ReservedName, trimmed);
        }
    }

    private int IndexOf(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return -1;
        }

        string trimmed = name.Trim();
        for (int i = 0; i < _items.Count; i++)
        {
            if (SameName(_items[i].Name, trimmed))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool SameName(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
