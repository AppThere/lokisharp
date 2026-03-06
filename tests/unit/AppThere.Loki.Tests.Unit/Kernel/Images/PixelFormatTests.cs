// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for PixelFormat enum and its BytesPerPixel extension.
// DEPENDS: PixelFormat, PixelFormatExtensions
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Images;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Images;

public sealed class PixelFormatTests
{
    [Fact]
    public void BytesPerPixel_Rgba8888Premul_ReturnsFour()
    {
        PixelFormat.Rgba8888Premul.BytesPerPixel().Should().Be(4);
    }

    [Fact]
    public void BytesPerPixel_Rgba8888Straight_ReturnsFour()
    {
        PixelFormat.Rgba8888Straight.BytesPerPixel().Should().Be(4);
    }

    [Fact]
    public void BytesPerPixel_Gray8_ReturnsOne()
    {
        PixelFormat.Gray8.BytesPerPixel().Should().Be(1);
    }

    [Fact]
    public void BytesPerPixel_GrayAlpha88_ReturnsTwo()
    {
        PixelFormat.GrayAlpha88.BytesPerPixel().Should().Be(2);
    }

    [Fact]
    public void BytesPerPixel_AllDefinedFormats_DoNotThrow()
    {
        foreach (var fmt in Enum.GetValues<PixelFormat>())
        {
            var act = () => fmt.BytesPerPixel();
            act.Should().NotThrow();
        }
    }
}
