// LAYER:   AppThere.Loki.Skia — Rendering Kernel
// KIND:    Record
// PURPOSE: Discriminated union of path verbs stored in LokiPath.
//          Sealed hierarchy enables exhaustive pattern matching in ToSkiaPath().
//          Coordinates are in logical points.
// DEPENDS: PointF
// USED BY: LokiPath
// PHASE:   1

using AppThere.Loki.Kernel.Geometry;

namespace AppThere.Loki.Skia.Paths;

public abstract record PathVerb;
public sealed record MoveToVerb(PointF P)                                    : PathVerb;
public sealed record LineToVerb(PointF P)                                    : PathVerb;
public sealed record CubicToVerb(PointF C1, PointF C2, PointF P)            : PathVerb;
public sealed record QuadToVerb(PointF Control, PointF P)                   : PathVerb;
public sealed record ConicToVerb(PointF Control, PointF P, float W)         : PathVerb;
public sealed record CloseVerb()                                             : PathVerb;
