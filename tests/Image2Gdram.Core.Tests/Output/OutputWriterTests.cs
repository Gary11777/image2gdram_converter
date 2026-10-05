using Image2Gdram.Core.Output;
using static Image2Gdram.Core.Tests.Output.OutputTestData;

namespace Image2Gdram.Core.Tests.Output;

/// <summary>Сохранение файлов: пара .c/.h, подтверждение перезаписи (п. 4.4.1 ТЗ), защита исходников (N-18).</summary>
public sealed class OutputWriterTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g_writer_" + Guid.NewGuid().ToString("N"));

    public OutputWriterTests()
    {
        Directory.CreateDirectory(_directory);
    }

    public void Dispose()
    {
        Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void C_output_is_saved_as_c_and_h_pair()
    {
        OutputDocument document = Generate(Logo128x64(), OutputFormat.CStm32, "logo_128x64");
        int confirmations = 0;

        OutputSaveResult result = OutputWriter.Save(document, _directory, _ => { confirmations++; return true; });

        Assert.Equal(OutputSaveResult.Saved, result);
        Assert.Equal(0, confirmations);
        Assert.Equal(document.Files[0].Content, File.ReadAllBytes(Path.Combine(_directory, "logo_128x64.c")));
        Assert.Equal(document.Files[1].Content, File.ReadAllBytes(Path.Combine(_directory, "logo_128x64.h")));
        Assert.Equal(new[] { "logo_128x64.c", "logo_128x64.h" }, Directory.GetFiles(_directory).Select(Path.GetFileName).Order());
    }

    [Fact]
    public void Existing_files_require_confirmation_and_refusal_keeps_them()
    {
        string h = Path.Combine(_directory, "logo.h");
        File.WriteAllText(h, "old");
        OutputDocument document = Generate(Logo128x64(), OutputFormat.CKeilC51, "logo");
        IReadOnlyList<string>? asked = null;

        OutputSaveResult result = OutputWriter.Save(document, _directory, existing => { asked = existing; return false; });

        Assert.Equal(OutputSaveResult.Cancelled, result);
        Assert.Equal(new[] { Path.GetFullPath(h) }, asked);
        Assert.Equal("old", File.ReadAllText(h));
        Assert.False(File.Exists(Path.Combine(_directory, "logo.c")));

        Assert.Equal(OutputSaveResult.Saved, OutputWriter.Save(document, _directory, _ => true));
        Assert.Equal(document.Files[1].Content, File.ReadAllBytes(h));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void Source_file_is_never_overwritten()
    {
        string source = Path.Combine(_directory, "font.c");
        File.WriteAllText(source, "unsigned char font[] = { 1 };");
        OutputDocument document = Generate(Font(6, 8), OutputFormat.CKeilC51, "font");

        var error = Assert.Throws<OutputWriteException>(() => OutputWriter.Save(document, _directory, _ => true, new[] { source.ToUpperInvariant() }));

        Assert.Equal(OutputWriteError.WouldOverwriteSource, error.Error);
        Assert.Equal("unsigned char font[] = { 1 };", File.ReadAllText(source));
        Assert.False(File.Exists(Path.Combine(_directory, "font.h")));
    }

    [Fact]
    public void Missing_directory_is_an_io_error()
    {
        OutputDocument document = Generate(Logo128x64(), OutputFormat.Bin, "logo");

        var error = Assert.Throws<OutputWriteException>(() => OutputWriter.Save(document, Path.Combine(_directory, "missing"), _ => true));

        Assert.Equal(OutputWriteError.IoError, error.Error);
    }

    [Fact]
    public void Target_paths_follow_document_file_names()
    {
        OutputDocument document = Registry.Generate(Logo128x64(), Options(OutputFormat.A51Module, "logo") with { AsmFileExtension = AsmFileExtension.Asm });

        Assert.Equal(new[] { Path.Combine(Path.GetFullPath(_directory), "logo.asm") }, OutputWriter.GetTargetPaths(document, _directory));
    }
}
