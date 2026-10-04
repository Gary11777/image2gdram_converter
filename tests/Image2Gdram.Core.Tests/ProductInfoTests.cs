using System.Reflection;
using Image2Gdram.Core.Text;

namespace Image2Gdram.Core.Tests;

public class ProductInfoTests
{
    [Fact]
    public void Name_and_version_match_specification_1_1()
    {
        Assert.Equal("Image2GDRAM Converter 1.0", ProductInfo.NameWithVersion);
    }

    [Fact]
    public void Assembly_attributes_match_product_info()
    {
        Assembly core = typeof(ProductInfo).Assembly;

        Assert.Equal(ProductInfo.Name, core.GetCustomAttribute<AssemblyProductAttribute>()?.Product);
        Assert.StartsWith("1.0.0", core.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion);
        Assert.Equal(new Version(1, 0, 0, 0), core.GetName().Version);
    }
}
