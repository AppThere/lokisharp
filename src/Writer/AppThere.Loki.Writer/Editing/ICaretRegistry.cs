// LAYER:   AppThere.Loki.Writer — Editing
// KIND:    Records + interface (caret registry contract)
// PURPOSE: CaretRegistry is owned by WriterEngine and tracks one CaretEntry
//          per connected session. ICaretRegistry is the public face exposed
//          via ILokiEngine for view-layer caret rendering.
//          Phase 5: only the local session has an entry.
//          Phase 7: remote entries are added as participants connect.
// DEPENDS: SessionId, Selection, LokiColor
// USED BY: WriterEngine, ILokiEngine (GetCarets), ILokiView (rendering)
// PHASE:   5
// ADR:     ADR-012

using AppThere.Loki.Kernel.Color;
using AppThere.Loki.LokiKit.Host;
using AppThere.Loki.LokiKit.Document;
using AppThere.Loki.LokiKit.Engine;

namespace AppThere.Loki.Writer.Editing;

/// <summary>
/// Read-only view of the caret registry for the render layer.
/// Exposes only what the tile renderer needs.
/// </summary>
public interface ICaretRegistry
{
    /// <summary>Snapshot of all active caret entries.</summary>
    IReadOnlyList<CaretEntry> GetAll();

    /// <summary>Entry for the given session, or null if not registered.</summary>
    CaretEntry? Get(SessionId sessionId);

    /// <summary>Fired on the engine's thread when any caret changes.</summary>
    event EventHandler<SessionId> CaretChanged;
}
