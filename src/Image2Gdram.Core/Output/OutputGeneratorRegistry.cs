using System.Diagnostics.CodeAnalysis;

namespace Image2Gdram.Core.Output;

/// <summary>
/// Реестр генераторов по формату вывода (п. 4.6 ТЗ). Заполняется при старте; после этого только читается,
/// поэтому безопасен для одновременного чтения из нескольких потоков.
/// </summary>
public sealed class OutputGeneratorRegistry
{
    private readonly Dictionary<OutputFormat, IOutputGenerator> _generators = new();

    /// <summary>Реестр со всеми генераторами версии 1.0.</summary>
    public static OutputGeneratorRegistry CreateDefault()
    {
        var registry = new OutputGeneratorRegistry();
        registry.Register(new C51CGenerator());
        registry.Register(new Stm32CGenerator());
        registry.Register(new A51ModuleGenerator());
        registry.Register(new A51IncludeGenerator());
        registry.Register(new BinGenerator());
        return registry;
    }

    /// <summary>Зарегистрированные форматы в порядке значений перечисления.</summary>
    public IReadOnlyList<OutputFormat> Formats => _generators.Keys.Order().ToArray();

    public void Register(IOutputGenerator generator)
    {
        ArgumentNullException.ThrowIfNull(generator);
        if (!_generators.TryAdd(generator.Format, generator))
        {
            throw new InvalidOperationException($"A generator for {generator.Format} is already registered.");
        }
    }

    public bool TryGet(OutputFormat format, [NotNullWhen(true)] out IOutputGenerator? generator) =>
        _generators.TryGetValue(format, out generator);

    public IOutputGenerator Get(OutputFormat format) =>
        TryGet(format, out IOutputGenerator? generator)
            ? generator
            : throw new NotSupportedException($"No output generator is registered for {format}.");

    /// <summary>Генерация форматом из <paramref name="options"/>.</summary>
    public OutputDocument Generate(OutputData data, OutputOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Get(options.Format).Generate(data, options);
    }
}
