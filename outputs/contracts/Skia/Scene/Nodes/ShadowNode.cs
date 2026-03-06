// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Drop shadow behind a child PaintNode.
//          The shadow is rendered before the Content node.
// DEPENDS: PaintNode, LokiColor, PointF, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record ShadowNode(
    RectF     Bounds,
    PaintNode Content,
    PointF    Offset,
    float     BlurRadius,
    LokiColor ShadowColor) : PaintNode(Bounds);
