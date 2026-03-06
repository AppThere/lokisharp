// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for DpiScale display scaling value type.
// DEPENDS: DpiScale
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Color;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Color;

public sealed class DpiScaleTests
{
    [Fact]
    public void Identity_Always_HasBothScalesAtOne()
    {
        DpiScale.Identity.ScaleX.Should().Be(1f);
        DpiScale.Identity.ScaleY.Should().Be(1f);
    }

    [Fact]
    public void Identity_IsIdentity_ReturnsTrue()
    {
        DpiScale.Identity.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void Retina2x_Always_HasBothScalesAtTwo()
    {
        DpiScale.Retina2x.ScaleX.Should().Be(2f);
        DpiScale.Retina2x.ScaleY.Should().Be(2f);
    }

    [Fact]
    public void Constructor_UniformValue_SetsBothAxes()
    {
        var d = new DpiScale(1.5f);
        d.ScaleX.Should().Be(1.5f);
        d.ScaleY.Should().Be(1.5f);
    }

    [Fact]
    public void IsIdentity_NonOneScale_ReturnsFalse()
    {
        new DpiScale(2f).IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void ToPhysicalX_ScalesLogicalByScaleX()
    {
        var d = new DpiScale(2f, 3f);
        d.ToPhysicalX(10f).Should().Be(20f);
    }

    [Fact]
    public void ToPhysicalY_ScalesLogicalByScaleY()
    {
        var d = new DpiScale(2f, 3f);
        d.ToPhysicalY(10f).Should().Be(30f);
    }

    [Fact]
    public void ToLogicalX_DividesPhysicalByScaleX()
    {
        var d = new DpiScale(2f, 3f);
        d.ToLogicalX(20f).Should().Be(10f);
    }

    [Fact]
    public void ToLogicalY_DividesPhysicalByScaleY()
    {
        var d = new DpiScale(2f, 3f);
        d.ToLogicalY(30f).Should().Be(10f);
    }

    [Fact]
    public void ToString_UniformScale_FormatsWithSingleMultiplier()
    {
        new DpiScale(2f).ToString().Should().Be("2\u00d7");
    }

    [Fact]
    public void ToString_NonUniformScale_FormatsBothAxes()
    {
        new DpiScale(2f, 3f).ToString().Should().Be("2\u00d73");
    }
}
