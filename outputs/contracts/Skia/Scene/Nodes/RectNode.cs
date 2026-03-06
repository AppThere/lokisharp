// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Axis-aligned filled and/or stroked rectangle.
// DEPENDS: PaintNode, PaintStyle, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record RectNode(
    RectF      Bounds,
    PaintStyle Fill,
    PaintStyle? Stroke = null) : PaintNode(Bounds);
