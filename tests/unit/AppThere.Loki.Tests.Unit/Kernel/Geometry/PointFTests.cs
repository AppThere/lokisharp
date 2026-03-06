// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for PointF geometry value type.
// DEPENDS: PointF
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Geometry;

public sealed class PointFTests
{
    [Fact]
    public void Zero_Always_HasXAndYAtOrigin()
    {
        PointF.Zero.X.Should().Be(0f);
        PointF.Zero.Y.Should().Be(0f);
    }

    [Fact]
    public void Offset_WithDxDy_ShiftsCoordinates()
    {
        var p = new PointF(1f, 2f);
        var result = p.Offset(3f, 4f);
        result.X.Should().Be(4f);
        result.Y.Should().Be(6f);
    }

    [Fact]
    public void Offset_WithPointFDelta_ShiftsCoordinates()
    {
        var p     = new PointF(1f, 2f);
        var delta = new PointF(3f, 4f);
        var result = p.Offset(delta);
        result.X.Should().Be(4f);
        result.Y.Should().Be(6f);
    }

    [Fact]
    public void Scale_ByFactor_MultipliesBothComponents()
    {
        var p = new PointF(3f, 4f);
        var result = p.Scale(2f);
        result.X.Should().Be(6f);
        result.Y.Should().Be(8f);
    }

    [Fact]
    public void DistanceTo_SamePoint_ReturnsZero()
    {
        var p = new PointF(1f, 1f);
        p.DistanceTo(p).Should().Be(0f);
    }

    [Fact]
    public void DistanceTo_KnownTriangle_ReturnsCorrectHypotenuse()
    {
        var a = new PointF(0f, 0f);
        var b = new PointF(3f, 4f);
        a.DistanceTo(b).Should().BeApproximately(5f, 0.0001f);
    }

    [Fact]
    public void ToString_Always_FormatsXAndY()
    {
        var p = new PointF(1.5f, 2.75f);
        p.ToString().Should().Be("(1.50, 2.75)");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new PointF(1f, 2f);
        var b = new PointF(1f, 2f);
        a.Should().Be(b);
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new PointF(1f, 2f);
        var b = new PointF(1f, 3f);
        a.Should().NotBe(b);
    }
}
