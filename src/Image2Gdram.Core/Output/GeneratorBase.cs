using System.Globalization;
using Image2Gdram.Core.Diagnostics;

namespace Image2Gdram.Core.Output;

/// <summary>Общие проверки генераторов: имя массива, байт в строке, предупреждение о 64 КБ для C51/A51.</summary>
public abstract class GeneratorBase : IOutputGenerator
{
    /// <summary>Наибольший размер массива без предупреждения для C51/A51 (решение D-12).</summary>
    public const int C51MaxArrayBytes = 65535;

    public abstract OutputFormat Format { get; }

    /// <summary>Цель — Keil C51/A51 (адресное пространство кода 64 КБ).</summary>
    protected abstract bool TargetsC51 { get; }

    public OutputDocument Generate(OutputData data, OutputOptions options)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(options);
        NameValidationResult name = NameValidator.Validate(options.ArrayName, Format);
        if (!name.IsValid)
        {
            throw new ArgumentException($"Invalid array name '{options.ArrayName}': {name.Error}.", nameof(options));
        }

        DataLayout.ValidateBytesPerLine(options.BytesPerLine);

        var diagnostics = new List<Diagnostic>();
        if (TargetsC51 && data.TotalBytes > C51MaxArrayBytes)
        {
            diagnostics.Add(new Diagnostic(
                DiagnosticCode.ArrayExceeds64KForC51,
                DiagnosticSeverity.Warning,
                new[] { data.TotalBytes.ToString(CultureInfo.InvariantCulture) }));
        }

        return Generate(data, options, diagnostics);
    }

    /// <param name="diagnostics">Уже найденные предупреждения; генератор дополняет список и передаёт его в документ.</param>
    protected abstract OutputDocument Generate(OutputData data, OutputOptions options, List<Diagnostic> diagnostics);
}
