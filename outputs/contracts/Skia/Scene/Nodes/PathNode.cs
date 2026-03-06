// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Arbitrary vector path (bezier curves, lines, arcs).
//          Path geometry is in LokiPath; style is in Fill/Stroke.
// DEPENDS: PaintNode, LokiPath, PaintStyle, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.Skia.Paths;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record PathNode(
    RectF      Bounds,
    LokiPath   Path,
    PaintStyle Fill,
    PaintStyle? Stroke = null) : PaintNode(Bounds);
