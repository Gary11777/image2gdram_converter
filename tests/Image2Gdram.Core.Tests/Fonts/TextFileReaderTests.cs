using System.Security.Cryptography;
using System.Text;
using Image2Gdram.Core.Fonts.Import;

namespace Image2Gdram.Core.Tests.Fonts;

/// <summary>Кодировка и чтение файла импорта (решения D-17, N-18, N-21).</summary>
public sealed class TextFileReaderTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "i2g-import-" + Guid.NewGuid().ToString("N"));

    public TextFileReaderTests() => Directory.CreateDirectory(_directory);

    public void Dispose()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void Decode_prefers_strict_utf8_and_falls_back_to_cp1251()
    {
        DecodedText bom = TextFileReader.Decode(new byte[] { 0xEF, 0xBB, 0xBF, (byte)'A' });
        Assert.Equal("A", bom.Text);
        Assert.Equal(TextFileEncoding.Utf8, bom.Encoding);

        DecodedText utf8 = TextFileReader.Decode(Encoding.UTF8.GetBytes("\u0410"));
        Assert.Equal("\u0410", utf8.Text);
        Assert.Equal(TextFileEncoding.Utf8, utf8.Encoding);

        DecodedText ascii = TextFileReader.Decode("OK"u8);
        Assert.Equal("OK", ascii.Text);
        Assert.Equal(TextFileEncoding.Utf8, ascii.Encoding);

        DecodedText empty = TextFileReader.Decode(ReadOnlySpan<byte>.Empty);
        Assert.Equal(string.Empty, empty.Text);
        Assert.Equal(TextFileEncoding.Utf8, empty.Encoding);

        DecodedText cyrillic = TextFileReader.Decode(new byte[] { 0xC0 });
        Assert.Equal("\u0410", cyrillic.Text);
        Assert.Equal(TextFileEncoding.Cp1251, cyrillic.Encoding);

        DecodedText mixed = TextFileReader.Decode(new byte[] { 0xC0, 0x41 });
        Assert.Equal("\u0410A", mixed.Text);
        Assert.Equal(TextFileEncoding.Cp1251, mixed.Encoding);

        DecodedText unassigned = TextFileReader.Decode(new byte[] { 0x98 });
        Assert.Equal("\u0098", unassigned.Text);
        Assert.Equal(TextFileEncoding.Cp1251, unassigned.Encoding);

        DecodedText ya = TextFileReader.Decode(new byte[] { 0xFF });
        Assert.Equal("\u044F", ya.Text);
        Assert.Equal(TextFileEncoding.Cp1251, ya.Encoding);
    }

    [Fact]
    public void Read_round_trips_utf8_and_cp1251_without_changing_or_locking_the_file()
    {
        string utf8Path = Path.Combine(_directory, "font.c");
        File.WriteAllBytes(utf8Path, new byte[] { 0xEF, 0xBB, 0xBF, (byte)'A', (byte)'B' });
        AssertSameAfterRead(utf8Path, "AB", TextFileEncoding.Utf8);

        string cp1251Path = Path.Combine(_directory, "font.a51");
        File.WriteAllBytes(cp1251Path, new byte[] { 0xC0, 0x98 });
        DecodedText cp1251 = AssertSameAfterRead(cp1251Path, "\u0410\u0098", TextFileEncoding.Cp1251);
        Assert.Equal("\u0410\u0098", cp1251.Text);

        string emptyPath = Path.Combine(_directory, "empty.h");
        File.WriteAllBytes(emptyPath, Array.Empty<byte>());
        AssertSameAfterRead(emptyPath, string.Empty, TextFileEncoding.Utf8);
    }

    [Fact]
    public void File_larger_than_16_megabytes_is_rejected_before_its_bytes_are_required()
    {
        string path = Path.Combine(_directory, "huge.inc");
        using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
        {
            stream.SetLength(TextFileReader.MaxFileSize + 1);
        }

        ArrayImportException ex = Assert.Throws<ArrayImportException>(() => TextFileReader.Read(path));
        Assert.Equal(ArrayImportErrorKind.FileTooLarge, ex.Kind);
        Assert.Equal(0, ex.Line);
    }

    [Fact]
    public void Missing_file_is_an_io_error()
    {
        ArrayImportException ex = Assert.Throws<ArrayImportException>(() => TextFileReader.Read(Path.Combine(_directory, "missing.c")));
        Assert.Equal(ArrayImportErrorKind.IoError, ex.Kind);
        Assert.Equal(0, ex.Line);
    }

    [Theory]
    [InlineData("font.c", ImportSyntax.C)]
    [InlineData("font.C", ImportSyntax.C)]
    [InlineData("font.h", ImportSyntax.C)]
    [InlineData("font.asm", ImportSyntax.Asm)]
    [InlineData("font.a51", ImportSyntax.Asm)]
    [InlineData("font.inc", ImportSyntax.Asm)]
    [InlineData(@"D:\data\FONT.A51", ImportSyntax.Asm)]
    public void Extension_selects_the_syntax(string path, ImportSyntax syntax) =>
        Assert.Equal(syntax, TextFileReader.GetSyntax(path));

    [Theory]
    [InlineData("font.txt")]
    [InlineData("font")]
    [InlineData("font.cpp")]
    [InlineData("font.c.backup")]
    public void Other_extensions_are_rejected(string path) =>
        Assert.Throws<ArgumentException>(() => TextFileReader.GetSyntax(path));

    private static DecodedText AssertSameAfterRead(string path, string text, TextFileEncoding encoding)
    {
        byte[] before = File.ReadAllBytes(path);
        string hash = Convert.ToHexString(SHA256.HashData(before));
        DecodedText decoded = TextFileReader.Read(path);
        byte[] after = File.ReadAllBytes(path);

        Assert.Equal(text, decoded.Text);
        Assert.Equal(encoding, decoded.Encoding);
        Assert.Equal(hash, Convert.ToHexString(SHA256.HashData(after)));
        using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
        Assert.Equal(before.Length, stream.Length);
        return decoded;
    }
}
