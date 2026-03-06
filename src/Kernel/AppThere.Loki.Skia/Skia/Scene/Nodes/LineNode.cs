// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: A straight line segment between two points.
//          Bounds is computed from A and B plus StrokeWidth / 2.
// DEPENDS: PaintNode, LinePaint, PointF, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record LineNode(
    PointF    A,
    PointF    B,
    LinePaint Paint)
    : PaintNode(RectF.FromLTRB(
        MathF.Min(A.X, B.X) - Paint.Width / 2f,
        MathF.Min(A.Y, B.Y) - Paint.Width / 2f,
        MathF.Max(A.X, B.X) + Paint.Width / 2f,
        MathF.Max(A.Y, B.Y) + Paint.Width / 2f));
