// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for RectF axis-aligned rectangle value type.
// DEPENDS: RectF, PointF, SizeF
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Geometry;

public sealed class RectFTests
{
    [Fact]
    public void Constructor_NegativeWidth_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new RectF(0f, 0f, -1f, 10f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_NegativeHeight_ThrowsArgumentOutOfRangeException()
    {
        var act = () => new RectF(0f, 0f, 10f, -1f);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void FromLTRB_ValidBounds_ComputesDimensions()
    {
        var r = RectF.FromLTRB(1f, 2f, 5f, 6f);
        r.X.Should().Be(1f);
        r.Y.Should().Be(2f);
        r.Width.Should().Be(4f);
        r.Height.Should().Be(4f);
    }

    [Fact]
    public void FromCenterSize_ValidInput_CentersRect()
    {
        var center = new PointF(10f, 10f);
        var size   = new SizeF(4f, 4f);
        var r = RectF.FromCenterSize(center, size);
        r.Center.X.Should().BeApproximately(10f, 0.0001f);
        r.Center.Y.Should().BeApproximately(10f, 0.0001f);
        r.Width.Should().Be(4f);
        r.Height.Should().Be(4f);
    }

    [Fact]
    public void Empty_Always_HasZeroDimensions()
    {
        RectF.Empty.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Edges_ComputedCorrectly()
    {
        var r = new RectF(2f, 3f, 10f, 5f);
        r.Left.Should().Be(2f);
        r.Top.Should().Be(3f);
        r.Right.Should().Be(12f);
        r.Bottom.Should().Be(8f);
    }

    [Fact]
    public void Corners_ComputedCorrectly()
    {
        var r = new RectF(1f, 2f, 4f, 6f);
        r.TopLeft.Should().Be(new PointF(1f, 2f));
        r.TopRight.Should().Be(new PointF(5f, 2f));
        r.BottomLeft.Should().Be(new PointF(1f, 8f));
        r.BottomRight.Should().Be(new PointF(5f, 8f));
    }

    [Fact]
    public void Contains_PointInsideRect_ReturnsTrue()
    {
        var r = new RectF(0f, 0f, 10f, 10f);
        r.Contains(new PointF(5f, 5f)).Should().BeTrue();
    }

    [Fact]
    public void Contains_PointOnRightEdge_ReturnsFalse()
    {
        var r = new RectF(0f, 0f, 10f, 10f);
        r.Contains(new PointF(10f, 5f)).Should().BeFalse();
    }

    [Fact]
    public void Contains_PointOutsideRect_ReturnsFalse()
    {
        var r = new RectF(0f, 0f, 10f, 10f);
        r.Contains(new PointF(15f, 5f)).Should().BeFalse();
    }

    [Fact]
    public void IntersectsWith_OverlappingRects_ReturnsTrue()
    {
        var a = new RectF(0f, 0f, 10f, 10f);
        var b = new RectF(5f, 5f, 10f, 10f);
        a.IntersectsWith(b).Should().BeTrue();
    }

    [Fact]
    public void IntersectsWith_NonOverlappingRects_ReturnsFalse()
    {
        var a = new RectF(0f, 0f, 5f, 5f);
        var b = new RectF(10f, 10f, 5f, 5f);
        a.IntersectsWith(b).Should().BeFalse();
    }

    [Fact]
    public void Intersect_OverlappingRects_ReturnsOverlapArea()
    {
        var a = new RectF(0f, 0f, 10f, 10f);
        var b = new RectF(5f, 5f, 10f, 10f);
        var result = a.Intersect(b);
        result.Should().Be(new RectF(5f, 5f, 5f, 5f));
    }

    [Fact]
    public void Intersect_NonOverlappingRects_ReturnsEmpty()
    {
        var a = new RectF(0f, 0f, 5f, 5f);
        var b = new RectF(10f, 10f, 5f, 5f);
        a.Intersect(b).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void Union_TwoRects_ReturnsBoundingBox()
    {
        var a = new RectF(0f, 0f, 5f, 5f);
        var b = new RectF(3f, 3f, 5f, 5f);
        var result = a.Union(b);
        result.Should().Be(RectF.FromLTRB(0f, 0f, 8f, 8f));
    }

    [Fact]
    public void Inflate_ByPositiveDelta_ExpandsRect()
    {
        var r = new RectF(5f, 5f, 10f, 10f);
        var result = r.Inflate(2f, 2f);
        result.Should().Be(new RectF(3f, 3f, 14f, 14f));
    }

    [Fact]
    public void Offset_ByDxDy_TranslatesOrigin()
    {
        var r = new RectF(1f, 2f, 5f, 5f);
        var result = r.Offset(3f, 4f);
        result.X.Should().Be(4f);
        result.Y.Should().Be(6f);
        result.Width.Should().Be(5f);
        result.Height.Should().Be(5f);
    }
}
