// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Immutable 3×2 row-major affine transform matrix for 2D rendering.
//          Represents scale, rotation, shear, and translation in one structure.
//          Matches the layout of System.Numerics.Matrix3x2 for interoperability.
//          Does NOT handle 3D transforms — use only for 2D coordinate mapping.
// DEPENDS: PointF
// USED BY: ILokiPainter, TileRenderer, LokiPath
// PHASE:   1

namespace AppThere.Loki.Kernel.Geometry;

public readonly record struct Matrix3x2F(
    float M11, float M12,
    float M21, float M22,
    float M31, float M32)
{
    public static readonly Matrix3x2F Identity = new(1f, 0f, 0f, 1f, 0f, 0f);

    public bool IsIdentity =>
        M11 == 1f && M12 == 0f &&
        M21 == 0f && M22 == 1f &&
        M31 == 0f && M32 == 0f;

    public static Matrix3x2F CreateTranslation(float tx, float ty) =>
        new(1f, 0f, 0f, 1f, tx, ty);

    public static Matrix3x2F CreateScale(float sx, float sy) =>
        new(sx, 0f, 0f, sy, 0f, 0f);

    public static Matrix3x2F CreateScale(float sx, float sy, PointF center) =>
        new(sx, 0f, 0f, sy, center.X * (1f - sx), center.Y * (1f - sy));

    public static Matrix3x2F CreateRotation(float radians)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        return new(cos, sin, -sin, cos, 0f, 0f);
    }

    public static Matrix3x2F CreateRotation(float radians, PointF center)
    {
        var cos = MathF.Cos(radians);
        var sin = MathF.Sin(radians);
        var tx  = center.X * (1f - cos) + center.Y * sin;
        var ty  = center.Y * (1f - cos) - center.X * sin;
        return new(cos, sin, -sin, cos, tx, ty);
    }

    public static Matrix3x2F Multiply(Matrix3x2F a, Matrix3x2F b) => new(
        a.M11 * b.M11 + a.M12 * b.M21,
        a.M11 * b.M12 + a.M12 * b.M22,
        a.M21 * b.M11 + a.M22 * b.M21,
        a.M21 * b.M12 + a.M22 * b.M22,
        a.M31 * b.M11 + a.M32 * b.M21 + b.M31,
        a.M31 * b.M12 + a.M32 * b.M22 + b.M32);

    public static Matrix3x2F operator *(Matrix3x2F a, Matrix3x2F b) => Multiply(a, b);

    public PointF Transform(PointF p) =>
        new(p.X * M11 + p.Y * M21 + M31,
            p.X * M12 + p.Y * M22 + M32);

    public float Determinant => M11 * M22 - M12 * M21;

    public bool TryInvert(out Matrix3x2F result)
    {
        var det = Determinant;
        if (MathF.Abs(det) < float.Epsilon) { result = Identity; return false; }
        var inv = 1f / det;
        result = new(
             M22 * inv, -M12 * inv,
            -M21 * inv,  M11 * inv,
            (M21 * M32 - M22 * M31) * inv,
            (M12 * M31 - M11 * M32) * inv);
        return true;
    }

    public override string ToString() =>
        $"[{M11:F3},{M12:F3} | {M21:F3},{M22:F3} | {M31:F3},{M32:F3}]";
}
