// LAYER:   AppThere.Loki.Tests.Unit — Tests
// KIND:    Tests
// PURPOSE: Unit tests for Matrix3x2F 2D affine transform value type.
// DEPENDS: Matrix3x2F, PointF
// USED BY: CI
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;
using FluentAssertions;

namespace AppThere.Loki.Tests.Unit.Kernel.Geometry;

public sealed class Matrix3x2FTests
{
    private const float Eps = 0.0001f;

    [Fact]
    public void Identity_Always_IsIdentityMatrix()
    {
        Matrix3x2F.Identity.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void Identity_Transform_ReturnsUnchangedPoint()
    {
        var p      = new PointF(3f, 7f);
        var result = Matrix3x2F.Identity.Transform(p);
        result.X.Should().BeApproximately(3f, Eps);
        result.Y.Should().BeApproximately(7f, Eps);
    }

    [Fact]
    public void CreateTranslation_TransformPoint_OffsetsByGivenAmount()
    {
        var m = Matrix3x2F.CreateTranslation(5f, 10f);
        var result = m.Transform(new PointF(1f, 2f));
        result.X.Should().BeApproximately(6f, Eps);
        result.Y.Should().BeApproximately(12f, Eps);
    }

    [Fact]
    public void CreateScale_TransformPoint_ScalesBothAxes()
    {
        var m = Matrix3x2F.CreateScale(2f, 3f);
        var result = m.Transform(new PointF(4f, 5f));
        result.X.Should().BeApproximately(8f, Eps);
        result.Y.Should().BeApproximately(15f, Eps);
    }

    [Fact]
    public void CreateScale_WithCenter_ScalesAroundCenter()
    {
        var center = new PointF(5f, 5f);
        var m      = Matrix3x2F.CreateScale(2f, 2f, center);
        var result = m.Transform(center);
        result.X.Should().BeApproximately(5f, Eps);
        result.Y.Should().BeApproximately(5f, Eps);
    }

    [Fact]
    public void CreateRotation_By90Degrees_RotatesPointCorrectly()
    {
        var m = Matrix3x2F.CreateRotation(MathF.PI / 2f);
        var result = m.Transform(new PointF(1f, 0f));
        result.X.Should().BeApproximately(0f, Eps);
        result.Y.Should().BeApproximately(1f, Eps);
    }

    [Fact]
    public void Multiply_IdentityWithIdentity_ReturnsIdentity()
    {
        var result = Matrix3x2F.Identity * Matrix3x2F.Identity;
        result.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void Multiply_TranslationWithScale_AppliesScaleThenTranslate()
    {
        var scale     = Matrix3x2F.CreateScale(2f, 2f);
        var translate = Matrix3x2F.CreateTranslation(10f, 0f);
        var combined  = scale * translate;
        var result    = combined.Transform(new PointF(1f, 0f));
        result.X.Should().BeApproximately(12f, Eps);
        result.Y.Should().BeApproximately(0f, Eps);
    }

    [Fact]
    public void Determinant_IdentityMatrix_ReturnsOne()
    {
        Matrix3x2F.Identity.Determinant.Should().BeApproximately(1f, Eps);
    }

    [Fact]
    public void TryInvert_IdentityMatrix_ReturnsIdentity()
    {
        var ok = Matrix3x2F.Identity.TryInvert(out var inv);
        ok.Should().BeTrue();
        inv.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void TryInvert_SingularMatrix_ReturnsFalse()
    {
        var singular = new Matrix3x2F(0f, 0f, 0f, 0f, 0f, 0f);
        var ok = singular.TryInvert(out _);
        ok.Should().BeFalse();
    }

    [Fact]
    public void TryInvert_ScaleMatrix_RoundTripsPoint()
    {
        var m  = Matrix3x2F.CreateScale(4f, 4f);
        var ok = m.TryInvert(out var inv);
        ok.Should().BeTrue();
        var p      = new PointF(8f, 12f);
        var scaled = m.Transform(p);
        var back   = inv.Transform(scaled);
        back.X.Should().BeApproximately(p.X, Eps);
        back.Y.Should().BeApproximately(p.Y, Eps);
    }
}
