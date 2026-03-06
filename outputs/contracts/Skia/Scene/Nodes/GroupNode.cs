// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Transparent group with optional transform, clip, and opacity.
//          Children are rendered in order (painter's algorithm).
//          Null transform = identity. Null clip = no clip.
// DEPENDS: PaintNode, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1
// ADR:     ADR-003

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record GroupNode(
    RectF                      Bounds,
    ImmutableArray<PaintNode>  Children,
    float                      Opacity    = 1f,
    RectF?                     Clip       = null,
    // Phase 1: only uniform scale + translate supported (no rotation/shear)
    float                      ScaleX     = 1f,
    float                      ScaleY     = 1f,
    float                      TranslateX = 0f,
    float                      TranslateY = 0f) : PaintNode(Bounds);
