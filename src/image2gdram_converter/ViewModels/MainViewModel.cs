using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Presets;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;
using image2gdram_converter.Services;

namespace image2gdram_converter.ViewModels;

/// <summary>????: ??????, ????????? ? ??? ???????. ???????? ??????? ???????????? ?? ????? 7.</summary>
public sealed partial class MainViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly AppSettings _settings;
    private readonly ILocalizationService _loc;
    private readonly IDialogService _dialogs;
    private readonly IFileDialogService _files;
    private readonly ISettingsAutosave _autosave;
    private readonly IImageDecoder _decoder;
    private LastFolders _folders;
    private string? _projectPath;
    private bool _dirty;
    private bool _loading = true;
    private string _windowTitle = string.Empty;
    private string _statusText = string.Empty;
    private int _selectedTab;

    public MainViewModel(
        SettingsService settingsService,
        SettingsLoadResult loaded,
        ILocalizationService localization,
        IDialogService dialogs,
        IFileDialogService files,
        IClipboardService clipboard,
        IRecalcScheduler scheduler,
        ISettingsAutosave autosave,
        IImageDecoder decoder)
    {
        ArgumentNullException.ThrowIfNull(settingsService);
        ArgumentNullException.ThrowIfNull(loaded);
        ArgumentNullException.ThrowIfNull(localization);
        ArgumentNullException.ThrowIfNull(dialogs);
        ArgumentNullException.ThrowIfNull(files);
        ArgumentNullException.ThrowIfNull(clipboard);
        ArgumentNullException.ThrowIfNull(scheduler);
        ArgumentNullException.ThrowIfNull(autosave);
        ArgumentNullException.ThrowIfNull(decoder);
        _settingsService = settingsService;
        _settings = loaded.Settings;
        _loc = localization;
        _dialogs = dialogs;
        _files = files;
        _autosave = autosave;
        _decoder = decoder;
        _folders = _settings.Folders;
        Image = new ImageConverterViewModel(
            localization,
            dialogs,
            files,
            clipboard,
            scheduler,
            decoder,
            PresetCatalog.Shared,
            _settings.UserPresets,
            _settings.Image.Parameters);
        Image.SetFolders(_folders.OpenImage, _folders.SaveOutput);
        Image.ParametersChanged = OnParametersChanged;
        Image.EditsChanged = OnEditsChanged;
        Image.FileUsed = Remember;
        Font = FontSession.From(_settings.Font);
        RecentFiles = new ObservableCollection<string>(_settings.RecentFiles);
        if (loaded.Diagnostic is not null)
        {
            StatusText = UserText.Diagnostic(localization, loaded.Diagnostic);
        }

        _loading = false;
        UpdateTitle();
    }

    public ImageConverterViewModel Image { get; }

    public FontSession Font { get; }

    public ObservableCollection<string> RecentFiles { get; }

    public string WindowTitle
    {
        get => _windowTitle;
        private set => SetProperty(ref _windowTitle, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public int SelectedTab
    {
        get => _selectedTab;
        set => SetProperty(ref _selectedTab, value);
    }

    public bool TryClose()
    {
        if (!DiscardOrSave())
        {
            return false;
        }

        _autosave.Cancel();
        SaveSettings();
        return true;
    }

    [RelayCommand]
    private void NewProject()
    {
        if (!DiscardOrSave())
        {
            return;
        }

        _loading = true;
        Image.ResetSession();
        Font.ResetTable();
        _projectPath = null;
        _dirty = false;
        _loading = false;
        UpdateTitle();
        OnParametersChanged();
    }

    [RelayCommand]
    private void OpenProject()
    {
        if (!DiscardOrSave())
        {
            return;
        }

        string? path = _files.PickOpenProject(_folders.OpenProject);
        if (path is not null)
        {
            OpenProjectFile(path);
        }
    }

    [RelayCommand]
    private void SaveProject() => SaveProjectCore(askPath: false);

    [RelayCommand]
    private void SaveProjectAs() => SaveProjectCore(askPath: true);

    [RelayCommand]
    private async Task OpenRecent(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!File.Exists(path))
        {
            _dialogs.Alert(_loc.Format("Error.RecentMissing", path));
            return;
        }

        if (path.EndsWith(".iiu", StringComparison.OrdinalIgnoreCase))
        {
            if (!DiscardOrSave())
            {
                return;
            }

            OpenProjectFile(path);
            return;
        }

        await Image.OpenPath(path).ConfigureAwait(true);
    }

    [RelayCommand]
    private void Exit() => Application.Current?.Shutdown();

    private void OpenProjectFile(string path)
    {
        ProjectLoadResult loaded;
        try
        {
            loaded = ProjectSerializer.Load(path);
        }
        catch (ProjectException ex)
        {
            _dialogs.Alert(UserText.Project(_loc, ex));
            return;
        }

        _loading = true;
        Image.LoadState(loaded.Project.Image.Parameters, schedule: false);
        Image.ReplaceEdits(loaded.Project.Image.Edits);
        Font.Load(loaded.Project.Font);
        if (loaded.Project.Image.SourcePath is string imagePath
            && imagePath.Length > 0
            && File.Exists(imagePath))
        {
            try
            {
                DecodedImage decoded = _decoder.Decode(imagePath);
                Image.LoadDecoded(decoded, imagePath, resetFrame: false, userAction: false);
            }
            catch (ImageLoadException ex)
            {
                Image.UnloadPixels();
                StatusText = UserText.ImageLoad(_loc, ex);
            }
        }
        else
        {
            Image.UnloadPixels();
        }

        _projectPath = Path.GetFullPath(path);
        _dirty = false;
        _loading = false;
        Remember(path, SessionFolder.OpenProject);
        UpdateTitle();
        string notes = string.Join(Environment.NewLine, loaded.Diagnostics.Select(item => UserText.Diagnostic(_loc, item)));
        if (notes.Length > 0)
        {
            StatusText = notes;
        }
    }

    private bool SaveProjectCore(bool askPath)
    {
        string? path = _projectPath;
        if (askPath || string.IsNullOrWhiteSpace(path))
        {
            path = _files.PickSaveProject(_folders.SaveProject);
            if (path is null)
            {
                return false;
            }
        }

        try
        {
            var document = new ProjectDocument(
                new ImageProjectTab(Image.ToParameters(), Image.SourcePath, Image.Edits),
                Font.ToProject());
            ProjectSerializer.Save(document, path);
            _projectPath = Path.GetFullPath(path);
            _dirty = false;
            Remember(path, SessionFolder.SaveProject);
            UpdateTitle();
            return true;
        }
        catch (ProjectException ex)
        {
            _dialogs.Alert(UserText.Project(_loc, ex));
            return false;
        }
    }

    private bool DiscardOrSave()
    {
        if (!NeedsPrompt())
        {
            return true;
        }

        SaveChoice choice = _dialogs.AskSave(_loc.Get("Dialog.Unsaved"));
        return choice switch
        {
            SaveChoice.Cancel => false,
            SaveChoice.Discard => true,
            _ => SaveProjectCore(askPath: false),
        };
    }

    private bool NeedsPrompt() =>
        _dirty && (_projectPath is not null || Image.HasEdits || Font.HasCustomTable());

    private void OnParametersChanged()
    {
        if (_loading)
        {
            return;
        }

        _dirty = true;
        _autosave.Schedule(SaveSettings);
    }

    private void OnEditsChanged()
    {
        if (!_loading)
        {
            _dirty = true;
        }
    }

    private void Remember(string path, SessionFolder folder)
    {
        if (folder != SessionFolder.SaveOutput)
        {
            _settings.RememberRecent(path);
            RecentFiles.Clear();
            foreach (string item in _settings.RecentFiles)
            {
                RecentFiles.Add(item);
            }
        }

        string? directory = folder == SessionFolder.SaveOutput ? path : Path.GetDirectoryName(path);
        _folders = folder switch
        {
            SessionFolder.OpenImage => _folders with { OpenImage = directory },
            SessionFolder.SaveOutput => _folders with { SaveOutput = directory },
            SessionFolder.OpenProject => _folders with { OpenProject = directory },
            _ => _folders with { SaveProject = directory },
        };
        Image.SetFolders(_folders.OpenImage, _folders.SaveOutput);
        if (!_loading)
        {
            _autosave.Schedule(SaveSettings);
        }
    }

    private void SaveSettings()
    {
        _settings.Image = new ImageSettingsTab
        {
            Parameters = Image.ToParameters(),
            SourcePath = Image.SourcePath,
        };
        _settings.Font = Font.ToSettings();
        _settings.Folders = _folders;
        try
        {
            _settingsService.Save(_settings);
        }
        catch (SettingsException ex)
        {
            _dialogs.Alert(UserText.Settings(_loc, ex));
        }
    }

    private void UpdateTitle()
    {
        string product = _loc.Get("Window.Title");
        WindowTitle = _projectPath is null
            ? product
            : _loc.Format("Window.TitleWithProject", product, Path.GetFileName(_projectPath));
    }
}
