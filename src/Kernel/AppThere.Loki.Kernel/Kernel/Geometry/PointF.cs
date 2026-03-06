// LAYER:   AppThere.Loki.Kernel — Kernel
// KIND:    Record
// PURPOSE: Immutable 2D point in logical units (points at 96 dpi).
//          Used throughout the rendering pipeline for positions and offsets.
//          All coordinate systems use (0,0) at top-left, Y increasing downward.
// DEPENDS: (none)
// USED BY: PaintNode, ILokiPainter, TileRequest, LokiPath
// PHASE:   1

namespace AppThere.Loki.Kernel.Geometry;

public readonly record struct PointF(float X, float Y)
{
    public static readonly PointF Zero = new(0f, 0f);

    public PointF Offset(float dx, float dy) => new(X + dx, Y + dy);
    public PointF Offset(PointF delta)        => new(X + delta.X, Y + delta.Y);
    public PointF Scale(float factor)         => new(X * factor, Y * factor);

    public float DistanceTo(PointF other)
    {
        var dx = X - other.X;
        var dy = Y - other.Y;
        return MathF.Sqrt(dx * dx + dy * dy);
    }

    public override string ToString() => $"({X:F2}, {Y:F2})";
}
