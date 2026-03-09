// LAYER:   AppThere.Loki.LokiKit — Engine
// KIND:    Record (caret snapshot)
// PURPOSE: Snapshot of one participant's caret state.
//          Returned by ILokiEngine.GetCarets() for view-layer rendering.
// DEPENDS: SessionId, Selection, LokiColor
// USED BY: ILokiEngine, ILokiView
// PHASE:   5
// ADR:     ADR-012

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Host;

namespace AppThere.Loki.LokiKit.Engine;

/// <summary>
/// One participant's caret state. Immutable snapshot.
/// </summary>
public sealed record CaretEntry(
    SessionId  SessionId,
    Selection  Selection,
    DateTime   LastActivity,
    string?    DisplayName,    // label for remote carets
    LokiColor  Color);         // highlight colour
