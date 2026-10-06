using Image2Gdram.Core.Fonts;
using Image2Gdram.Core.Projects;
using Image2Gdram.Core.Settings;

namespace image2gdram_converter.ViewModels;

/// <summary>
/// Вкладка шрифтов без редактора: параметры и таблица хранятся, чтобы проект их не терял.
/// Редактор — этап 7.
/// </summary>
public sealed class FontSession
{
    public FontTabParameters Parameters { get; set; } = FontTabParameters.CreateDefault();

    public FontTable Table { get; set; } = new(FontCellSize.Cell6x8);

    public string? SheetPath { get; set; }

    public string? ImportPath { get; set; }

    public bool HasCustomTable()
    {
        for (int code = 0; code < FontTable.CharCount; code++)
        {
            if (Table.GetOrigin(code) != GlyphOrigin.None)
            {
                return true;
            }
        }

        return false;
    }

    public void ResetTable()
    {
        Table = new FontTable(Table.Cell);
        SheetPath = null;
        ImportPath = null;
    }

    public FontProjectTab ToProject() => new(Parameters, Table.Clone(), SheetPath, ImportPath);

    public FontSettingsTab ToSettings() => new()
    {
        Parameters = Parameters,
        SheetPath = SheetPath,
        ImportPath = ImportPath,
    };

    public void Load(FontProjectTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        Parameters = tab.Parameters;
        Table = tab.Table;
        SheetPath = tab.SheetPath;
        ImportPath = tab.ImportPath;
    }

    public static FontSession From(FontSettingsTab tab)
    {
        ArgumentNullException.ThrowIfNull(tab);
        return new FontSession
        {
            Parameters = tab.Parameters,
            SheetPath = tab.SheetPath,
            ImportPath = tab.ImportPath,
        };
    }
}
