using Image2Gdram.Core.Imaging;
using Image2Gdram.Core.Processing;

namespace Image2Gdram.Core.Tests.Processing;

public class CropGeometryTests
{
    [Theory]
    [InlineData(Rotation.Rotate0, 240, 128)]
    [InlineData(Rotation.Rotate180, 240, 128)]
    [InlineData(Rotation.Rotate90, 128, 240)]
    [InlineData(Rotation.Rotate270, 128, 240)]
    public void Fit_uses_the_oriented_size_and_keeps_the_center(Rotation rotation, int orientedWidth, int orientedHeight)
    {
        CropViewport fit = CropGeometry.Fit(240, 128, 128, 64, rotation);
        double expected = Math.Min(128d / orientedWidth, 64d / orientedHeight);
        Assert.Equal(CropGeometry.QuantizeScale(expected), fit.Scale);
        Assert.Equal(120, fit.CenterX);
        Assert.Equal(64, fit.CenterY);

        SourceRect rect = CropGeometry.SourceRect(fit, 128, 64, rotation);
        bool swapped = rotation is Rotation.Rotate90 or Rotation.Rotate270;
        double width = (swapped ? 64 : 128) / fit.Scale;
        double height = (swapped ? 128 : 64) / fit.Scale;
        Assert.Equal(width, rect.Width, 6);
        Assert.Equal(height, rect.Height, 6);
    }

    [Fact]
    public void Scale_range_is_one_eighth_of_fit_up_to_the_smaller_target_side()
    {
        (double min, double max) = CropGeometry.ScaleRange(240, 128, 128, 64, Rotation.Rotate0);
        double fit = Math.Min(128d / 240d, 64d / 128d);
        Assert.Equal(fit / 8, min, 12);
        Assert.Equal(Math.Max(fit, 64), max, 12);
    }

    [Fact]
    public void Clamp_keeps_the_center_inside_and_the_scale_inside_the_range()
    {
        var outside = new CropViewport(-4, 500, 10_000);
        CropViewport clamped = CropGeometry.Clamp(outside, 20, 10, 8, 6, Rotation.Rotate0);
        Assert.InRange(clamped.CenterX, 0, 20);
        Assert.InRange(clamped.CenterY, 0, 10);
        (double min, double max) = CropGeometry.ScaleRange(20, 10, 8, 6, Rotation.Rotate0);
        Assert.InRange(clamped.Scale, min, max);
        Assert.Equal(clamped, CropGeometry.Clamp(clamped, 20, 10, 8, 6, Rotation.Rotate0));
    }

    [Theory]
    [InlineData(Rotation.Rotate0, false, false)]
    [InlineData(Rotation.Rotate0, true, false)]
    [InlineData(Rotation.Rotate0, false, true)]
    [InlineData(Rotation.Rotate0, true, true)]
    [InlineData(Rotation.Rotate90, false, false)]
    [InlineData(Rotation.Rotate90, true, false)]
    [InlineData(Rotation.Rotate90, false, true)]
    [InlineData(Rotation.Rotate90, true, true)]
    [InlineData(Rotation.Rotate180, false, false)]
    [InlineData(Rotation.Rotate180, true, false)]
    [InlineData(Rotation.Rotate180, false, true)]
    [InlineData(Rotation.Rotate180, true, true)]
    [InlineData(Rotation.Rotate270, false, false)]
    [InlineData(Rotation.Rotate270, true, false)]
    [InlineData(Rotation.Rotate270, false, true)]
    [InlineData(Rotation.Rotate270, true, true)]
    public void To_oriented_matches_rotate_flip_on_a_marker_pixel(Rotation rotation, bool flipH, bool flipV)
    {
        const int width = 5;
        const int height = 3;
        var source = new RgbaImage(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                source.SetPixel(x, y, (byte)(x + 1), (byte)(y + 1), (byte)(x * 10 + y), 255);
            }
        }

        RgbaImage oriented = RotateFlipStep.Apply(source, rotation, flipH, flipV);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                (double u, double v) = CropGeometry.ToOriented(x + 0.5, y + 0.5, width, height, rotation, flipH, flipV);
                int dx = (int)Math.Floor(u);
                int dy = (int)Math.Floor(v);
                oriented.GetPixel(dx, dy, out byte r, out byte g, out byte b, out _);
                Assert.Equal(((byte)(x + 1), (byte)(y + 1), (byte)(x * 10 + y)), (r, g, b));

                (double backU, double backV) = CropGeometry.FromOriented(u, v, width, height, rotation, flipH, flipV);
                Assert.Equal(x + 0.5, backU, 9);
                Assert.Equal(y + 0.5, backV, 9);
            }
        }
    }

    [Theory]
    [InlineData(Alignment.TopLeft, 2, 2)]
    [InlineData(Alignment.TopCenter, 5, 2)]
    [InlineData(Alignment.TopRight, 8, 2)]
    [InlineData(Alignment.MiddleLeft, 2, 4)]
    [InlineData(Alignment.Center, 5, 4)]
    [InlineData(Alignment.MiddleRight, 8, 4)]
    [InlineData(Alignment.BottomLeft, 2, 6)]
    [InlineData(Alignment.BottomCenter, 5, 6)]
    [InlineData(Alignment.BottomRight, 8, 6)]
    public void From_legacy_none_places_the_window_by_alignment(Alignment alignment, double centerX, double centerY)
    {
        CropViewport crop = CropGeometry.FromLegacy(
            10, 8, 4, 4, Rotation.Rotate0, false, false, FitMode.None, alignment, 0, 0);
        Assert.Equal(1, crop.Scale);
        Assert.Equal(centerX, crop.CenterX, 3);
        Assert.Equal(centerY, crop.CenterY, 3);
    }

    [Fact]
    public void From_legacy_fit_center_without_offset_is_the_automatic_fit()
    {
        CropViewport legacy = CropGeometry.FromLegacy(
            240, 128, 128, 64, Rotation.Rotate0, false, false, FitMode.Fit, Alignment.Center, 0, 0);
        Assert.Equal(CropGeometry.Fit(240, 128, 128, 64, Rotation.Rotate0), legacy);
    }

    [Fact]
    public void From_legacy_fill_is_larger_than_fit_and_none_stays_at_one()
    {
        CropViewport fit = CropGeometry.FromLegacy(10, 8, 4, 4, Rotation.Rotate0, false, false, FitMode.Fit, Alignment.Center, 0, 0);
        CropViewport fill = CropGeometry.FromLegacy(10, 8, 4, 4, Rotation.Rotate0, false, false, FitMode.Fill, Alignment.Center, 0, 0);
        CropViewport none = CropGeometry.FromLegacy(10, 8, 4, 4, Rotation.Rotate0, false, false, FitMode.None, Alignment.Center, 0, 0);
        Assert.Equal(1, none.Scale);
        Assert.True(fill.Scale > fit.Scale);
    }

    [Fact]
    public void From_legacy_stretch_center_falls_back_to_fit_scale()
    {
        CropViewport stretch = CropGeometry.FromLegacy(
            240, 128, 100, 40, Rotation.Rotate0, false, false, FitMode.Stretch, Alignment.Center, 0, 0);
        Assert.Equal(CropGeometry.Fit(240, 128, 100, 40, Rotation.Rotate0), stretch);
    }

    [Fact]
    public void From_legacy_positive_offset_y_moves_the_window_down()
    {
        CropViewport plain = CropGeometry.FromLegacy(
            10, 8, 4, 4, Rotation.Rotate0, false, false, FitMode.None, Alignment.TopLeft, 0, 0);
        CropViewport raised = CropGeometry.FromLegacy(
            10, 8, 4, 4, Rotation.Rotate0, false, false, FitMode.None, Alignment.TopLeft, 2, 2);
        Assert.Equal(plain.CenterX - 2, raised.CenterX, 3);
        Assert.Equal(plain.CenterY + 2, raised.CenterY, 3);
    }

    [Fact]
    public void From_legacy_rotation_90_stores_the_center_in_source_coordinates()
    {
        CropViewport crop = CropGeometry.FromLegacy(
            6, 4, 4, 4, Rotation.Rotate90, false, false, FitMode.None, Alignment.Center, 0, 0);
        (double u, double v) = CropGeometry.ToOriented(crop.CenterX, crop.CenterY, 6, 4, Rotation.Rotate90, false, false);
        (int orientedWidth, int orientedHeight) = CropGeometry.OrientedSize(6, 4, Rotation.Rotate90);
        Assert.Equal(orientedWidth / 2d, u, 3);
        Assert.Equal(orientedHeight / 2d, v, 3);
    }
}
