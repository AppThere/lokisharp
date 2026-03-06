// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Abstract base for all visual primitives in a PaintScene.
//          Sealed hierarchy — exhaustive pattern matching guaranteed.
//          Every node carries a Bounds for spatial indexing and clip testing.
//          See ADR-003 for the full immutability contract and granularity rationale.
//          Concrete types live in Scene/Nodes/ — one file per type.
// DEPENDS: RectF, PaintStyle, TextPaint, LinePaint, GlyphRun, ImageRef, LokiPath
// USED BY: PaintBand, TileRenderer, LokiSkiaPainter
// PHASE:   1
// ADR:     ADR-003

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Scene;

/// <summary>Abstract base. Never instantiate directly — use concrete subtypes.</summary>
public abstract record PaintNode(RectF Bounds);
