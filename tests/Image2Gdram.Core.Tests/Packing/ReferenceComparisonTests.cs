using Image2Gdram.Core.Packing;
using Image2Gdram.Reference;

namespace Image2Gdram.Core.Tests.Packing;

/// <summary>Побайтное сравнение ядра с независимой эталонной реализацией по всем 16 комбинациям.</summary>
public class ReferenceComparisonTests
{
    private readonly Mono1bppPacker _packer = new();

    [Theory]
    [MemberData(nameof(TestBitmaps.SizeAndCombination), MemberType = typeof(TestBitmaps))]
    public void Core_matches_reference_on_random_bitmaps(int width, int height, int combination)
    {
        RefOptions reference = RefOptions.AllCombinations[combination];
        MonoBitmap bitmap = TestBitmaps.Random(width, height, TestBitmaps.SeedFor(width, height));

        byte[] expected = ReferencePacker.Pack(TestBitmaps.ToArray(bitmap), reference);
        byte[] actual = _packer.Pack(bitmap, TestBitmaps.ToCore(reference));

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Core_matches_reference_on_1024x1024_for_all_combinations()
    {
        MonoBitmap bitmap = TestBitmaps.Random(1024, 1024, TestBitmaps.SeedFor(1024, 1024));
        bool[,] active = TestBitmaps.ToArray(bitmap);

        foreach (RefOptions reference in RefOptions.AllCombinations)
        {
            byte[] expected = ReferencePacker.Pack(active, reference);
            byte[] actual = _packer.Pack(bitmap, TestBitmaps.ToCore(reference));

            Assert.True(expected.AsSpan().SequenceEqual(actual), $"Mismatch for {reference.Name}");
        }
    }

    [Fact]
    public void Combination_list_has_16_distinct_entries()
    {
        IReadOnlyList<RefOptions> all = RefOptions.AllCombinations;

        Assert.Equal(16, all.Count);
        Assert.Equal(16, all.Select(o => o.Name).Distinct().Count());
        Assert.Equal(8, all.Count(o => o.Direction == RefDirection.Horizontal));
        Assert.Equal(16, all.Select(TestBitmaps.ToCore).Distinct().Count());
    }
}
