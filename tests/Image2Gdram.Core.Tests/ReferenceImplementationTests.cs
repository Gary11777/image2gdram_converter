using System.Reflection;
using Image2Gdram.Core.Text;
using Image2Gdram.Reference;

namespace Image2Gdram.Core.Tests;

/// <summary>
/// Самопроверка эталонной реализации без участия ядра: контрольные примеры F-05 и пример п. 4.3.4,
/// а также отсутствие зависимости от ядра (решение N-28).
/// </summary>
public class ReferenceImplementationTests
{
    [Theory]
    [InlineData(RefDirection.Vertical, RefBitOrder.LsbFirst, 8, false, 0x01)]
    [InlineData(RefDirection.Vertical, RefBitOrder.LsbFirst, 8, true, 0xFE)]
    [InlineData(RefDirection.Vertical, RefBitOrder.MsbFirst, 8, false, 0x80)]
    [InlineData(RefDirection.Vertical, RefBitOrder.MsbFirst, 8, true, 0x7F)]
    [InlineData(RefDirection.Horizontal, RefBitOrder.MsbFirst, 8, false, 0x80)]
    [InlineData(RefDirection.Horizontal, RefBitOrder.MsbFirst, 8, true, 0x7F)]
    [InlineData(RefDirection.Horizontal, RefBitOrder.MsbFirst, 6, false, 0x20)]
    [InlineData(RefDirection.Horizontal, RefBitOrder.MsbFirst, 6, true, 0xDF)]
    public void Reference_gives_control_bytes(RefDirection direction, RefBitOrder order, int bits, bool invert, int expected)
    {
        var active = new bool[16, 16];
        active[0, 0] = true;

        byte[] bytes = ReferencePacker.Pack(active, new RefOptions(direction, order, bits, RefTraversal.ByPages, invert));

        Assert.Equal(expected, bytes[0]);
        Assert.All(bytes.Skip(1), b => Assert.Equal(invert ? 0xFF : 0x00, b));
    }

    [Fact]
    public void Reference_follows_traversal_example_for_12x16()
    {
        var active = new bool[12, 16];
        active[1, 9] = true;

        byte[] byPages = ReferencePacker.Pack(active, new RefOptions(RefDirection.Vertical, RefBitOrder.LsbFirst, 8, RefTraversal.ByPages, false));
        byte[] byColumns = ReferencePacker.Pack(active, new RefOptions(RefDirection.Vertical, RefBitOrder.LsbFirst, 8, RefTraversal.ByColumns, false));

        Assert.Equal(24, byPages.Length);
        Assert.Equal(0x02, byPages[12 + 1]);
        Assert.Single(byPages, b => b != 0);
        Assert.Equal(0x02, byColumns[2 * 1 + 1]);
        Assert.Single(byColumns, b => b != 0);
    }

    [Fact]
    public void Reference_assembly_does_not_reference_core()
    {
        AssemblyName[] references = typeof(ReferencePacker).Assembly.GetReferencedAssemblies();
        string coreName = typeof(ProductInfo).Assembly.GetName().Name!;

        Assert.DoesNotContain(references, r => r.Name == coreName);
    }
}
