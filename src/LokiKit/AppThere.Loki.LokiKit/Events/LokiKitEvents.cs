// LAYER:   AppThere.Loki.LokiKit — Hourglass Waist
// KIND:    Event argument types
// PURPOSE: EventArgs subclasses for all LokiKit events.
//          Kept thin — carry only what the subscriber needs to act.
// DEPENDS: TileKey
// USED BY: ILokiDocument, ILokiView, ILokiEngine
// PHASE:   2

using AppThere.Loki.Skia.Rendering;

namespace AppThere.Loki.LokiKit.Events;

/// <summary>
/// Fired by ILokiDocument.Changed when document content or layout changes.
/// ChangeKind indicates whether the change is cosmetic (rerender only)
/// or structural (layout must be recomputed).
/// </summary>
public sealed class DocumentChangedEventArgs : EventArgs
{
    public DocumentChangeKind ChangeKind { get; }

    /// <summary>
    /// Part indices whose layout was invalidated.
    /// Empty means all parts are potentially affected.
    /// </summary>
    public IReadOnlyList<int> AffectedParts { get; }

    public DocumentChangedEventArgs(
        DocumentChangeKind changeKind,
        IReadOnlyList<int>? affectedParts = null)
    {
        ChangeKind    = changeKind;
        AffectedParts = affectedParts ?? Array.Empty<int>();
    }
}

public enum DocumentChangeKind
{
    /// <summary>Content changed; layout must be recomputed.</summary>
    ContentChanged,

    /// <summary>Only visual properties changed (e.g. colour); layout is unchanged.</summary>
    CosmeticChanged,

    /// <summary>Parts were inserted or removed.</summary>
    StructureChanged,
}

/// <summary>
/// Fired by ILokiView.TileInvalidated when one or more tiles are stale.
/// The UI should evict the listed keys from its tile cache and
/// schedule re-renders.
/// </summary>
public sealed class TileInvalidatedEventArgs : EventArgs
{
    /// <summary>
    /// The stale tile keys. Empty means all tiles for the active part
    /// are invalidated.
    /// </summary>
    public IReadOnlyList<TileKey> InvalidatedKeys { get; }

    public TileInvalidatedEventArgs(IReadOnlyList<TileKey>? keys = null)
    {
        InvalidatedKeys = keys ?? Array.Empty<TileKey>();
    }

    /// <summary>Convenience factory for a full-page invalidation.</summary>
    public static TileInvalidatedEventArgs All => new();
}

/// <summary>
/// Fired by ILokiEngine.LayoutInvalidated when PaintScenes must be rebuilt.
/// </summary>
public sealed class EngineLayoutInvalidatedEventArgs : EventArgs
{
    /// <summary>Part indices requiring a fresh GetPaintScene call.</summary>
    public IReadOnlyList<int> AffectedParts { get; }

    public EngineLayoutInvalidatedEventArgs(IReadOnlyList<int>? affectedParts = null)
    {
        AffectedParts = affectedParts ?? Array.Empty<int>();
    }
}
