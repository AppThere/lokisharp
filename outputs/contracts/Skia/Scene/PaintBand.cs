// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: A horizontal strip of a document Part containing a set of PaintNodes.
//          The unit of partial invalidation: only bands with a changed LayoutVersion
//          trigger tile re-renders. Bands with the same LayoutVersion are reused
//          from the previous PaintScene (structural sharing).
//          YStart and Height are in logical points. Band spans [YStart, YStart+Height).
// DEPENDS: PaintNode
// USED BY: PaintScene, TileRenderer
// PHASE:   1
// ADR:     ADR-003

using System.Collections.Immutable;

namespace AppThere.Loki.Skia.Scene;

public sealed record PaintBand(
    float                     YStart,
    float                     Height,
    ImmutableArray<PaintNode>  Nodes,
    long                       LayoutVersion);
