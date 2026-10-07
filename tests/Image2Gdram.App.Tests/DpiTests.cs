using System.Windows;
using image2gdram_converter;

namespace Image2Gdram.App.Tests;

/// <summary>Масштаб Windows 100–200 % (п. 5.1 ТЗ, решение N-57).</summary>
public class DpiTests
{
    public static TheoryData<double> Scales => new() { 1.0, 1.25, 1.5, 1.75, 2.0 };

    [Theory]
    [InlineData(2, 1.0, 2)]
    [InlineData(2, 1.25, 3)]
    [InlineData(2, 1.5, 3)]
    [InlineData(2, 1.75, 4)]
    [InlineData(2, 2.0, 4)]
    [InlineData(1, 1.25, 1)]
    [InlineData(1, 1.5, 2)]
    [InlineData(8, 1.25, 10)]
    public void Glyph_dots_are_whole_screen_pixels(int units, double scale, int expected) =>
        Assert.Equal(expected, DevicePixels.Cell(units, scale));

    [Theory]
    [MemberData(nameof(Scales))]
    public void Every_grid_cell_has_the_same_whole_number_of_screen_pixels(double scale)
    {
        const int zoom = 3;
        for (int x = 0; x < 240; x++)
        {
            double left = DevicePixels.ToDip(x * zoom, scale) * scale;
            double right = DevicePixels.ToDip((x + 1) * zoom, scale) * scale;
            Assert.Equal(zoom, right - left, 6);
            Assert.Equal(Math.Round(left), left, 6);
        }
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void Hit_test_maps_each_screen_pixel_to_its_cell(double scale)
    {
        const int zoom = 5;
        for (int pixel = 0; pixel < 200; pixel++)
        {
            double dip = (pixel + 0.5) / scale;
            Assert.Equal(pixel / zoom, DevicePixels.CellAt(dip, scale, zoom));
        }
    }

    [Theory]
    [MemberData(nameof(Scales))]
    public void Snapped_offsets_land_on_screen_pixels(double scale)
    {
        double snapped = DevicePixels.Snap(22, scale);
        Assert.Equal(Math.Round(snapped * scale), snapped * scale, 6);
        Assert.True(Math.Abs(snapped - 22) <= 0.5 / scale + 1e-9);
    }

    [Fact]
    public void Fit_to_window_counts_screen_pixels()
    {
        Assert.Equal(4, GridScale.Fit(600 * 2.0, 300 * 2.0, 240, 128));
        Assert.Equal(2, GridScale.Fit(600, 300, 240, 128));
    }

    [Fact]
    public void Window_keeps_the_minimum_of_the_specification_on_a_large_screen()
    {
        (Size minimum, Size size) = WindowFit.Fit(new Size(1200, 800), new Rect(0, 0, 1920, 1040));

        Assert.Equal(new Size(1024, 680), minimum);
        Assert.Equal(new Size(1200, 800), size);
    }

    [Theory]
    [InlineData(1366, 728, 1.0)]
    [InlineData(1366, 728, 1.25)]
    [InlineData(1920, 1040, 1.5)]
    [InlineData(1920, 1040, 2.0)]
    [InlineData(3840, 2120, 2.0)]
    public void Window_fits_the_work_area_at_any_windows_scale(double pixelsWide, double pixelsHigh, double scale)
    {
        var area = new Rect(0, 0, pixelsWide / scale, pixelsHigh / scale);

        (Size minimum, Size size) = WindowFit.Fit(new Size(1200, 800), area);

        Assert.True(size.Width <= area.Width && size.Height <= area.Height);
        Assert.True(minimum.Width <= size.Width && minimum.Height <= size.Height);
        Assert.Equal(Math.Min(1024, area.Width), minimum.Width);
        Assert.Equal(Math.Min(680, area.Height), minimum.Height);
    }
}
