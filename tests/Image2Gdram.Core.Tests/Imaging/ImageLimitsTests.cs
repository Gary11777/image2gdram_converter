using Image2Gdram.Core.Imaging;

namespace Image2Gdram.Core.Tests.Imaging;

public class ImageLimitsTests
{
    [Fact]
    public void Side_limit_is_inclusive_and_two_full_frames_fit_in_512_megabytes()
    {
        Assert.False(ImageLimits.SideExceedsLimit(8192, 8192));
        Assert.True(ImageLimits.SideExceedsLimit(8193, 1));
        Assert.True(ImageLimits.SideExceedsLimit(1, 8193));
        Assert.False(ImageLimits.MemoryExceedsLimit(8192, 8192, 2));
        Assert.True(ImageLimits.MemoryExceedsLimit(8192, 8192, 3));
    }
}
