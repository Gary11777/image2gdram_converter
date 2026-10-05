namespace Image2Gdram.Core.Output;

/// <summary>
/// Язык C для STM32: <c>const uint8_t</c> (по умолчанию) или <c>const unsigned char</c>, без <c>static</c>
/// (п. 4.4.6 ТЗ, пример В.4).
/// </summary>
public sealed class Stm32CGenerator : CGeneratorBase
{
    public override OutputFormat Format => OutputFormat.CStm32;

    protected override bool TargetsC51 => false;

    protected override string Declare(string declarator, OutputOptions options) => options.Stm32ElementType switch
    {
        Stm32ElementType.Uint8T => "const uint8_t " + declarator,
        Stm32ElementType.UnsignedChar => "const unsigned char " + declarator,
        _ => throw new ArgumentOutOfRangeException(nameof(options), options.Stm32ElementType, "Unknown element type."),
    };

    protected override bool NeedsStdint(OutputOptions options) => options.Stm32ElementType == Stm32ElementType.Uint8T;
}
