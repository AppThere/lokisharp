// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Record
// PURPOSE: Minimal ILokiPart implementation used by StubEngine.
//          Holds index, size-in-points, and a display name.
//          Immutable by construction — record semantics.
//          Phase 2 only; replaced by engine-specific part types in Phase 3.
// DEPENDS: ILokiPart, SizeF
// USED BY: StubEngine
// PHASE:   2

using AppThere.Loki.Kernel.Geometry;
using AppThere.Loki.LokiKit.Document;

namespace AppThere.Loki.LokiKit.Engine;

public sealed record StubPart(
    int Index,
    SizeF SizeInPoints,
    string DisplayName) : ILokiPart;
