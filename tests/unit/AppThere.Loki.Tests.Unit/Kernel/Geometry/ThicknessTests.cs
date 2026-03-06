// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for Thickness margin/padding value type.
// DEPENDS: Thickness
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Geometry;

public sealed class ThicknessTests
{
    [Fact]
    public void Constructor_UniformValue_SetsAllSides()
    {
        var t = new Thickness(5f);
        t.Left.Should().Be(5f);
        t.Top.Should().Be(5f);
        t.Right.Should().Be(5f);
        t.Bottom.Should().Be(5f);
    }

    [Fact]
    public void Constructor_HorizontalVertical_SetsPairsCorrectly()
    {
        var t = new Thickness(3f, 7f);
        t.Left.Should().Be(3f);
        t.Right.Should().Be(3f);
        t.Top.Should().Be(7f);
        t.Bottom.Should().Be(7f);
    }

    [Fact]
    public void Constructor_AllFourValues_SetsEachSide()
    {
        var t = new Thickness(1f, 2f, 3f, 4f);
        t.Left.Should().Be(1f);
        t.Top.Should().Be(2f);
        t.Right.Should().Be(3f);
        t.Bottom.Should().Be(4f);
    }

    [Fact]
    public void Zero_Always_HasAllSidesAtZero()
    {
        Thickness.Zero.IsZero.Should().BeTrue();
    }

    [Fact]
    public void Horizontal_SumsLeftAndRight()
    {
        var t = new Thickness(3f, 0f, 7f, 0f);
        t.Horizontal.Should().Be(10f);
    }

    [Fact]
    public void Vertical_SumsTopAndBottom()
    {
        var t = new Thickness(0f, 4f, 0f, 6f);
        t.Vertical.Should().Be(10f);
    }

    [Fact]
    public void IsZero_NonZeroSide_ReturnsFalse()
    {
        new Thickness(0f, 0f, 0f, 1f).IsZero.Should().BeFalse();
    }

    [Fact]
    public void ToString_Always_FormatsAllSides()
    {
        var t = new Thickness(1f, 2f, 3f, 4f);
        t.ToString().Should().Be("L=1 T=2 R=3 B=4");
    }
}
