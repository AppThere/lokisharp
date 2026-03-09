// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: One or more shaped text runs sharing a single baseline origin.
//          GlyphRun granularity: one node per styled text run (same font/size/colour).
//          This matches the granularity of DirectWrite, Core Text, and HarfBuzz.
// DEPENDS: PaintNode, GlyphRun, TextPaint, PointF, RectF
// USED BY: PaintBand, TileRenderer
// PHASE:   1
// ADR:     ADR-003

using System.Collections.Immutable;
using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene.Nodes;

public sealed record GlyphRunNode(
    RectF                    Bounds,
    PointF                   Origin,   // baseline origin in logical points
    ImmutableArray<GlyphRun> Runs,
    TextPaint                Paint,
    int                      ParagraphIndex = 0,
    int                      RunIndex       = 0,
    int                      RunOffset      = 0,
    string                   Text           = "") : PaintNode(Bounds);
