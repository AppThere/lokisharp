// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for SizeF geometry value type.
// DEPENDS: SizeF, PointF
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Geometry;

public sealed class SizeFTests
{
    [Fact]
    public void Constructor_NegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SizeF(-1f, 10f);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("width");
    }

    [Fact]
    public void Constructor_NegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new SizeF(10f, -1f);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("height");
    }

    [Fact]
    public void Constructor_ZeroValues_Succeeds()
    {
        var s = new SizeF(0f, 0f);
        s.Width.Should().Be(0f);
        s.Height.Should().Be(0f);
    }

    [Fact]
    public void Zero_Always_HasZeroDimensions()
    {
        SizeF.Zero.Width.Should().Be(0f);
        SizeF.Zero.Height.Should().Be(0f);
    }

    [Fact]
    public void IsEmpty_ZeroSize_ReturnsTrue()
    {
        SizeF.Zero.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void IsEmpty_NonZeroSize_ReturnsFalse()
    {
        new SizeF(1f, 1f).IsEmpty.Should().BeFalse();
    }

    [Fact]
    public void Scale_ByUniformFactor_ScalesBothDimensions()
    {
        var s = new SizeF(4f, 6f);
        var result = s.Scale(2f);
        result.Width.Should().Be(8f);
        result.Height.Should().Be(12f);
    }

    [Fact]
    public void Scale_ByNonUniformFactors_ScalesIndependently()
    {
        var s = new SizeF(4f, 6f);
        var result = s.Scale(2f, 3f);
        result.Width.Should().Be(8f);
        result.Height.Should().Be(18f);
    }

    [Fact]
    public void Inflate_ByPositiveDelta_IncreasesDimensions()
    {
        var s = new SizeF(4f, 6f);
        var result = s.Inflate(1f, 2f);
        result.Width.Should().Be(5f);
        result.Height.Should().Be(8f);
    }

    [Fact]
    public void ToPoint_ReturnsPointWithSameValues()
    {
        var s = new SizeF(3f, 5f);
        var p = s.ToPoint();
        p.X.Should().Be(3f);
        p.Y.Should().Be(5f);
    }

    [Fact]
    public void ToString_Always_FormatsWidthAndHeight()
    {
        var s = new SizeF(100f, 200f);
        s.ToString().Should().Be("100.00 \u00d7 200.00");
    }
}
