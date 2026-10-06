using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Image2Gdram.Core.Presets;
using image2gdram_converter.Services;

namespace image2gdram_converter.ViewModels;

/// <summary>Список пресетов и пункт «Пользовательский (на основе …)» (решение N-26).</summary>
public sealed partial class PresetSelectorViewModel : ObservableObject
{
    private readonly ILocalizationService _text;
    private readonly PresetCatalog _catalog;
    private readonly UserPresetStore _users;
    private readonly Action<Preset?> _chosen;
    private bool _suppress;
    private PresetBinding _binding = PresetBinding.Custom;

    public PresetSelectorViewModel(
        ILocalizationService text,
        PresetCatalog catalog,
        UserPresetStore users,
        Action<Preset?> chosen)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(catalog);
        ArgumentNullException.ThrowIfNull(users);
        ArgumentNullException.ThrowIfNull(chosen);
        _text = text;
        _catalog = catalog;
        _users = users;
        _chosen = chosen;
        Show(PresetBinding.Custom);
    }

    public ObservableCollection<PresetChoice> Items { get; } = new();

    public string Caption { get; private set; } = string.Empty;

    public PresetBinding Binding => _binding;

    public bool CanRename => Selected is { IsUser: true };

    public event Action? SelectionChanged;

    [ObservableProperty]
    private PresetChoice? _selected;

    public void Show(PresetBinding binding)
    {
        ArgumentNullException.ThrowIfNull(binding);
        _binding = binding;
        Caption = MakeCaption(binding);
        OnPropertyChanged(nameof(Caption));

        _suppress = true;
        Items.Clear();
        foreach (Preset preset in _catalog.BuiltIn)
        {
            Items.Add(new PresetChoice(preset, preset.Name, isUser: false, isCustom: false));
        }

        foreach (Preset preset in _users.Presets)
        {
            Items.Add(new PresetChoice(preset, preset.Name, isUser: true, isCustom: false));
        }

        string customCaption = binding.IsCustom ? Caption : _text.Get("Preset.Custom");
        var custom = new PresetChoice(null, customCaption, isUser: false, isCustom: true);
        Items.Add(custom);
        Selected = binding.IsCustom
            ? custom
            : Items.FirstOrDefault(item => item.Preset is not null && Same(item.Preset.Name, binding.Name)) ?? custom;
        _suppress = false;
        OnPropertyChanged(nameof(CanRename));
        SelectionChanged?.Invoke();
    }

    partial void OnSelectedChanged(PresetChoice? value)
    {
        if (_suppress || value is null)
        {
            return;
        }

        OnPropertyChanged(nameof(CanRename));
        SelectionChanged?.Invoke();
        _chosen(value.IsCustom ? null : value.Preset);
    }

    private string MakeCaption(PresetBinding binding)
    {
        if (!binding.IsCustom)
        {
            return binding.Name ?? _text.Get("Preset.Custom");
        }

        return string.IsNullOrWhiteSpace(binding.BasedOn)
            ? _text.Get("Preset.Custom")
            : _text.Format("Preset.CustomBasedOn", binding.BasedOn);
    }

    private static bool Same(string left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}
