using System.Diagnostics.CodeAnalysis;

namespace Image2Gdram.Core.Packing;

/// <summary>
/// Реестр упаковщиков по формату пикселя (п. 4.6 ТЗ). Заполняется при старте; после этого только читается,
/// поэтому безопасен для одновременного чтения из нескольких потоков.
/// </summary>
public sealed class PackerRegistry
{
    private readonly Dictionary<PixelFormat, IPacker> _packers = new();

    /// <summary>Реестр со всеми упаковщиками версии 1.0 (только <see cref="PixelFormat.Mono1bpp"/>).</summary>
    public static PackerRegistry CreateDefault()
    {
        var registry = new PackerRegistry();
        registry.Register(new Mono1bppPacker());
        return registry;
    }

    /// <summary>Зарегистрированные форматы в порядке значений перечисления.</summary>
    public IReadOnlyList<PixelFormat> Formats => _packers.Keys.Order().ToArray();

    public void Register(IPacker packer)
    {
        ArgumentNullException.ThrowIfNull(packer);
        if (!_packers.TryAdd(packer.Format, packer))
        {
            throw new InvalidOperationException($"A packer for {packer.Format} is already registered.");
        }
    }

    public bool TryGet(PixelFormat format, [NotNullWhen(true)] out IPacker? packer) =>
        _packers.TryGetValue(format, out packer);

    public IPacker Get(PixelFormat format) =>
        TryGet(format, out IPacker? packer) ? packer : throw new PackerNotRegisteredException(format);

    public IPacker Get(PackingOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return Get(options.PixelFormat);
    }
}

/// <summary>Для формата пикселя нет упаковщика (например, <see cref="PixelFormat.Rgb565"/> в версии 1.0).</summary>
public sealed class PackerNotRegisteredException : NotSupportedException
{
    public PackerNotRegisteredException(PixelFormat format)
        : base($"No packer is registered for pixel format {format}.")
    {
        Format = format;
    }

    public PixelFormat Format { get; }
}
