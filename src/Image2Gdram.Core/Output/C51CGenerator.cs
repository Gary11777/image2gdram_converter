namespace Image2Gdram.Core.Output;

/// <summary>Язык C для Keil C51: <c>unsigned char code</c> в памяти программ (п. 4.4.5 ТЗ, пример В.1).</summary>
public sealed class C51CGenerator : CGeneratorBase
{
    public override OutputFormat Format => OutputFormat.CKeilC51;

    protected override bool TargetsC51 => true;

    protected override string Declare(string declarator, OutputOptions options) => "unsigned char code " + declarator;

    protected override bool NeedsStdint(OutputOptions options) => false;
}
