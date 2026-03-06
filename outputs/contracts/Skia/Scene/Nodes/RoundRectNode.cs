// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Axis-aligned rectangle with rounded corners.
// DEPENDS: PaintNode, PaintStyle, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record RoundRectNode(
    RectF      Bounds,
    float      RadiusX,
    float      RadiusY,
    PaintStyle Fill,
    PaintStyle? Stroke = null) : PaintNode(Bounds);
