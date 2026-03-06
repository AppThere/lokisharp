// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for ImageData raster pixel container.
// DEPENDS: ImageData, PixelFormat
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Images;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Images;

public sealed class ImageDataTests
{
    [Fact]
    public void RowBytes_Rgba8888_ReturnsFourTimesWidth()
    {
        var img = new ImageData(10, 5, PixelFormat.Rgba8888Premul, new byte[200]);
        img.RowBytes.Should().Be(40);
    }

    [Fact]
    public void RowBytes_Gray8_ReturnsWidth()
    {
        var img = new ImageData(10, 5, PixelFormat.Gray8, new byte[50]);
        img.RowBytes.Should().Be(10);
    }

    [Fact]
    public void RowBytes_GrayAlpha88_ReturnsTwoTimesWidth()
    {
        var img = new ImageData(10, 5, PixelFormat.GrayAlpha88, new byte[100]);
        img.RowBytes.Should().Be(20);
    }

    [Fact]
    public void TotalBytes_Rgba8888_ReturnsWidthTimesHeightTimesFour()
    {
        var img = new ImageData(10, 5, PixelFormat.Rgba8888Premul, new byte[200]);
        img.TotalBytes.Should().Be(200);
    }

    [Fact]
    public void TotalBytes_Gray8_ReturnsWidthTimesHeight()
    {
        var img = new ImageData(10, 5, PixelFormat.Gray8, new byte[50]);
        img.TotalBytes.Should().Be(50);
    }

    [Fact]
    public void Pixels_AfterConstruction_AreAccessible()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        var img  = new ImageData(1, 1, PixelFormat.Rgba8888Premul, data);
        img.Pixels.Length.Should().Be(4);
    }

    [Fact]
    public void Properties_AfterConstruction_MatchConstructorArgs()
    {
        var img = new ImageData(640, 480, PixelFormat.Rgba8888Straight, new byte[640 * 480 * 4]);
        img.Width.Should().Be(640);
        img.Height.Should().Be(480);
        img.Format.Should().Be(PixelFormat.Rgba8888Straight);
    }
}
